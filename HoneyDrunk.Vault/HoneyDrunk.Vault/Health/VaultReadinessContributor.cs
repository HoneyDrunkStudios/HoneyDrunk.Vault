using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Health;

/// <summary>
/// Readiness contributor for the vault system.
/// Vault is ready only if all required providers are reachable.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VaultReadinessContributor"/> class.
/// </remarks>
/// <param name="secretProviders">The registered secret providers.</param>
/// <param name="configProviders">The registered configuration providers.</param>
/// <param name="logger">The logger.</param>
public sealed class VaultReadinessContributor(
    IEnumerable<RegisteredSecretProvider> secretProviders,
    IEnumerable<RegisteredConfigSourceProvider> configProviders,
    ILogger<VaultReadinessContributor> logger) : IReadinessContributor
{
    private readonly IEnumerable<RegisteredSecretProvider> _secretProviders = secretProviders ?? throw new ArgumentNullException(nameof(secretProviders));
    private readonly IEnumerable<RegisteredConfigSourceProvider> _configProviders = configProviders ?? throw new ArgumentNullException(nameof(configProviders));
    private readonly ILogger<VaultReadinessContributor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public string Name => "HoneyDrunk.Vault";

    /// <inheritdoc/>
    public int Priority => 100;

    /// <inheritdoc/>
    public bool IsRequired => true;

    /// <inheritdoc/>
    public async Task<(bool, string?)> CheckReadinessAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Performing vault readiness check");

        var readyProviders = new List<string>();
        var notReadyProviders = new List<string>();
        var requiredNotReadyProviders = new List<string>();

        // Check secret providers
        foreach (var registered in _secretProviders.Where(p => p.Registration.IsEnabled))
        {
            var providerName = registered.Provider.ProviderName;
            var isRequired = registered.Registration.IsRequired;

            try
            {
                var isHealthy = await registered.Provider.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

                if (isHealthy)
                {
                    readyProviders.Add(providerName);
                    _logger.LogDebug("Secret provider '{ProviderName}' is ready", providerName);
                }
                else
                {
                    notReadyProviders.Add(providerName);
                    if (isRequired)
                    {
                        requiredNotReadyProviders.Add(providerName);
                    }

                    _logger.LogWarning(
                        "Secret provider '{ProviderName}' is not ready (required: {IsRequired})",
                        providerName,
                        isRequired);
                }
            }
            catch (Exception ex)
            {
                notReadyProviders.Add(providerName);
                if (isRequired)
                {
                    requiredNotReadyProviders.Add(providerName);
                }

                _logger.LogWarning(
                    ex,
                    "Secret provider '{ProviderName}' readiness check failed (required: {IsRequired})",
                    providerName,
                    isRequired);
            }
        }

        // Check config providers
        foreach (var registered in _configProviders.Where(p => p.Registration.IsEnabled))
        {
            var providerName = registered.Provider.ProviderName;
            var isRequired = registered.Registration.IsRequired;

            try
            {
                var isHealthy = await registered.Provider.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

                if (isHealthy)
                {
                    if (!readyProviders.Contains(providerName))
                    {
                        readyProviders.Add(providerName);
                    }

                    _logger.LogDebug("Config provider '{ProviderName}' is ready", providerName);
                }
                else
                {
                    if (!notReadyProviders.Contains(providerName))
                    {
                        notReadyProviders.Add(providerName);
                    }

                    if (isRequired && !requiredNotReadyProviders.Contains(providerName))
                    {
                        requiredNotReadyProviders.Add(providerName);
                    }

                    _logger.LogWarning(
                        "Config provider '{ProviderName}' is not ready (required: {IsRequired})",
                        providerName,
                        isRequired);
                }
            }
            catch (Exception ex)
            {
                if (!notReadyProviders.Contains(providerName))
                {
                    notReadyProviders.Add(providerName);
                }

                if (isRequired && !requiredNotReadyProviders.Contains(providerName))
                {
                    requiredNotReadyProviders.Add(providerName);
                }

                _logger.LogWarning(
                    ex,
                    "Config provider '{ProviderName}' readiness check failed (required: {IsRequired})",
                    providerName,
                    isRequired);
            }
        }

        // Determine readiness
        // Not ready if any required provider is not reachable
        if (requiredNotReadyProviders.Count > 0)
        {
            _logger.LogError(
                "Vault not ready: required providers unavailable: {Providers}",
                string.Join(", ", requiredNotReadyProviders));
            return (false, $"Required providers not ready: {string.Join(", ", requiredNotReadyProviders)}");
        }

        // Ready if no providers are configured (degenerate case)
        if (readyProviders.Count == 0 && notReadyProviders.Count == 0)
        {
            _logger.LogWarning("Vault ready but no providers configured");
            return (true, "Vault ready (no providers configured)");
        }

        // Ready if at least one provider is available (and no required providers are down)
        if (readyProviders.Count > 0)
        {
            var message = notReadyProviders.Count > 0
                ? $"Ready: {string.Join(", ", readyProviders)}; Unavailable: {string.Join(", ", notReadyProviders)}"
                : $"Ready: {string.Join(", ", readyProviders)}";

            _logger.LogDebug("Vault readiness check passed: {Message}", message);
            return (true, message);
        }

        // No providers ready
        _logger.LogError("Vault not ready: no providers available");
        return (false, "No providers available");
    }
}
