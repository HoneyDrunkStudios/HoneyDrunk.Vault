using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace HoneyDrunk.Vault.Providers.InMemory.Services;

/// <summary>
/// In-memory implementation of the secret store for testing and development.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemorySecretStore"/> class with initial secrets.
/// </remarks>
/// <param name="secrets">The initial secrets dictionary.</param>
/// <param name="logger">The logger.</param>
public sealed class InMemorySecretStore(
    ConcurrentDictionary<string, string> secrets,
    ILogger<InMemorySecretStore> logger) : ISecretStore, ISecretProvider
{
    private readonly ConcurrentDictionary<string, string> _secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
    private readonly ILogger<InMemorySecretStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySecretStore"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public InMemorySecretStore(ILogger<InMemorySecretStore> logger)
        : this(new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase), logger)
    {
    }

    /// <inheritdoc/>
    public string ProviderName => "in-memory";

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        _logger.LogDebug("Getting secret '{SecretName}' from in-memory store", identifier.Name);

        if (!_secrets.TryGetValue(identifier.Name, out var value))
        {
            _logger.LogWarning("Secret '{SecretName}' not found in in-memory store", identifier.Name);
            throw new SecretNotFoundException(identifier.Name);
        }

        var secretValue = new SecretValue(identifier, value, "latest");
        _logger.LogDebug("Successfully retrieved secret '{SecretName}' from in-memory store", identifier.Name);

        return Task.FromResult(secretValue);
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        _logger.LogDebug("Attempting to get secret '{SecretName}' from in-memory store", identifier.Name);

        try
        {
            var secretValue = await GetSecretAsync(identifier, cancellationToken).ConfigureAwait(false);
            return VaultResult.Success(secretValue);
        }
        catch (SecretNotFoundException ex)
        {
            _logger.LogDebug("Secret '{SecretName}' not found in in-memory store", identifier.Name);
            return VaultResult.Failure<SecretValue>($"Secret '{identifier.Name}' not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretName}' from in-memory store", identifier.Name);
            return VaultResult.Failure<SecretValue>($"Failed to retrieve secret '{identifier.Name}': {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(secretName));
        }

        _logger.LogDebug("Listing versions for secret '{SecretName}' from in-memory store", secretName);

        if (!_secrets.ContainsKey(secretName))
        {
            _logger.LogWarning("Secret '{SecretName}' not found in in-memory store", secretName);
            throw new SecretNotFoundException(secretName);
        }

        // In-memory store only supports a single version
        var versions = new List<SecretVersion>
        {
            new("latest", DateTimeOffset.UtcNow),
        };

        _logger.LogDebug("Listed {Count} version for secret '{SecretName}'", versions.Count, secretName);

        return Task.FromResult<IReadOnlyList<SecretVersion>>(versions);
    }

    /// <summary>
    /// Adds or updates a secret in the in-memory store.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="value">The secret value.</param>
    public void SetSecret(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(value);

        _secrets[name] = value;
        _logger.LogDebug("Secret '{SecretName}' set in in-memory store", name);
    }

    /// <summary>
    /// Removes a secret from the in-memory store.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <returns>True if the secret was removed, false if it did not exist.</returns>
    public bool RemoveSecret(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(name));
        }

        var removed = _secrets.TryRemove(name, out _);
        if (removed)
        {
            _logger.LogDebug("Secret '{SecretName}' removed from in-memory store", name);
        }

        return removed;
    }

    /// <summary>
    /// Clears all secrets from the in-memory store.
    /// </summary>
    public void Clear()
    {
        _secrets.Clear();
        _logger.LogDebug("All secrets cleared from in-memory store");
    }

    /// <inheritdoc/>
    public Task<SecretValue> FetchSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        return GetSecretAsync(new SecretIdentifier(key, version), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryFetchSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        return await TryGetSecretAsync(new SecretIdentifier(key, version), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SecretVersion>> ListVersionsAsync(string key, CancellationToken cancellationToken = default)
    {
        return ListSecretVersionsAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        // In-memory store is always healthy
        return Task.FromResult(true);
    }
}
