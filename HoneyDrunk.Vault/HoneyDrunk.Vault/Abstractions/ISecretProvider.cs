using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Vault.Abstractions;

/// <summary>
/// Defines the contract for a backend-specific secret provider.
/// Implementations provide access to secrets from a specific backend (file, Azure Key Vault, AWS, etc.).
/// </summary>
public interface ISecretProvider
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
    Task<SecretValue> FetchSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to fetch a secret from the backend.
    /// </summary>
    /// <param name="key">The secret key/name.</param>
    /// <param name="version">The optional secret version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the secret value if found, or a failure result.</returns>
    Task<VaultResult<SecretValue>> TryFetchSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all versions of a secret from the backend.
    /// </summary>
    /// <param name="key">The secret key/name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret versions.</returns>
    Task<IReadOnlyList<SecretVersion>> ListVersionsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the provider is healthy and can communicate with its backend.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if healthy, false otherwise.</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
