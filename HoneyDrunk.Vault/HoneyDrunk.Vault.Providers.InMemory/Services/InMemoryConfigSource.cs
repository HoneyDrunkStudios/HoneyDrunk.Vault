using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HoneyDrunk.Vault.Providers.InMemory.Services;

/// <summary>
/// In-memory implementation of the configuration source for testing and development.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryConfigSource"/> class with initial values.
/// </remarks>
/// <param name="configValues">The initial configuration values dictionary.</param>
/// <param name="logger">The logger.</param>
public sealed class InMemoryConfigSource(
    ConcurrentDictionary<string, string> configValues,
    ILogger<InMemoryConfigSource> logger) : IConfigSource, IConfigSourceProvider
{
    private readonly ConcurrentDictionary<string, string> _configValues = configValues ?? throw new ArgumentNullException(nameof(configValues));
    private readonly ILogger<InMemoryConfigSource> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConfigSource"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public InMemoryConfigSource(ILogger<InMemoryConfigSource> logger)
        : this(new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase), logger)
    {
    }

    /// <inheritdoc/>
    public string ProviderName => "in-memory";

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        // In-memory store is always healthy
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        _logger.LogDebug("Getting configuration value for key '{Key}' from in-memory store", key);

        if (!_configValues.TryGetValue(key, out var value))
        {
            _logger.LogWarning("Configuration key '{Key}' not found in in-memory store", key);
            throw new ConfigurationNotFoundException(key);
        }

        _logger.LogDebug("Successfully retrieved configuration value for key '{Key}'", key);

        return Task.FromResult(value);
    }

    /// <inheritdoc/>
    public Task<string?> TryGetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        _logger.LogDebug("Attempting to get configuration value for key '{Key}' from in-memory store", key);

        _configValues.TryGetValue(key, out var value);

        if (value != null)
        {
            _logger.LogDebug("Successfully retrieved configuration value for key '{Key}'", key);
        }
        else
        {
            _logger.LogDebug("Configuration value for key '{Key}' not found", key);
        }

        return Task.FromResult(value);
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

    /// <summary>
    /// Adds or updates a configuration value in the in-memory store.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    public void SetConfigValue(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        ArgumentNullException.ThrowIfNull(value);

        _configValues[key] = value;
        _logger.LogDebug("Configuration value for key '{Key}' set in in-memory store", key);
    }

    /// <summary>
    /// Removes a configuration value from the in-memory store.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>True if the value was removed, false if it did not exist.</returns>
    public bool RemoveConfigValue(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        var removed = _configValues.TryRemove(key, out _);
        if (removed)
        {
            _logger.LogDebug("Configuration value for key '{Key}' removed from in-memory store", key);
        }

        return removed;
    }

    /// <summary>
    /// Clears all configuration values from the in-memory store.
    /// </summary>
    public void Clear()
    {
        _configValues.Clear();
        _logger.LogDebug("All configuration values cleared from in-memory store");
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
}
