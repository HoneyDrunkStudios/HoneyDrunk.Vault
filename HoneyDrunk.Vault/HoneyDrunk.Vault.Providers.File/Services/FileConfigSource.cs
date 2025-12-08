using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Providers.File.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace HoneyDrunk.Vault.Providers.File.Services;

/// <summary>
/// File-based implementation of the configuration source for development and local testing.
/// </summary>
public sealed class FileConfigSource : IConfigSource, IConfigProvider, IDisposable
{
    private readonly FileVaultOptions _options;
    private readonly ILogger<FileConfigSource> _logger;
    private readonly ConcurrentDictionary<string, string> _configValues = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly FileSystemWatcher? _watcher;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileConfigSource"/> class.
    /// </summary>
    /// <param name="options">The file vault options.</param>
    /// <param name="logger">The logger.</param>
    public FileConfigSource(
        IOptions<FileVaultOptions> options,
        ILogger<FileConfigSource> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Load initial config
        LoadConfigAsync().GetAwaiter().GetResult();

        // Set up file watcher if enabled
        if (_options.WatchForChanges && System.IO.File.Exists(_options.ConfigFilePath))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_options.ConfigFilePath));
            var fileName = Path.GetFileName(_options.ConfigFilePath);

            if (!string.IsNullOrEmpty(directory))
            {
                _watcher = new FileSystemWatcher(directory, fileName)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true,
                };
                _watcher.Changed += OnFileChanged;
            }
        }
    }

    // IConfigSource implementation

    /// <inheritdoc/>
    public Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        _logger.LogDebug("Getting configuration value for key '{Key}' from file store", key);

        if (!_configValues.TryGetValue(key, out var value))
        {
            _logger.LogWarning("Configuration key '{Key}' not found in file store", key);
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

        _configValues.TryGetValue(key, out var value);
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
        var value = await TryGetConfigValueAsync(key, cancellationToken).ConfigureAwait(false);
        if (value == null)
        {
            return defaultValue;
        }

        try
        {
            return ConvertValue<T>(value, key);
        }
        catch
        {
            return defaultValue;
        }
    }

    // IConfigProvider implementation

    /// <inheritdoc/>
    public Task<string> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        return GetConfigValueAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<T> GetValueAsync<T>(string path, T defaultValue, CancellationToken cancellationToken = default)
    {
        return TryGetConfigValueAsync(path, defaultValue, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<string?> TryGetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        return TryGetConfigValueAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    Task<T> IConfigProvider.GetValueAsync<T>(string key, CancellationToken cancellationToken)
    {
        return GetConfigValueAsync<T>(key, cancellationToken);
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _watcher?.Dispose();
        _lock.Dispose();
        _disposed = true;
    }

    private static T ConvertValue<T>(string value, string key)
    {
        var targetType = typeof(T);

        if (targetType == typeof(string))
        {
            return (T)(object)value;
        }

        try
        {
            if (targetType == typeof(int) || targetType == typeof(int?))
            {
                return (T)(object)int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(bool) || targetType == typeof(bool?))
            {
                return (T)(object)bool.Parse(value);
            }

            if (targetType == typeof(double) || targetType == typeof(double?))
            {
                return (T)(object)double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(long) || targetType == typeof(long?))
            {
                return (T)(object)long.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            // Try JSON deserialization for complex types
            return JsonSerializer.Deserialize<T>(value)
                ?? throw new InvalidOperationException($"Failed to deserialize configuration value for key '{key}'");
        }
        catch (Exception ex)
        {
            throw new VaultOperationException($"Failed to convert configuration value for key '{key}' to type '{typeof(T).Name}'", ex);
        }
    }

    private async Task LoadConfigAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!System.IO.File.Exists(_options.ConfigFilePath))
            {
                if (_options.CreateIfNotExists)
                {
                    _logger.LogInformation("Creating empty config file at '{Path}'", _options.ConfigFilePath);
                    var directory = Path.GetDirectoryName(_options.ConfigFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    await System.IO.File.WriteAllTextAsync(_options.ConfigFilePath, "{}").ConfigureAwait(false);
                }
                else
                {
                    _logger.LogWarning("Config file not found at '{Path}'", _options.ConfigFilePath);
                    return;
                }
            }

            var json = await System.IO.File.ReadAllTextAsync(_options.ConfigFilePath).ConfigureAwait(false);
            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (config != null)
            {
                _configValues.Clear();
                foreach (var (key, value) in config)
                {
                    _configValues[key] = value;
                }

                _logger.LogInformation("Loaded {Count} config values from file store", config.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading config from file");
        }
        finally
        {
            _lock.Release();
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("Config file changed, reloading");
        _ = LoadConfigAsync();
    }
}
