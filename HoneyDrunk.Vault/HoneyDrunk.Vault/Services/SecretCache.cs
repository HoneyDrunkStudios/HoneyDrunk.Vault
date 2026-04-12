using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Provides caching for secret values with configurable TTL and max size.
/// Supports explicit invalidation so ADR-0006 Tier 3 Event Grid notifications can demote TTL to a fallback path.
/// </summary>
public sealed class SecretCache : ISecretCacheInvalidator, IDisposable
{
    private readonly MemoryCache _cache;
    private readonly VaultCacheOptions _options;
    private readonly ILogger<SecretCache> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private int _currentCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretCache"/> class.
    /// </summary>
    /// <param name="options">The vault options.</param>
    /// <param name="logger">The logger.</param>
    public SecretCache(
        IOptions<VaultOptions> options,
        ILogger<SecretCache> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value.Cache;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _cache = new MemoryCache(new MemoryCacheOptions
        {
            SizeLimit = _options.MaxSize,
        });
    }

    /// <summary>
    /// Gets the current number of cached items.
    /// </summary>
    public int Count => _currentCount;

    /// <summary>
    /// Gets a value indicating whether caching is enabled.
    /// </summary>
    public bool IsEnabled => _options.Enabled;

    /// <summary>
    /// Tries to get a cached secret value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The cached value if found.</param>
    /// <returns>True if the value was found in cache, false otherwise.</returns>
    public bool TryGet(string key, out SecretValue? value)
    {
        if (!_options.Enabled)
        {
            value = null;
            return false;
        }

        var cacheKey = BuildCacheKey(key);
        var found = _cache.TryGetValue(cacheKey, out value);

        if (found)
        {
            _logger.LogDebug("Cache hit for secret '{Key}'", key);
        }
        else
        {
            _logger.LogDebug("Cache miss for secret '{Key}'", key);
        }

        return found;
    }

    /// <summary>
    /// Sets a secret value in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="ttl">Optional TTL override.</param>
    public void Set(string key, SecretValue value, TimeSpan? ttl = null)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var cacheKey = BuildCacheKey(key);
        var effectiveTtl = ttl ?? _options.DefaultTtl;

        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = effectiveTtl,
            Size = 1,
        };

        if (_options.SlidingExpiration.HasValue)
        {
            entryOptions.SlidingExpiration = _options.SlidingExpiration.Value;
        }

        entryOptions.RegisterPostEvictionCallback((_, _, _, _) =>
        {
            Interlocked.Decrement(ref _currentCount);
        });

        _cache.Set(cacheKey, value, entryOptions);
        Interlocked.Increment(ref _currentCount);

        _logger.LogDebug("Cached secret '{Key}' with TTL {Ttl}", key, effectiveTtl);
    }

    /// <summary>
    /// Gets or creates a cached secret value.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory to create the value if not cached.</param>
    /// <param name="ttl">Optional TTL override.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The cached or newly created value.</returns>
    public async Task<SecretValue> GetOrCreateAsync(
        string key,
        Func<CancellationToken, Task<SecretValue>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return await factory(cancellationToken).ConfigureAwait(false);
        }

        if (TryGet(key, out var cached) && cached != null)
        {
            return cached;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check after acquiring lock
            if (TryGet(key, out cached) && cached != null)
            {
                return cached;
            }

            var value = await factory(cancellationToken).ConfigureAwait(false);
            Set(key, value, ttl);
            return value;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Invalidates the cached entry for the specified secret name.
    /// This preserves invariant 21 by ensuring the next latest-version read rehydrates from the backing provider.
    /// </summary>
    /// <param name="secretName">The secret name.</param>
    public void Invalidate(string secretName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        var cacheKey = BuildCacheKey(secretName);
        _cache.Remove(cacheKey);
        _logger.LogDebug("Invalidated cached secret '{SecretName}'", secretName);
    }

    /// <summary>
    /// Invalidates all cached secrets.
    /// </summary>
    public void InvalidateAll()
    {
        _cache.Compact(1.0);
        _currentCount = 0;
        _logger.LogInformation("Invalidated all cached secrets");
    }

    /// <summary>
    /// Removes a secret from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    public void Remove(string key)
    {
        Invalidate(key);
    }

    /// <summary>
    /// Clears all cached secrets.
    /// </summary>
    public void Clear()
    {
        InvalidateAll();
    }

    /// <summary>
    /// Disposes the cache.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cache.Dispose();
        _lock.Dispose();
        _disposed = true;
    }

    private static string BuildCacheKey(string key)
    {
        return $"vault:secret:{key}";
    }
}
