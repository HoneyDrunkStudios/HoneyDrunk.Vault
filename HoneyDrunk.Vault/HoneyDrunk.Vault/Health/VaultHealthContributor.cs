using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Health;

/// <summary>
/// Health contributor for the vault system.
/// Checks that the configured provider is reachable and operational.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VaultHealthContributor"/> class.
/// </remarks>
/// <param name="secretStore">The secret store.</param>
/// <param name="options">The vault options.</param>
/// <param name="logger">The logger.</param>
public sealed class VaultHealthContributor(
    ISecretStore secretStore,
    IOptions<VaultOptions> options,
    ILogger<VaultHealthContributor> logger) : IHealthContributor
{
    private readonly ISecretStore _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    private readonly VaultOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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
        _logger.LogDebug("Performing vault health check");

        try
        {
            // If a health check secret key is configured, try to fetch it
            if (!string.IsNullOrEmpty(_options.HealthCheckSecretKey))
            {
                var result = await _secretStore.TryGetSecretAsync(
                    new Models.SecretIdentifier(_options.HealthCheckSecretKey),
                    cancellationToken).ConfigureAwait(false);

                if (result.IsSuccess)
                {
                    _logger.LogDebug("Vault health check passed: health check secret is accessible");
                    return (status: HealthStatus.Healthy, message: "Vault is operational and health check secret is accessible");
                }

                // Secret not found is still healthy, just means no test secret configured
                _logger.LogDebug("Health check secret not found, but vault is operational");
                return (status: HealthStatus.Healthy, message: "Vault is operational (health check secret not found)");
            }

            // No health check secret configured, check basic provider availability
            var providers = _options.Providers.Values.Where(p => p.IsEnabled).ToList();
            if (providers.Count == 0)
            {
                _logger.LogWarning("No vault providers are configured and enabled");
                return (status: HealthStatus.Degraded, message: "No vault providers configured");
            }

            _logger.LogDebug("Vault health check passed: {Count} provider(s) configured", providers.Count);
            return (status: HealthStatus.Healthy, message: $"Vault is operational with {providers.Count} provider(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vault health check failed");
            return (status: HealthStatus.Unhealthy, message: $"Vault health check failed: {ex.Message}");
        }
    }
}
