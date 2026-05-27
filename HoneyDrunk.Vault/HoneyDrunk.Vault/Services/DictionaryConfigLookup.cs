using HoneyDrunk.Vault.Exceptions;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Shared helpers for dictionary-backed configuration source implementations
/// (in-memory, file-backed). Centralises the "validate + lookup + log" shape.
/// </summary>
public static class DictionaryConfigLookup
{
    /// <summary>
    /// Gets a configuration value from the provided dictionary, throwing
    /// <see cref="ConfigurationNotFoundException"/> when the key is absent.
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

        logger.LogDebug("Successfully retrieved configuration value for key '{Key}'", key);

        return Task.FromResult(value);
    }

    /// <summary>
    /// Attempts to get a configuration value from the provided dictionary, returning
    /// <c>null</c> when the key is absent.
    /// </summary>
    /// <param name="configValues">The dictionary backing the source.</param>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value, or <c>null</c> when missing.</returns>
    public static Task<string?> TryGetConfigValueAsync(
        IDictionary<string, string> configValues,
        string key)
    {
        ArgumentNullException.ThrowIfNull(configValues);

        ConfigSourceFacade.ValidateKey(key);

        configValues.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }
}
