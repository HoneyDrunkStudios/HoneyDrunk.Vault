using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;
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
    ILogger<InMemorySecretStore> logger) : ISecretProvider
{
    private const string StoreName = "in-memory store";

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
        return DictionarySecretLookup.GetSecretAsync(_secrets, identifier, _logger, StoreName);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        return DictionarySecretLookup.ListSecretVersionsAsync(_secrets, secretName, _logger, StoreName);
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
    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        // In-memory store is always healthy
        return Task.FromResult(true);
    }
}
