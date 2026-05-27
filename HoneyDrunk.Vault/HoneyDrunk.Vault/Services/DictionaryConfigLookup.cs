using HoneyDrunk.Vault.Exceptions;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Shared helpers for dictionary-backed configuration source implementations
/// (in-memory, file-backed). Each helper validates the key, performs the lookup,
/// emits the standard log shape (when a logger is supplied), and either returns
/// the value or throws <see cref="ConfigurationNotFoundException"/>.
/// </summary>
public static class DictionaryConfigLookup
{
    /// <summary>
    /// Gets a configuration value from the provided dictionary, throwing
    /// <see cref="ConfigurationNotFoundException"/> when the key is absent.
    /// Logs the attempt, the miss (as a warning), and the success.
    /// </summary>
    /// <param name="configValues">The dictionary backing the source.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="storeName">A short human label used in log messages.</param>
    /// <returns>The configuration value.</returns>
    public static Task<string> GetConfigValueAsync(
        IDictionary<string, string> configValues,
        string key,
        ILogger logger,
        string storeName)
    {
        ArgumentNullException.ThrowIfNull(configValues);
        ArgumentNullException.ThrowIfNull(logger);

        ConfigSourceFacade.ValidateKey(key);

        logger.LogDebug("Getting configuration value for key '{Key}' from {StoreName}", key, storeName);

        if (!configValues.TryGetValue(key, out var value))
        {
            logger.LogWarning("Configuration key '{Key}' not found in {StoreName}", key, storeName);
            throw new ConfigurationNotFoundException(key);
        }

        logger.LogDebug("Successfully retrieved configuration value for key '{Key}' from {StoreName}", key, storeName);

        return Task.FromResult(value);
    }

    /// <summary>
    /// Attempts to get a configuration value from the provided dictionary, returning
    /// <c>null</c> when the key is absent. When a logger is supplied, emits debug
    /// traces for the attempt and the success/miss decision (matching the legacy
    /// per-provider shape).
    /// </summary>
    /// <param name="configValues">The dictionary backing the source.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="logger">The optional logger. When <c>null</c>, no log lines are emitted.</param>
    /// <param name="storeName">A short human label used in log messages.</param>
    /// <returns>The configuration value, or <c>null</c> when missing.</returns>
    public static Task<string?> TryGetConfigValueAsync(
        IDictionary<string, string> configValues,
        string key,
        ILogger? logger = null,
        string? storeName = null)
    {
        ArgumentNullException.ThrowIfNull(configValues);

        ConfigSourceFacade.ValidateKey(key);

        logger?.LogDebug("Attempting to get configuration value for key '{Key}' from {StoreName}", key, storeName);

        configValues.TryGetValue(key, out var value);

        if (value != null)
        {
            logger?.LogDebug("Successfully retrieved configuration value for key '{Key}' from {StoreName}", key, storeName);
        }
        else
        {
            logger?.LogDebug("Configuration value for key '{Key}' not found in {StoreName}", key, storeName);
        }

        return Task.FromResult(value);
    }
}
