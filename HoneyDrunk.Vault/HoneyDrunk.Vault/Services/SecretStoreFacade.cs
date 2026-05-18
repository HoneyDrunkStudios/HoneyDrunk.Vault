using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Shared facade helpers for secret stores that also expose provider-style APIs.
/// </summary>
public static class SecretStoreFacade
{
    /// <summary>
    /// Fetches a secret by provider key through a secret-store delegate.
    /// </summary>
    /// <param name="getSecretAsync">The store get delegate.</param>
    /// <param name="key">The secret key.</param>
    /// <param name="version">The optional secret version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret value.</returns>
    public static Task<SecretValue> FetchSecretAsync(
        Func<SecretIdentifier, CancellationToken, Task<SecretValue>> getSecretAsync,
        string key,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(getSecretAsync);

        return getSecretAsync(new SecretIdentifier(key, version), cancellationToken);
    }

    /// <summary>
    /// Attempts a get-secret operation and converts common Vault exceptions to a failed result.
    /// </summary>
    /// <param name="identifier">The secret identifier.</param>
    /// <param name="getSecretAsync">The store get delegate.</param>
    /// <param name="logger">The optional logger.</param>
    /// <param name="storeName">The store name used in log messages.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the secret or failure details.</returns>
    public static async Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier,
        Func<SecretIdentifier, CancellationToken, Task<SecretValue>> getSecretAsync,
        ILogger? logger = null,
        string? storeName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(getSecretAsync);

        try
        {
            var secretValue = await getSecretAsync(identifier, cancellationToken).ConfigureAwait(false);
            return VaultResult.Success(secretValue);
        }
        catch (SecretNotFoundException ex)
        {
            logger?.LogDebug("Secret '{SecretName}' not found in {StoreName}", identifier.Name, storeName ?? "secret store");
            return VaultResult.Failure<SecretValue>($"Secret '{identifier.Name}' not found: {ex.Message}");
        }
        catch (VaultOperationException ex)
        {
            logger?.LogError(ex, "Vault operation failed retrieving secret '{SecretName}' from {StoreName}", identifier.Name, storeName ?? "secret store");
            return VaultResult.Failure<SecretValue>(ex.Message);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error retrieving secret '{SecretName}' from {StoreName}", identifier.Name, storeName ?? "secret store");
            return VaultResult.Failure<SecretValue>($"Failed to retrieve secret '{identifier.Name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to fetch a secret by provider key through a secret-store try delegate.
    /// </summary>
    /// <param name="tryGetSecretAsync">The store try-get delegate.</param>
    /// <param name="key">The secret key.</param>
    /// <param name="version">The optional secret version.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the secret or failure details.</returns>
    public static Task<VaultResult<SecretValue>> TryFetchSecretAsync(
        Func<SecretIdentifier, CancellationToken, Task<VaultResult<SecretValue>>> tryGetSecretAsync,
        string key,
        string? version = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tryGetSecretAsync);

        return tryGetSecretAsync(new SecretIdentifier(key, version), cancellationToken);
    }

    /// <summary>
    /// Lists secret versions through a secret-store delegate.
    /// </summary>
    /// <param name="listSecretVersionsAsync">The store list-versions delegate.</param>
    /// <param name="key">The secret key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret versions.</returns>
    public static Task<IReadOnlyList<SecretVersion>> ListVersionsAsync(
        Func<string, CancellationToken, Task<IReadOnlyList<SecretVersion>>> listSecretVersionsAsync,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(listSecretVersionsAsync);

        return listSecretVersionsAsync(key, cancellationToken);
    }
}
