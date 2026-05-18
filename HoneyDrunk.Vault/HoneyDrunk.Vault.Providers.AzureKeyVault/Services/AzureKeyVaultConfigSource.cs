using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Providers.AzureKeyVault.Services;

/// <summary>
/// Azure Key Vault implementation of the configuration source.
/// This implementation uses the secret store to retrieve configuration values stored as secrets.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AzureKeyVaultConfigSource"/> class.
/// </remarks>
/// <param name="secretStore">The secret store.</param>
/// <param name="logger">The logger.</param>
public sealed class AzureKeyVaultConfigSource(
    ISecretStore secretStore,
    ILogger<AzureKeyVaultConfigSource> logger) : IConfigSource
{
    private readonly ISecretStore _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    private readonly ILogger<AzureKeyVaultConfigSource> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        ConfigSourceFacade.ValidateKey(key);

        _logger.LogDebug("Getting configuration value for key '{Key}' from Azure Key Vault", key);

        try
        {
            var secretIdentifier = new SecretIdentifier(NormalizeKeyForKeyVault(key));
            var secret = await _secretStore.GetSecretAsync(secretIdentifier, cancellationToken).ConfigureAwait(false);
            return secret.Value;
        }
        catch (SecretNotFoundException ex)
        {
            _logger.LogWarning("Configuration key '{Key}' not found in Azure Key Vault", key);
            throw new ConfigurationNotFoundException(key, ex);
        }
    }

    /// <inheritdoc/>
    public async Task<string?> TryGetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        ConfigSourceFacade.ValidateKey(key);

        _logger.LogDebug("Attempting to get configuration value for key '{Key}' from Azure Key Vault", key);

        try
        {
            var secretIdentifier = new SecretIdentifier(NormalizeKeyForKeyVault(key));
            var result = await _secretStore.TryGetSecretAsync(secretIdentifier, cancellationToken).ConfigureAwait(false);
            return result.IsSuccess ? result.Value?.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving configuration key '{Key}' from Azure Key Vault", key);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<T> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await ConfigSourceFacade.GetValueAsync<T>(GetConfigValueAsync, key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        return await ConfigSourceFacade.TryGetValueAsync(TryGetConfigValueAsync, key, defaultValue, cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeKeyForKeyVault(string key)
    {
        // Azure Key Vault secret names can only contain alphanumeric characters and hyphens
        // Replace common separators with hyphens
        return key.Replace(":", "-").Replace("__", "-").Replace(".", "-");
    }
}
