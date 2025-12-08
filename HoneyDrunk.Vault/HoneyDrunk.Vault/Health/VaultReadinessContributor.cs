using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Health;

/// <summary>
/// Readiness contributor for the vault system.
/// Determines if the vault is ready to serve requests.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VaultReadinessContributor"/> class.
/// </remarks>
/// <param name="secretStore">The secret store.</param>
/// <param name="options">The vault options.</param>
/// <param name="logger">The logger.</param>
public sealed class VaultReadinessContributor(
    ISecretStore secretStore,
    IOptions<VaultOptions> options,
    ILogger<VaultReadinessContributor> logger) : IReadinessContributor
{
    private readonly ISecretStore _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    private readonly VaultOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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

        try
        {
            // Check that at least one provider is configured and enabled
            var enabledProviders = _options.Providers.Values.Count(p => p.IsEnabled);
            if (enabledProviders == 0)
            {
                _logger.LogWarning("No vault providers are enabled");
                return (false, "No vault providers are enabled");
            }

            // If warmup keys are configured, verify they were loaded
            if (_options.WarmupKeys.Count > 0 && !string.IsNullOrEmpty(_options.HealthCheckSecretKey))
            {
                var result = await _secretStore.TryGetSecretAsync(
                    new Models.SecretIdentifier(_options.HealthCheckSecretKey),
                    cancellationToken).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Vault readiness check: warmup may not be complete");
                    return (true, "Vault ready (warmup may not be complete)");
                }
            }

            _logger.LogDebug("Vault readiness check passed");
            return (true, $"Vault ready with {enabledProviders} provider(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vault readiness check failed");
            return (false, $"Vault not ready: {ex.Message}");
        }
    }
}
