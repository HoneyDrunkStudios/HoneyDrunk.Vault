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

        var buckets = new ReadinessBuckets(
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal));

        await ProviderProbe.ProbeAllAsync(_secretProviders, _configProviders, buckets, Classify, cancellationToken).ConfigureAwait(false);

        return Summarize(buckets.Ready, buckets.NotReady, buckets.RequiredNotReady);
    }

    private void Classify(string providerKind, string providerName, bool isRequired, ProbeOutcome outcome, ReadinessBuckets buckets)
    {
        if (outcome.IsHealthy)
        {
            buckets.Ready.Add(providerName);
            _logger.LogDebug("{Kind} provider '{ProviderName}' is ready", providerKind, providerName);
            return;
        }

        buckets.NotReady.Add(providerName);
        if (isRequired)
        {
            buckets.RequiredNotReady.Add(providerName);
        }

        if (outcome.Exception is null)
        {
            _logger.LogWarning("{Kind} provider '{ProviderName}' is not ready (required: {IsRequired})", providerKind, providerName, isRequired);
        }
        else
        {
            _logger.LogWarning(outcome.Exception, "{Kind} provider '{ProviderName}' readiness check failed (required: {IsRequired})", providerKind, providerName, isRequired);
        }
    }

    private (bool isReady, string? message) Summarize(
        HashSet<string> ready,
        HashSet<string> notReady,
        HashSet<string> requiredNotReady)
    {
        // HashSet enumeration order is unspecified — sort ordinally so readiness messages and
        // log output are stable across runs.
        var readyList = string.Join(", ", ready.OrderBy(name => name, StringComparer.Ordinal));
        var notReadyList = string.Join(", ", notReady.OrderBy(name => name, StringComparer.Ordinal));
        var requiredNotReadyList = string.Join(", ", requiredNotReady.OrderBy(name => name, StringComparer.Ordinal));

        // Not ready if any required provider is not reachable
        if (requiredNotReady.Count > 0)
        {
            _logger.LogError("Vault not ready: required providers unavailable: {Providers}", requiredNotReadyList);
            return (isReady: false, message: $"Required providers not ready: {requiredNotReadyList}");
        }

        // Ready if no providers are configured (degenerate case)
        if (ready.Count == 0 && notReady.Count == 0)
        {
            _logger.LogWarning("Vault ready but no providers configured");
            return (isReady: true, message: "Vault ready (no providers configured)");
        }

        // Ready if at least one provider is available (and no required providers are down)
        if (ready.Count > 0)
        {
            var summary = notReady.Count > 0
                ? $"Ready: {readyList}; Unavailable: {notReadyList}"
                : $"Ready: {readyList}";

            _logger.LogDebug("Vault readiness check passed: {Message}", summary);
            return (isReady: true, message: summary);
        }

        // No providers ready
        _logger.LogError("Vault not ready: no providers available");
        return (isReady: false, message: "No providers available");
    }

    private readonly record struct ReadinessBuckets(HashSet<string> Ready, HashSet<string> NotReady, HashSet<string> RequiredNotReady);
}
