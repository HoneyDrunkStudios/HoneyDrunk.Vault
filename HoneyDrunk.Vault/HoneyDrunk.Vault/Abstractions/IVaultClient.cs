using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Vault.Abstractions;

/// <summary>
/// Defines the contract for the main vault client orchestrator.
/// </summary>
public interface IVaultClient
{
    /// <summary>
    /// Gets a secret by its identifier.
    /// </summary>
    /// <param name="identifier">The secret identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret value.</returns>
    Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get a secret by its identifier.
    /// </summary>
    /// <param name="identifier">The secret identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the secret value if found, or a failure result.</returns>
    Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Lists all versions of a secret.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret versions.</returns>
    Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default);
}
