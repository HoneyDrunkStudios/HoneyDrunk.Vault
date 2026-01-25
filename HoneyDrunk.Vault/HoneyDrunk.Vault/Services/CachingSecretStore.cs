using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Caching decorator for ISecretStore that wraps the composite orchestration.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CachingSecretStore"/> class.
/// </remarks>
/// <param name="inner">The inner secret store (typically the composite).</param>
/// <param name="cache">The secret cache.</param>
/// <param name="logger">The logger.</param>
public sealed class CachingSecretStore(
    ISecretStore inner,
    SecretCache cache,
    ILogger<CachingSecretStore> logger) : ISecretStore
{
    private readonly ISecretStore _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    private readonly SecretCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ILogger<CachingSecretStore> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public async Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var cacheKey = BuildCacheKey(identifier);

        // Check cache first (SecretCache handles hit/miss logging)
        if (_cache.TryGet(cacheKey, out var cached) && cached != null)
        {
            return cached;
        }

        // Fetch from providers
        var result = await _inner.GetSecretAsync(identifier, cancellationToken).ConfigureAwait(false);

        // Cache successful result
        _cache.Set(cacheKey, result);

        return result;
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var cacheKey = BuildCacheKey(identifier);

        // Check cache first (SecretCache handles hit/miss logging)
        if (_cache.TryGet(cacheKey, out var cached) && cached != null)
        {
            return VaultResult.Success(cached);
        }

        // Fetch from providers
        var result = await _inner.TryGetSecretAsync(identifier, cancellationToken).ConfigureAwait(false);

        // Only cache successful results - never cache failures
        if (result.IsSuccess && result.Value != null)
        {
            _cache.Set(cacheKey, result.Value);
        }

        return result;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        // Version listing is not cached - always go to providers
        return _inner.ListSecretVersionsAsync(secretName, cancellationToken);
    }

    /// <summary>
    /// Invalidates the cache entry for a specific secret.
    /// </summary>
    /// <param name="identifier">The secret identifier.</param>
    public void InvalidateCache(SecretIdentifier identifier)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var cacheKey = BuildCacheKey(identifier);
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated cache for secret '{SecretName}'", identifier.Name);
    }

    /// <summary>
    /// Clears all cached secrets.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
        _logger.LogInformation("Cleared all cached secrets");
    }

    private static string BuildCacheKey(SecretIdentifier identifier)
    {
        return string.IsNullOrEmpty(identifier.Version)
            ? identifier.Name
            : $"{identifier.Name}:{identifier.Version}";
    }
}
