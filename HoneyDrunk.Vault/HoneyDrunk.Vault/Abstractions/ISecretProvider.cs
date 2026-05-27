using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;

namespace HoneyDrunk.Vault.Abstractions;

/// <summary>
/// Defines the contract for a backend-specific secret provider.
/// Implementations provide access to secrets from a specific backend (file, Azure Key Vault, AWS, etc.).
/// </summary>
/// <remarks>
/// Extends <see cref="ISecretStore"/> with backend-aware metadata
/// (<see cref="ProviderName"/>, <see cref="IsAvailable"/>, <see cref="CheckHealthAsync(System.Threading.CancellationToken)"/>).
/// The provider-key-flavored fetch overloads
/// (<see cref="FetchSecretAsync"/>, <see cref="TryFetchSecretAsync"/>, <see cref="ListVersionsAsync"/>)
/// have default interface implementations that delegate to <see cref="SecretStoreFacade"/>
/// against the inherited <see cref="ISecretStore"/> members; implementers only override when
/// they need provider-specific behavior.
/// </remarks>
public interface ISecretProvider : ISecretStore
{
    /// <summary>
    /// Gets the logical name of this provider (e.g., "file", "azure-keyvault", "aws-secretsmanager").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets a value indicating whether this provider is available and properly configured.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Fetches a secret from the backend.
    /// </summary>
    /// <param name="key">The secret key/name.</param>
    /// <param name="version">The optional secret version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret value.</returns>
    Task<SecretValue> FetchSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        return SecretStoreFacade.FetchSecretAsync(GetSecretAsync, key, version, cancellationToken);
    }

    /// <summary>
    /// Attempts to fetch a secret from the backend.
    /// </summary>
    /// <param name="key">The secret key/name.</param>
    /// <param name="version">The optional secret version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the secret value if found, or a failure result.</returns>
    Task<VaultResult<SecretValue>> TryFetchSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        return SecretStoreFacade.TryFetchSecretAsync(TryGetSecretAsync, key, version, cancellationToken);
    }

    /// <summary>
    /// Lists all versions of a secret from the backend.
    /// </summary>
    /// <param name="key">The secret key/name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret versions.</returns>
    Task<IReadOnlyList<SecretVersion>> ListVersionsAsync(string key, CancellationToken cancellationToken = default)
    {
        return SecretStoreFacade.ListVersionsAsync(ListSecretVersionsAsync, key, cancellationToken);
    }

    /// <summary>
    /// Checks if the provider is healthy and can communicate with its backend.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if healthy, false otherwise.</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
