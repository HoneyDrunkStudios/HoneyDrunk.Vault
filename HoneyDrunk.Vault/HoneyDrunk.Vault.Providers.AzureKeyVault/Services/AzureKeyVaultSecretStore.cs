using Azure;
using Azure.Security.KeyVault.Secrets;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Providers.AzureKeyVault.Services;

/// <summary>
/// Azure Key Vault implementation of the secret store.
/// </summary>
public sealed class AzureKeyVaultSecretStore : ISecretStore
{
    private readonly SecretClient _secretClient;
    private readonly ILogger<AzureKeyVaultSecretStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultSecretStore"/> class.
    /// </summary>
    /// <param name="secretClient">The Azure Key Vault secret client.</param>
    /// <param name="logger">The logger.</param>
    public AzureKeyVaultSecretStore(
        SecretClient secretClient,
        ILogger<AzureKeyVaultSecretStore> logger)
    {
        _secretClient = secretClient ?? throw new ArgumentNullException(nameof(secretClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        _logger.LogDebug("Getting secret '{SecretName}' from Azure Key Vault", identifier.Name);

        try
        {
            KeyVaultSecret secret;
            if (!string.IsNullOrWhiteSpace(identifier.Version))
            {
                secret = await _secretClient.GetSecretAsync(identifier.Name, identifier.Version, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                secret = await _secretClient.GetSecretAsync(identifier.Name, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            var secretValue = new SecretValue(
                identifier,
                secret.Value,
                secret.Properties.Version);

            _logger.LogDebug("Successfully retrieved secret '{SecretName}' version '{Version}'", identifier.Name, secret.Properties.Version);

            return secretValue;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret '{SecretName}' not found in Azure Key Vault", identifier.Name);
            throw new SecretNotFoundException(identifier.Name, ex);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Key Vault request failed for secret '{SecretName}'", identifier.Name);
            throw new VaultOperationException($"Failed to retrieve secret '{identifier.Name}' from Azure Key Vault", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving secret '{SecretName}' from Azure Key Vault", identifier.Name);
            throw new VaultOperationException($"Failed to retrieve secret '{identifier.Name}' from Azure Key Vault", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        _logger.LogDebug("Attempting to get secret '{SecretName}' from Azure Key Vault", identifier.Name);

        try
        {
            var secretValue = await GetSecretAsync(identifier, cancellationToken).ConfigureAwait(false);
            return VaultResult.Success(secretValue);
        }
        catch (SecretNotFoundException ex)
        {
            _logger.LogDebug("Secret '{SecretName}' not found in Azure Key Vault", identifier.Name);
            return VaultResult.Failure<SecretValue>($"Secret '{identifier.Name}' not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretName}' from Azure Key Vault", identifier.Name);
            return VaultResult.Failure<SecretValue>($"Failed to retrieve secret '{identifier.Name}': {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(secretName));
        }

        _logger.LogDebug("Listing versions for secret '{SecretName}' from Azure Key Vault", secretName);

        try
        {
            var versions = new List<SecretVersion>();

            await foreach (var properties in _secretClient.GetPropertiesOfSecretVersionsAsync(secretName, cancellationToken).ConfigureAwait(false))
            {
                if (properties.Enabled == true)
                {
                    versions.Add(new SecretVersion(
                        properties.Version,
                        properties.CreatedOn ?? DateTimeOffset.UtcNow));
                }
            }

            _logger.LogDebug("Listed {Count} versions for secret '{SecretName}'", versions.Count, secretName);

            return versions;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Secret '{SecretName}' not found in Azure Key Vault", secretName);
            throw new SecretNotFoundException(secretName, ex);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Key Vault request failed while listing versions for secret '{SecretName}'", secretName);
            throw new VaultOperationException($"Failed to list versions for secret '{secretName}' from Azure Key Vault", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error listing versions for secret '{SecretName}' from Azure Key Vault", secretName);
            throw new VaultOperationException($"Failed to list versions for secret '{secretName}' from Azure Key Vault", ex);
        }
    }
}
