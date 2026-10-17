using HoneyDrunk.Vault.Services;

namespace HoneyDrunk.Vault.Abstractions;

/// <summary>
/// Defines the contract for a configuration source provider.
/// </summary>
/// <remarks>
/// Concrete implementations supply the two string-returning methods
/// (<see cref="GetConfigValueAsync(string, System.Threading.CancellationToken)"/> and
/// <see cref="TryGetConfigValueAsync(string, System.Threading.CancellationToken)"/>).
/// The generic typed overloads have default interface implementations that delegate to
/// <see cref="ConfigSourceFacade"/> against those string-returning methods; implementers
/// only override them when they need provider-specific conversion or richer telemetry
/// (the default implementations log nothing — <c>logger: null</c>).
/// </remarks>
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
    /// Gets a typed configuration value by key.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The typed configuration value.</returns>
    async Task<T> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return await ConfigSourceFacade.GetValueAsync<T>(GetConfigValueAsync, key, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Attempts to get a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configuration value if found, otherwise null.</returns>
    Task<string?> TryGetConfigValueAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get a typed configuration value by key.
    /// </summary>
    /// <typeparam name="T">The type to convert the value to.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value to return if the key is not found.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The typed configuration value if found, otherwise the default value.</returns>
    async Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        return await ConfigSourceFacade.TryGetValueAsync(TryGetConfigValueAsync, key, defaultValue, logger: null, cancellationToken).ConfigureAwait(false);
    }
}
