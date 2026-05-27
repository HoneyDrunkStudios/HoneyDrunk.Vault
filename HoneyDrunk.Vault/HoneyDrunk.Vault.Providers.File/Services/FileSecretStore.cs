using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.File.Configuration;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace HoneyDrunk.Vault.Providers.File.Services;

/// <summary>
/// File-based implementation of the secret store for development and local testing.
/// </summary>
public sealed class FileSecretStore : ISecretProvider, IDisposable
{
    private const string StoreName = "file store";

    private readonly FileVaultOptions _options;
    private readonly ILogger<FileSecretStore> _logger;
    private readonly ConcurrentDictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly FileSystemWatcher? _watcher;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSecretStore"/> class.
    /// </summary>
    /// <param name="options">The file vault options.</param>
    /// <param name="logger">The logger.</param>
    public FileSecretStore(
        IOptions<FileVaultOptions> options,
        ILogger<FileSecretStore> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Load initial secrets
        LoadSecretsAsync().GetAwaiter().GetResult();

        // Set up file watcher if enabled
        if (_options.WatchForChanges && System.IO.File.Exists(_options.SecretsFilePath))
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_options.SecretsFilePath));
            var fileName = Path.GetFileName(_options.SecretsFilePath);

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

    /// <inheritdoc/>
    public string ProviderName => "file";

    /// <inheritdoc/>
    public bool IsAvailable => System.IO.File.Exists(_options.SecretsFilePath) || _options.CreateIfNotExists;

    /// <inheritdoc/>
    public Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        return DictionarySecretLookup.GetSecretAsync(_secrets, identifier, _logger, StoreName);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        return DictionarySecretLookup.ListSecretVersionsAsync(_secrets, secretName, _logger, StoreName);
    }

    /// <inheritdoc/>
    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var fileExists = System.IO.File.Exists(_options.SecretsFilePath);
        return Task.FromResult(fileExists || _options.CreateIfNotExists);
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

    private async Task LoadSecretsAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!System.IO.File.Exists(_options.SecretsFilePath))
            {
                if (_options.CreateIfNotExists)
                {
                    _logger.LogInformation("Creating empty secrets file at '{Path}'", _options.SecretsFilePath);
                    var directory = Path.GetDirectoryName(_options.SecretsFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    await System.IO.File.WriteAllTextAsync(_options.SecretsFilePath, "{}").ConfigureAwait(false);
                }
                else
                {
                    _logger.LogWarning("Secrets file not found at '{Path}'", _options.SecretsFilePath);
                    return;
                }
            }

            var json = await System.IO.File.ReadAllTextAsync(_options.SecretsFilePath).ConfigureAwait(false);
            var secrets = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

            if (secrets != null)
            {
                _secrets.Clear();
                foreach (var (key, value) in secrets)
                {
                    _secrets[key] = value;
                }

                _logger.LogInformation("Loaded {Count} secrets from file store", secrets.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading secrets from file");
        }
        finally
        {
            _lock.Release();
        }
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        _logger.LogDebug("Secrets file changed, reloading");
        _ = LoadSecretsAsync();
    }
}
