using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Lifecycle;

/// <summary>
/// Startup hook for the vault system.
/// Validates provider configuration and optionally warms caches.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VaultStartupHook"/> class.
/// </remarks>
/// <param name="secretStore">The secret store.</param>
/// <param name="options">The vault options.</param>
/// <param name="logger">The logger.</param>
public sealed class VaultStartupHook(
    ISecretStore secretStore,
    IOptions<VaultOptions> options,
    ILogger<VaultStartupHook> logger) : IStartupHook
{
    private readonly ISecretStore _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    private readonly VaultOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<VaultStartupHook> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public int Priority => 100; // Run after core services are initialized

    /// <inheritdoc/>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Vault startup hook executing");

        // Validate provider configuration
        ValidateConfiguration();

        // Warm cache if configured
        if (_options.WarmupKeys.Count > 0)
        {
            await WarmCacheAsync(cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Vault startup hook completed");
    }

    private void ValidateConfiguration()
    {
        var enabledProviders = _options.Providers.Values.Where(p => p.IsEnabled).ToList();

        if (enabledProviders.Count == 0)
        {
            _logger.LogWarning("No vault providers are configured and enabled");
            return;
        }

        foreach (var provider in enabledProviders)
        {
            _logger.LogDebug(
                "Provider '{Name}' configured with type '{Type}' and priority {Priority}",
                provider.Name,
                provider.ProviderType,
                provider.Priority);

            // Validate provider-specific configuration
            ValidateProviderConfiguration(provider);
        }

        if (string.IsNullOrEmpty(_options.DefaultProvider))
        {
            _logger.LogWarning("No default provider specified, using first enabled provider");
        }
    }

    private void ValidateProviderConfiguration(ProviderRegistration provider)
    {
        switch (provider.ProviderType)
        {
            case ProviderType.AzureKeyVault:
                if (!provider.Settings.TryGetValue("VaultUri", out var vaultUri) ||
                    string.IsNullOrEmpty(vaultUri))
                {
                    throw new InvalidOperationException(
                        $"Azure Key Vault provider '{provider.Name}' requires VaultUri to be configured");
                }

                break;

            case ProviderType.AwsSecretsManager:
                if (!provider.Settings.TryGetValue("Region", out var region) ||
                    string.IsNullOrEmpty(region))
                {
                    _logger.LogWarning(
                        "AWS Secrets Manager provider '{Name}' has no region configured, will use default",
                        provider.Name);
                }

                break;

            case ProviderType.File:
                if (!provider.Settings.TryGetValue("FilePath", out var filePath) ||
                    string.IsNullOrEmpty(filePath))
                {
                    _logger.LogWarning(
                        "File provider '{Name}' has no file path configured, using default",
                        provider.Name);
                }

                break;

            case ProviderType.InMemory:
            case ProviderType.Configuration:
                // No required configuration
                break;
        }
    }

    private async Task WarmCacheAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Warming vault cache with {Count} keys", _options.WarmupKeys.Count);

        var successCount = 0;
        var failCount = 0;

        foreach (var key in _options.WarmupKeys)
        {
            try
            {
                var result = await _secretStore.TryGetSecretAsync(
                    new Models.SecretIdentifier(key),
                    cancellationToken).ConfigureAwait(false);

                if (result.IsSuccess)
                {
                    successCount++;
                    _logger.LogDebug("Warmed cache for secret '{Key}'", key);
                }
                else
                {
                    failCount++;
                    _logger.LogWarning("Failed to warm cache for secret '{Key}': not found", key);
                }
            }
            catch (Exception ex)
            {
                failCount++;
                _logger.LogWarning(ex, "Failed to warm cache for secret '{Key}'", key);
            }
        }

        _logger.LogInformation(
            "Cache warmup complete: {Success} succeeded, {Failed} failed",
            successCount,
            failCount);
    }
}
