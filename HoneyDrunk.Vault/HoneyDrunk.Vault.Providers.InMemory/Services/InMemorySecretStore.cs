using HoneyDrunk.Kernel.Abstractions.Context;
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
        => GetSecretAsync(identifier, context: null, cancellationToken);

    /// <summary>
    /// Gets a secret by its identifier with grid context for distributed tracing.
    /// </summary>
    /// <param name="identifier">The secret identifier.</param>
    /// <param name="context">The optional grid context for correlation and tracing.</param>
    /// <returns>The secret value.</returns>
    public Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier,
        IGridContext? context,
        CancellationToken _ = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        using var scope = CreateLogScope(context);

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
    public Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        => TryGetSecretAsync(identifier, context: null, cancellationToken);

    /// <summary>
    /// Attempts to get a secret by its identifier with grid context for distributed tracing.
    /// </summary>
    /// <param name="identifier">The secret identifier.</param>
    /// <param name="context">The optional grid context for correlation and tracing.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the secret value if found, or a failure result.</returns>
    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier,
        IGridContext? context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        using var scope = CreateLogScope(context);

        _logger.LogDebug("Attempting to get secret '{SecretName}' from in-memory store", identifier.Name);

        try
        {
            var secretValue = await GetSecretAsync(identifier, context, cancellationToken).ConfigureAwait(false);
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
        => ListSecretVersionsAsync(secretName, context: null, cancellationToken);

    /// <summary>
    /// Lists all versions of a secret with grid context for distributed tracing.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="context">The optional grid context for correlation and tracing.</param>
    /// <returns>A collection of secret versions.</returns>
    public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
        string secretName,
        IGridContext? context,
        CancellationToken _ = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(secretName));
        }

        using var scope = CreateLogScope(context);

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

    private IDisposable? CreateLogScope(IGridContext? context)
    {
        if (context == null)
        {
            return null;
        }

        var scopeProperties = new Dictionary<string, object>
        {
            ["CorrelationId"] = context.CorrelationId,
            ["NodeId"] = context.NodeId,
            ["StudioId"] = context.StudioId,
        };

        // Add CausationId if present
        if (context.CausationId != null)
        {
            scopeProperties["CausationId"] = context.CausationId;
        }

        // Add baggage items to scope
        if (context.Baggage != null)
        {
            foreach (var (key, value) in context.Baggage)
            {
                scopeProperties[$"Baggage.{key}"] = value;
            }
        }

        return _logger.BeginScope(scopeProperties);
    }
}
