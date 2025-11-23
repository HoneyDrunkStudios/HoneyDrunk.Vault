namespace HoneyDrunk.Vault.Abstractions;

/// <summary>
/// Defines the contract for a configuration source provider.
/// </summary>
public interface IConfigSource
{
    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration value.</returns>
    Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration value if found, otherwise null.</returns>
    Task<string?> TryGetConfigValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed configuration value by key.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The typed configuration value.</returns>
    Task<T> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get a typed configuration value by key.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The typed configuration value if found, otherwise the default value.</returns>
    Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);
}
