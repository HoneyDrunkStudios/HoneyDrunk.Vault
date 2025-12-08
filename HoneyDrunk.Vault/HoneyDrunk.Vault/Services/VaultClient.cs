using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Central orchestrator for vault operations, implementing the resolution pipeline.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VaultClient"/> class.
/// </remarks>
/// <param name="secretStore">The secret store provider.</param>
/// <param name="configSource">The configuration source provider.</param>
/// <param name="logger">The logger.</param>
public sealed class VaultClient(
    ISecretStore secretStore,
    IConfigSource configSource,
    ILogger<VaultClient> logger) : IVaultClient
{
    private readonly ISecretStore _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    private readonly IConfigSource _configSource = configSource ?? throw new ArgumentNullException(nameof(configSource));
    private readonly ILogger<VaultClient> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting secret '{SecretName}' with version '{Version}'", identifier.Name, identifier.Version);

        try
        {
            var result = await _secretStore.GetSecretAsync(identifier, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully retrieved secret '{SecretName}'", identifier.Name);
            return result;
        }
        catch (Exception ex) when (ex is not SecretNotFoundException)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretName}'", identifier.Name);
            throw new VaultOperationException($"Failed to retrieve secret '{identifier.Name}'", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Attempting to get secret '{SecretName}' with version '{Version}'", identifier.Name, identifier.Version);

        try
        {
            var result = await _secretStore.TryGetSecretAsync(identifier, cancellationToken).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                _logger.LogDebug("Successfully retrieved secret '{SecretName}'", identifier.Name);
            }
            else
            {
                _logger.LogDebug("Secret '{SecretName}' not found: {ErrorMessage}", identifier.Name, result.ErrorMessage);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attempting to retrieve secret '{SecretName}'", identifier.Name);
            return VaultResult.Failure<SecretValue>($"Failed to retrieve secret '{identifier.Name}': {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting configuration value for key '{Key}'", key);

        try
        {
            var result = await _configSource.GetConfigValueAsync(key, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully retrieved configuration value for key '{Key}'", key);
            return result;
        }
        catch (Exception ex) when (ex is not ConfigurationNotFoundException)
        {
            _logger.LogError(ex, "Error retrieving configuration value for key '{Key}'", key);
            throw new VaultOperationException($"Failed to retrieve configuration value for key '{key}'", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<string?> TryGetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Attempting to get configuration value for key '{Key}'", key);

        try
        {
            var result = await _configSource.TryGetConfigValueAsync(key, cancellationToken).ConfigureAwait(false);

            if (result != null)
            {
                _logger.LogDebug("Successfully retrieved configuration value for key '{Key}'", key);
            }
            else
            {
                _logger.LogDebug("Configuration value for key '{Key}' not found", key);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error attempting to retrieve configuration value for key '{Key}'", key);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<T> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting typed configuration value for key '{Key}' as type '{Type}'", key, typeof(T).Name);

        try
        {
            var result = await _configSource.GetConfigValueAsync<T>(key, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully retrieved typed configuration value for key '{Key}'", key);
            return result;
        }
        catch (Exception ex) when (ex is not ConfigurationNotFoundException)
        {
            _logger.LogError(ex, "Error retrieving typed configuration value for key '{Key}' as type '{Type}'", key, typeof(T).Name);
            throw new VaultOperationException($"Failed to retrieve configuration value for key '{key}' as type '{typeof(T).Name}'", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Attempting to get typed configuration value for key '{Key}' as type '{Type}'", key, typeof(T).Name);

        try
        {
            var result = await _configSource.TryGetConfigValueAsync(key, defaultValue, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully retrieved typed configuration value for key '{Key}'", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error attempting to retrieve typed configuration value for key '{Key}', returning default value", key);
            return defaultValue;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Listing versions for secret '{SecretName}'", secretName);

        try
        {
            var result = await _secretStore.ListSecretVersionsAsync(secretName, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Successfully listed {Count} versions for secret '{SecretName}'", result.Count, secretName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing versions for secret '{SecretName}'", secretName);
            throw new VaultOperationException($"Failed to list versions for secret '{secretName}'", ex);
        }
    }
}
