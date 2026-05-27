using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Shared helpers for dictionary-backed secret store implementations
/// (in-memory, file-backed). Centralises the "log + lookup + throw" shape.
/// </summary>
public static class DictionarySecretLookup
{
    /// <summary>
    /// Gets a secret value from the provided dictionary, logging at each step
    /// and throwing <see cref="SecretNotFoundException"/> when the key is absent.
    /// </summary>
    /// <param name="secrets">The dictionary backing the store.</param>
    /// <param name="identifier">The secret identifier.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="storeName">A short human label used in log messages (e.g., "in-memory store").</param>
    /// <returns>The secret value reporting <c>"latest"</c> as the version.</returns>
    public static Task<SecretValue> GetSecretAsync(
        IDictionary<string, string> secrets,
        SecretIdentifier identifier,
        ILogger logger,
        string storeName)
    {
        ArgumentNullException.ThrowIfNull(secrets);
        ArgumentNullException.ThrowIfNull(identifier);
        ArgumentNullException.ThrowIfNull(logger);

        logger.LogDebug("Getting secret '{SecretName}' from {StoreName}", identifier.Name, storeName);

        if (!secrets.TryGetValue(identifier.Name, out var value))
        {
            logger.LogWarning("Secret '{SecretName}' not found in {StoreName}", identifier.Name, storeName);
            throw new SecretNotFoundException(identifier.Name);
        }

        var secretValue = new SecretValue(identifier, value, "latest");
        logger.LogDebug("Successfully retrieved secret '{SecretName}' from {StoreName}", identifier.Name, storeName);

        return Task.FromResult(secretValue);
    }

    /// <summary>
    /// Lists versions for a secret backed by a dictionary. Dictionary stores expose a
    /// single <c>"latest"</c> version and throw <see cref="SecretNotFoundException"/>
    /// when the key is absent.
    /// </summary>
    /// <param name="secrets">The dictionary backing the store.</param>
    /// <param name="secretName">The secret name.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="storeName">A short human label used in log messages.</param>
    /// <returns>A single-element list with <c>"latest"</c>.</returns>
    public static Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
        IDictionary<string, string> secrets,
        string secretName,
        ILogger logger,
        string storeName)
    {
        ArgumentNullException.ThrowIfNull(secrets);
        ArgumentNullException.ThrowIfNull(logger);

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(secretName));
        }

        logger.LogDebug("Listing versions for secret '{SecretName}' from {StoreName}", secretName, storeName);

        if (!secrets.ContainsKey(secretName))
        {
            logger.LogWarning("Secret '{SecretName}' not found in {StoreName}", secretName, storeName);
            throw new SecretNotFoundException(secretName);
        }

        var versions = new List<SecretVersion>
        {
            new("latest", DateTimeOffset.UtcNow),
        };

        logger.LogDebug("Listed {Count} version(s) for secret '{SecretName}' from {StoreName}", versions.Count, secretName, storeName);

        return Task.FromResult<IReadOnlyList<SecretVersion>>(versions);
    }
}
