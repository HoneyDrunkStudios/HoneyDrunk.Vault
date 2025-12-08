namespace HoneyDrunk.Vault.Abstractions;

/// <summary>
/// Defines the contract for a configuration provider.
/// Provides async access to configuration values with typed support.
/// </summary>
public interface IConfigProvider
{
    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration value.</returns>
    /// <exception cref="Exceptions.ConfigurationNotFoundException">Thrown when the key is not found.</exception>
    Task<string> GetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed configuration value by key with a default fallback.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="path">The configuration path/key.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The typed configuration value if found, otherwise the default value.</returns>
    Task<T> GetValueAsync<T>(string path, T defaultValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration value if found, otherwise null.</returns>
    Task<string?> TryGetValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed configuration value by key.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The typed configuration value.</returns>
    /// <exception cref="Exceptions.ConfigurationNotFoundException">Thrown when the key is not found.</exception>
    Task<T> GetValueAsync<T>(string key, CancellationToken cancellationToken = default);
}
