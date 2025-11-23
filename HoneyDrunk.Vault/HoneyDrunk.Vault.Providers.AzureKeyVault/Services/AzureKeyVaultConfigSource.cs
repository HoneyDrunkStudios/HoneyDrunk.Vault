using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Providers.AzureKeyVault.Services;

/// <summary>
/// Azure Key Vault implementation of the configuration source.
/// This implementation uses the secret store to retrieve configuration values stored as secrets.
/// </summary>
public sealed class AzureKeyVaultConfigSource : IConfigSource
{
    private readonly ISecretStore _secretStore;
    private readonly ILogger<AzureKeyVaultConfigSource> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultConfigSource"/> class.
    /// </summary>
    /// <param name="secretStore">The secret store.</param>
    /// <param name="logger">The logger.</param>
    public AzureKeyVaultConfigSource(
        ISecretStore secretStore,
        ILogger<AzureKeyVaultConfigSource> logger)
    {
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

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
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

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
        var value = await GetConfigValueAsync(key, cancellationToken).ConfigureAwait(false);
        return ConvertValue<T>(value, key);
    }

    /// <inheritdoc/>
    public async Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await TryGetConfigValueAsync(key, cancellationToken).ConfigureAwait(false);

            if (value == null)
            {
                return defaultValue;
            }

            return ConvertValue<T>(value, key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error converting configuration key '{Key}', returning default value", key);
            return defaultValue;
        }
    }

    private static T ConvertValue<T>(string value, string key)
    {
        try
        {
            var targetType = typeof(T);

            if (targetType == typeof(string))
            {
                return (T)(object)value;
            }

            var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                var result = converter.ConvertFromInvariantString(value);
                if (result != null)
                {
                    return (T)result;
                }
            }

            throw new InvalidOperationException($"Cannot convert value to type {targetType.Name}");
        }
        catch (Exception ex)
        {
            throw new VaultOperationException($"Failed to convert configuration value for key '{key}' to type {typeof(T).Name}", ex);
        }
    }

    private static string NormalizeKeyForKeyVault(string key)
    {
        // Azure Key Vault secret names can only contain alphanumeric characters and hyphens
        // Replace common separators with hyphens
        return key.Replace(":", "-").Replace("__", "-").Replace(".", "-");
    }
}
