using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Providers.Configuration.Services;

/// <summary>
/// Configuration-based implementation of the configuration source.
/// </summary>
public sealed class ConfigurationConfigSource : IConfigSource
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationConfigSource> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationConfigSource"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public ConfigurationConfigSource(
        IConfiguration configuration,
        ILogger<ConfigurationConfigSource> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        _logger.LogDebug("Getting configuration value for key '{Key}' from configuration", key);

        var value = _configuration[key];

        if (string.IsNullOrEmpty(value))
        {
            _logger.LogWarning("Configuration key '{Key}' not found", key);
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

        _logger.LogDebug("Attempting to get configuration value for key '{Key}' from configuration", key);

        var value = _configuration[key];

        if (!string.IsNullOrEmpty(value))
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
    public Task<T> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        _logger.LogDebug("Getting typed configuration value for key '{Key}' as type '{Type}' from configuration", key, typeof(T).Name);

        try
        {
            var value = _configuration.GetValue<T>(key);

            if (value == null)
            {
                _logger.LogWarning("Configuration key '{Key}' not found", key);
                throw new ConfigurationNotFoundException(key);
            }

            _logger.LogDebug("Successfully retrieved typed configuration value for key '{Key}'", key);

            return Task.FromResult(value);
        }
        catch (Exception ex) when (ex is not ConfigurationNotFoundException)
        {
            _logger.LogError(ex, "Error retrieving typed configuration value for key '{Key}' as type '{Type}'", key, typeof(T).Name);
            throw new VaultOperationException($"Failed to retrieve configuration value for key '{key}' as type '{typeof(T).Name}'", ex);
        }
    }

    /// <inheritdoc/>
    public Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        _logger.LogDebug("Attempting to get typed configuration value for key '{Key}' as type '{Type}' from configuration", key, typeof(T).Name);

        try
        {
            var value = _configuration.GetValue<T>(key, defaultValue)!;
            _logger.LogDebug("Successfully retrieved typed configuration value for key '{Key}'", key);
            return Task.FromResult(value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error retrieving typed configuration value for key '{Key}', returning default value", key);
            return Task.FromResult(defaultValue!);
        }
    }
}
