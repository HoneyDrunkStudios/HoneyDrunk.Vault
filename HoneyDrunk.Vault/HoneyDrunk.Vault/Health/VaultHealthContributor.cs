using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Health;

/// <summary>
/// Health contributor for the vault system.
/// Performs deep health checks by probing registered providers.
/// Vault is healthy if core is operational and at least one provider is reachable.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VaultHealthContributor"/> class.
/// </remarks>
/// <param name="secretProviders">The registered secret providers.</param>
/// <param name="configProviders">The registered configuration providers.</param>
/// <param name="logger">The logger.</param>
public sealed class VaultHealthContributor(
    IEnumerable<RegisteredSecretProvider> secretProviders,
    IEnumerable<RegisteredConfigSourceProvider> configProviders,
    ILogger<VaultHealthContributor> logger) : IHealthContributor
{
    private readonly IEnumerable<RegisteredSecretProvider> _secretProviders = secretProviders ?? throw new ArgumentNullException(nameof(secretProviders));
    private readonly IEnumerable<RegisteredConfigSourceProvider> _configProviders = configProviders ?? throw new ArgumentNullException(nameof(configProviders));
    private readonly ILogger<VaultHealthContributor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public string Name => "HoneyDrunk.Vault";

    /// <inheritdoc/>
    public int Priority => 100;

    /// <inheritdoc/>
    public bool IsCritical => true;

    /// <inheritdoc/>
    public async Task<(HealthStatus status, string? message)> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Performing vault health check with deep provider probes");

        var buckets = new HealthBuckets(
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal));

        await ProviderProbe.ProbeAllAsync(_secretProviders, _configProviders, buckets, Classify, cancellationToken).ConfigureAwait(false);

        return Summarize(buckets.Healthy, buckets.Unhealthy);
    }

    private void Classify(string providerKind, string providerName, bool isRequired, ProbeOutcome outcome, HealthBuckets buckets)
    {
        _ = isRequired; // Health is unaffected by required-ness; readiness is the gate that cares.

        if (outcome.IsHealthy)
        {
            buckets.Healthy.Add(providerName);
            _logger.LogDebug("{Kind} provider '{ProviderName}' is healthy", providerKind, providerName);
            return;
        }

        buckets.Unhealthy.Add(providerName);
        if (outcome.Exception is null)
        {
            _logger.LogWarning("{Kind} provider '{ProviderName}' health check returned unhealthy", providerKind, providerName);
        }
        else
        {
            _logger.LogWarning(outcome.Exception, "{Kind} provider '{ProviderName}' health check failed", providerKind, providerName);
        }
    }

    private (HealthStatus status, string? message) Summarize(HashSet<string> healthy, HashSet<string> unhealthy)
    {
        // Healthy: at least one provider is reachable
        // Degraded: some providers are unhealthy but at least one is healthy
        // Unhealthy: no providers are healthy
        // HashSet enumeration order is unspecified — sort ordinally so log/health-message output
        // is stable across runs and easy to diff in monitoring.
        var healthyList = string.Join(", ", healthy.OrderBy(name => name, StringComparer.Ordinal));
        var unhealthyList = string.Join(", ", unhealthy.OrderBy(name => name, StringComparer.Ordinal));

        if (healthy.Count == 0 && unhealthy.Count == 0)
        {
            _logger.LogWarning("No vault providers are configured");
            return (status: HealthStatus.Degraded, message: "No vault providers configured");
        }

        if (healthy.Count == 0)
        {
            _logger.LogError("All vault providers are unhealthy: {Providers}", unhealthyList);
            return (status: HealthStatus.Unhealthy, message: $"All providers unhealthy: {unhealthyList}");
        }

        if (unhealthy.Count > 0)
        {
            _logger.LogWarning(
                "Some vault providers are unhealthy. Healthy: {Healthy}, Unhealthy: {Unhealthy}",
                healthyList,
                unhealthyList);
            return (status: HealthStatus.Degraded, message: $"Healthy: {healthyList}; Unhealthy: {unhealthyList}");
        }

        _logger.LogDebug("All vault providers are healthy: {Providers}", healthyList);
        return (status: HealthStatus.Healthy, message: $"All providers healthy: {healthyList}");
    }

    private readonly record struct HealthBuckets(HashSet<string> Healthy, HashSet<string> Unhealthy);
}
