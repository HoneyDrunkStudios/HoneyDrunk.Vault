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

        var healthyProviders = new List<string>();
        var unhealthyProviders = new List<string>();

        // Check secret providers
        foreach (var registered in _secretProviders.Where(p => p.Registration.IsEnabled))
        {
            var providerName = registered.Provider.ProviderName;

            try
            {
                var isHealthy = await registered.Provider.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

                if (isHealthy)
                {
                    healthyProviders.Add(providerName);
                    _logger.LogDebug("Secret provider '{ProviderName}' is healthy", providerName);
                }
                else
                {
                    unhealthyProviders.Add(providerName);
                    _logger.LogWarning("Secret provider '{ProviderName}' health check returned unhealthy", providerName);
                }
            }
            catch (Exception ex)
            {
                unhealthyProviders.Add(providerName);
                _logger.LogWarning(ex, "Secret provider '{ProviderName}' health check failed", providerName);
            }
        }

        // Check config providers
        foreach (var registered in _configProviders.Where(p => p.Registration.IsEnabled))
        {
            var providerName = registered.Provider.ProviderName;

            try
            {
                var isHealthy = await registered.Provider.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

                if (isHealthy)
                {
                    if (!healthyProviders.Contains(providerName))
                    {
                        healthyProviders.Add(providerName);
                    }

                    _logger.LogDebug("Config provider '{ProviderName}' is healthy", providerName);
                }
                else
                {
                    if (!unhealthyProviders.Contains(providerName))
                    {
                        unhealthyProviders.Add(providerName);
                    }

                    _logger.LogWarning("Config provider '{ProviderName}' health check returned unhealthy", providerName);
                }
            }
            catch (Exception ex)
            {
                if (!unhealthyProviders.Contains(providerName))
                {
                    unhealthyProviders.Add(providerName);
                }

                _logger.LogWarning(ex, "Config provider '{ProviderName}' health check failed", providerName);
            }
        }

        // Determine overall health status
        // Healthy: at least one provider is reachable
        // Degraded: some providers are unhealthy but at least one is healthy
        // Unhealthy: no providers are healthy
        if (healthyProviders.Count == 0 && unhealthyProviders.Count == 0)
        {
            _logger.LogWarning("No vault providers are configured");
            return (status: HealthStatus.Degraded, "No vault providers configured");
        }

        if (healthyProviders.Count == 0)
        {
            _logger.LogError(
                "All vault providers are unhealthy: {Providers}",
                string.Join(", ", unhealthyProviders));
            return (status: HealthStatus.Unhealthy, $"All providers unhealthy: {string.Join(", ", unhealthyProviders)}");
        }

        if (unhealthyProviders.Count > 0)
        {
            _logger.LogWarning(
                "Some vault providers are unhealthy. Healthy: {Healthy}, Unhealthy: {Unhealthy}",
                string.Join(", ", healthyProviders),
                string.Join(", ", unhealthyProviders));
            return (status: HealthStatus.Degraded, $"Healthy: {string.Join(", ", healthyProviders)}; Unhealthy: {string.Join(", ", unhealthyProviders)}");
        }

        _logger.LogDebug(
            "All vault providers are healthy: {Providers}",
            string.Join(", ", healthyProviders));
        return (status: HealthStatus.Healthy, $"All providers healthy: {string.Join(", ", healthyProviders)}");
    }
}
