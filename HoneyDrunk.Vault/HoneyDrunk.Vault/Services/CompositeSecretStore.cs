using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Resilience;
using HoneyDrunk.Vault.Telemetry;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Composite secret store that orchestrates multiple providers with priority-based fallback.
/// </summary>
public sealed class CompositeSecretStore : ISecretStore
{
    private static readonly ActivitySource CompositeActivitySource = new("HoneyDrunk.Vault.Composite");

    private readonly ResiliencePipelineFactory _resilienceFactory;
    private readonly VaultTelemetry? _telemetry;
    private readonly ILogger<CompositeSecretStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeSecretStore"/> class.
    /// </summary>
    /// <param name="providers">The registered secret providers.</param>
    /// <param name="resilienceFactory">The resilience pipeline factory.</param>
    /// <param name="telemetry">The optional telemetry service.</param>
    /// <param name="logger">The logger.</param>
    public CompositeSecretStore(
        IEnumerable<RegisteredSecretProvider> providers,
        ResiliencePipelineFactory resilienceFactory,
        VaultTelemetry? telemetry,
        ILogger<CompositeSecretStore> logger)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(resilienceFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _resilienceFactory = resilienceFactory;
        _telemetry = telemetry;
        _logger = logger;

        // Order providers by priority (ascending - lower number = higher priority)
        Providers = [.. providers
            .Where(p => p.Registration.IsEnabled && p.Provider.IsAvailable)
            .OrderBy(p => p.Registration.Priority)];

        if (Providers.Count == 0)
        {
            _logger.LogWarning("No enabled and available secret providers registered");
        }
        else
        {
            _logger.LogInformation(
                "Composite secret store initialized with {Count} providers: {Providers}",
                Providers.Count,
                string.Join(", ", Providers.Select(p => $"{p.Provider.ProviderName} (priority: {p.Registration.Priority})")));
        }
    }

    /// <summary>
    /// Gets the registered providers in priority order.
    /// </summary>
    public IReadOnlyList<RegisteredSecretProvider> Providers { get; }

