using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;

namespace HoneyDrunk.Vault.Abstractions;

/// <summary>
/// Defines the contract for a secret store provider.
/// </summary>
/// <remarks>
/// Concrete implementations supply the two operations that touch the backend
/// (<see cref="GetSecretAsync(SecretIdentifier, System.Threading.CancellationToken)"/> and
/// <see cref="ListSecretVersionsAsync(string, System.Threading.CancellationToken)"/>).
/// <see cref="TryGetSecretAsync(SecretIdentifier, System.Threading.CancellationToken)"/>
/// is supplied as a default interface method that delegates to
/// <see cref="SecretStoreFacade"/> against <see cref="GetSecretAsync(SecretIdentifier, System.Threading.CancellationToken)"/>
/// (the default implementation logs nothing — <c>logger: null</c>). Implementers
/// only override it when they need provider-specific telemetry.
/// </remarks>
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
    async Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        return await SecretStoreFacade.TryGetSecretAsync(identifier, GetSecretAsync, logger: null, storeName: null, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Lists all versions of a secret.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret versions.</returns>
    Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default);
}
