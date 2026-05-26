using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Providers.File.Configuration;
using HoneyDrunk.Vault.Services;
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
        ConfigSourceFacade.ValidateKey(key);

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
    public async Task<T> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await ConfigSourceFacade.GetValueAsync<T>(GetConfigValueAsync, key, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<string?> TryGetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        ConfigSourceFacade.ValidateKey(key);

        _configValues.TryGetValue(key, out var value);
        return Task.FromResult(value);
    }

    /// <inheritdoc/>
    public async Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        return await ConfigSourceFacade.TryGetValueAsync(TryGetConfigValueAsync, key, defaultValue, _logger, cancellationToken).ConfigureAwait(false);
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