    /// <inheritdoc/>
    public async Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var result = await TryGetSecretAsync(identifier, cancellationToken).ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return result.Value!;
        }

        // Distinguish between "not found" and provider failures
        // Error messages for not-found start with "Secret '" or "No providers"
        var isNotFound = result.ErrorMessage?.StartsWith("Secret '", StringComparison.Ordinal) == true
            || result.ErrorMessage?.StartsWith("No providers", StringComparison.Ordinal) == true;

        if (isNotFound)
        {
            throw new SecretNotFoundException(identifier.Name);
        }

        // Provider failure - throw VaultOperationException with the error details
        throw new VaultOperationException(
            result.ErrorMessage ?? $"Failed to retrieve secret '{identifier.Name}'");
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        using var activity = CompositeActivitySource.StartActivity("composite.get_secret", ActivityKind.Internal);
        activity?.SetTag("vault.key", identifier.Name);
        activity?.SetTag("vault.provider_count", Providers.Count);

        if (Providers.Count == 0)
        {
            _logger.LogWarning("No providers available to retrieve secret '{SecretName}'", identifier.Name);
            activity?.SetTag("vault.result", "no_providers");
            return VaultResult.Failure<SecretValue>("No providers available");
        }

        Exception? lastException = null;
        var attemptedProviders = new List<string>();
        var fallbackCount = 0;

        foreach (var registered in Providers)
        {
            var providerName = registered.Provider.ProviderName;
            attemptedProviders.Add(providerName);

            try
            {
                var pipeline = _resilienceFactory.GetPipeline(providerName);

                var secretValue = await pipeline.ExecuteAsync(
                    async ct =>
                    {
                        _logger.LogDebug(
                            "Attempting to fetch secret '{SecretName}' from provider '{ProviderName}'",
                            identifier.Name,
                            providerName);

                        return await registered.Provider.FetchSecretAsync(
                            identifier.Name,
                            identifier.Version,
                            ct)
                            .ConfigureAwait(false);
                    },
                    cancellationToken).ConfigureAwait(false);

                _logger.LogDebug(
                    "Successfully retrieved secret '{SecretName}' from provider '{ProviderName}'",
                    identifier.Name,
                    providerName);

                // Record telemetry for successful retrieval
                activity?.SetTag("vault.result", "success");
                activity?.SetTag("vault.serving_provider", providerName);
                activity?.SetTag("vault.fallback_count", fallbackCount);
                _telemetry?.RecordCacheHit(identifier.Name); // Indicates provider served the request

                return VaultResult.Success(secretValue);
            }
            catch (Exception ex)
            {
                lastException = ex;
                var classification = FailureClassifier.Classify(ex);
                fallbackCount++;

                _logger.LogDebug(
                    ex,
                    "Provider '{ProviderName}' failed for secret '{SecretName}' with classification '{Classification}'",
                    providerName,
                    identifier.Name,
                    classification);

                switch (classification)
                {
                    case FailureClassification.NotFound:
                        // Continue to next provider
                        _logger.LogDebug(
                            "Secret '{SecretName}' not found in provider '{ProviderName}', trying next provider",
                            identifier.Name,
                            providerName);
                        continue;

                    case FailureClassification.FatalConfiguration:
                        // Fatal error - fail fast
                        _logger.LogError(
                            ex,
                            "Fatal configuration error from provider '{ProviderName}' for secret '{SecretName}'",
                            providerName,
                            identifier.Name);
                        activity?.SetTag("vault.result", "fatal_error");
                        activity?.SetTag("vault.failed_provider", providerName);
                        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                        return VaultResult.Failure<SecretValue>($"Fatal configuration error: {ex.Message}");

                    case FailureClassification.Transient:
                        if (registered.Registration.IsRequired)
                        {
                            // Required provider failed - fail fast
                            _logger.LogError(
                                ex,
                                "Required provider '{ProviderName}' failed transiently for secret '{SecretName}'",
                                providerName,
                                identifier.Name);
                            activity?.SetTag("vault.result", "required_provider_failed");
                            activity?.SetTag("vault.failed_provider", providerName);
                            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                            return VaultResult.Failure<SecretValue>($"Required provider '{providerName}' failed: {ex.Message}");
                        }

                        // Optional provider - continue to next
                        _logger.LogWarning(
                            ex,
                            "Optional provider '{ProviderName}' failed transiently for secret '{SecretName}', trying next provider",
                            providerName,
                            identifier.Name);
                        continue;
                }
            }
        }

        // All providers exhausted
        _logger.LogWarning(
            "Secret '{SecretName}' not found in any provider. Attempted: {Providers}",
            identifier.Name,
            string.Join(", ", attemptedProviders));

        activity?.SetTag("vault.result", "not_found");
        activity?.SetTag("vault.attempted_providers", string.Join(",", attemptedProviders));
        activity?.SetTag("vault.fallback_count", fallbackCount);

        var errorMessage = lastException is SecretNotFoundException or null
            ? $"Secret '{identifier.Name}' not found in any provider"
            : $"Failed to retrieve secret '{identifier.Name}': {lastException.Message}";

        return VaultResult.Failure<SecretValue>(errorMessage);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        if (Providers.Count == 0)
        {
            _logger.LogWarning("No providers available to list versions for secret '{SecretName}'", secretName);
            throw new VaultOperationException($"No providers available to list versions for secret '{secretName}'");
        }

        Exception? lastException = null;

        foreach (var registered in Providers)
        {
            var providerName = registered.Provider.ProviderName;

            try
            {
                var pipeline = _resilienceFactory.GetPipeline(providerName);

                var versions = await pipeline.ExecuteAsync(
                    async ct =>
                    {
                        return await registered.Provider.ListVersionsAsync(secretName, ct).ConfigureAwait(false);
                    },
                    cancellationToken).ConfigureAwait(false);

                _logger.LogDebug(
                    "Listed {Count} versions for secret '{SecretName}' from provider '{ProviderName}'",
                    versions.Count,
                    secretName,
                    providerName);

                return versions;
            }
            catch (Exception ex)
            {
                lastException = ex;
                var classification = FailureClassifier.Classify(ex);

                if (classification == FailureClassification.NotFound)
                {
                    continue;
                }

                if (classification == FailureClassification.FatalConfiguration)
                {
                    throw new VaultOperationException($"Fatal configuration error: {ex.Message}", ex);
                }

                if (registered.Registration.IsRequired)
                {
                    throw new VaultOperationException($"Required provider '{providerName}' failed: {ex.Message}", ex);
                }

                _logger.LogWarning(
                    ex,
                    "Provider '{ProviderName}' failed to list versions for secret '{SecretName}', trying next provider",
                    providerName,
                    secretName);
            }
        }

        throw new SecretNotFoundException(secretName);
    }
}
