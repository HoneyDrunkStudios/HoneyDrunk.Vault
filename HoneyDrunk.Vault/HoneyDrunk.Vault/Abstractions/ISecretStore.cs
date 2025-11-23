using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Vault.Abstractions;

/// <summary>
/// Defines the contract for a secret store provider.
/// </summary>
public interface ISecretStore
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
    /// Lists all versions of a secret.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret versions.</returns>
    Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default);
}
