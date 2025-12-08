using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.File.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.Json;

namespace HoneyDrunk.Vault.Providers.File.Services;

/// <summary>
/// File-based implementation of the secret store for development and local testing.
/// </summary>
public sealed class FileSecretStore : ISecretStore, ISecretProvider, IDisposable
{
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
        ArgumentNullException.ThrowIfNull(identifier);

        _logger.LogDebug("Getting secret '{SecretName}' from file store", identifier.Name);

        if (!_secrets.TryGetValue(identifier.Name, out var value))
        {
            _logger.LogWarning("Secret '{SecretName}' not found in file store", identifier.Name);
            throw new SecretNotFoundException(identifier.Name);
        }

        var secretValue = new SecretValue(identifier, value, "latest");
        _logger.LogDebug("Successfully retrieved secret '{SecretName}' from file store", identifier.Name);

        return Task.FromResult(secretValue);
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        try
        {
            var result = await GetSecretAsync(identifier, cancellationToken).ConfigureAwait(false);
            return VaultResult.Success(result);
        }
        catch (SecretNotFoundException)
        {
            return VaultResult.Failure<SecretValue>($"Secret '{identifier.Name}' not found");
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(secretName));
        }

        if (!_secrets.ContainsKey(secretName))
        {
            throw new SecretNotFoundException(secretName);
        }

        // File store only supports single version
        var versions = new List<SecretVersion>
        {
            new("latest", DateTimeOffset.UtcNow),
        };

        return Task.FromResult<IReadOnlyList<SecretVersion>>(versions);
    }

    /// <inheritdoc/>
    public Task<SecretValue> FetchSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        return GetSecretAsync(new SecretIdentifier(key, version), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryFetchSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        return await TryGetSecretAsync(new SecretIdentifier(key, version), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SecretVersion>> ListVersionsAsync(string key, CancellationToken cancellationToken = default)
    {
        return ListSecretVersionsAsync(key, cancellationToken);
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
