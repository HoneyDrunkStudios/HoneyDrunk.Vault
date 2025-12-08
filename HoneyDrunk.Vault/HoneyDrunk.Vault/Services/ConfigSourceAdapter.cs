using HoneyDrunk.Vault.Abstractions;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Adapts an <see cref="IConfigSource"/> implementation to the <see cref="IConfigProvider"/> interface.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConfigSourceAdapter"/> class.
/// </remarks>
/// <param name="configSource">The config source to adapt.</param>
internal sealed class ConfigSourceAdapter(IConfigSource configSource) : IConfigProvider
{
    private readonly IConfigSource _configSource = configSource ?? throw new ArgumentNullException(nameof(configSource));

    /// <inheritdoc/>
    public Task<string> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        return _configSource.GetConfigValueAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<T> GetValueAsync<T>(string path, T defaultValue, CancellationToken cancellationToken = default)
    {
        return _configSource.TryGetConfigValueAsync(path, defaultValue, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<string?> TryGetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        return _configSource.TryGetConfigValueAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<T> GetValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return _configSource.GetConfigValueAsync<T>(key, cancellationToken);
    }
}
