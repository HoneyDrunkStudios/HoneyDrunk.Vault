# 🔄 Services - Core Services

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [VaultClient.cs](#vaultclientcs)
- [SecretCache.cs](#secretcachecs)
- [ConfigSourceAdapter.cs](#configsourceadaptercs)

---

## Overview

Core service implementations that orchestrate vault operations. These services coordinate caching, provider resolution, and error handling.

**Location:** `HoneyDrunk.Vault/Services/`

---

## VaultClient.cs

Central orchestrator for vault operations. Implements the `IVaultClient` interface and coordinates between secret stores and configuration sources.

```csharp
public sealed class VaultClient : IVaultClient
{
    public VaultClient(
        ISecretStore secretStore,
        IConfigSource configSource,
        ILogger<VaultClient> logger);

    // Secret operations
    public Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier, 
        CancellationToken cancellationToken = default);
    
    public Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier, 
        CancellationToken cancellationToken = default);
    
    public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
        string secretName, 
        CancellationToken cancellationToken = default);

    // Configuration operations
    public Task<string> GetConfigValueAsync(
        string key, 
        CancellationToken cancellationToken = default);
    
    public Task<string?> TryGetConfigValueAsync(
        string key, 
        CancellationToken cancellationToken = default);
    
    public Task<T> GetConfigValueAsync<T>(
        string key, 
        CancellationToken cancellationToken = default);
    
    public Task<T> TryGetConfigValueAsync<T>(
        string key, 
        T defaultValue, 
        CancellationToken cancellationToken = default);
}
```

### Orchestration Flow

```
VaultClient.GetSecretAsync()
        │
        ▼
┌───────────────────────────────────┐
│ Log operation start               │
│ (secret name, version)            │
└───────────────┬───────────────────┘
                │
                ▼
┌───────────────────────────────────┐
│ Delegate to ISecretStore          │
│ GetSecretAsync()                  │
└───────────────┬───────────────────┘
                │
        ┌───────┴───────┐
        │               │
     Success         Exception
        │               │
        ▼               ▼
┌───────────────┐ ┌───────────────────────┐
│ Log success   │ │ Log error             │
│ Return value  │ │ Wrap in VaultOperation│
└───────────────┘ │ Exception             │
                  └───────────────────────┘
```

### Usage Example

```csharp
public class PaymentService(IVaultClient vaultClient)
{
    public async Task ProcessPaymentAsync(Payment payment, CancellationToken ct)
    {
        // Get API credentials
        var apiKey = await vaultClient.GetSecretAsync(
            new SecretIdentifier("payment-gateway-api-key"),
            ct);

        // Get configuration
        var timeout = await vaultClient.TryGetConfigValueAsync<int>(
            "payment:timeout-seconds",
            defaultValue: 30,
            ct);

        // Process payment
        await ProcessWithCredentials(apiKey.Value, timeout, ct);
    }
}

// Registration
services.AddSingleton<IVaultClient, VaultClient>();
```

[↑ Back to top](#table-of-contents)

---

## SecretCache.cs

In-memory caching layer for secrets. Reduces calls to remote secret stores with configurable TTL and size limits.

```csharp
public sealed class SecretCache : IDisposable
{
    public SecretCache(
        IOptions<VaultOptions> options,
        ILogger<SecretCache> logger);

    /// <summary>
    /// Gets the current number of cached items.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets whether caching is enabled.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Tries to get a cached secret value.
    /// </summary>
    public bool TryGet(string key, out SecretValue? value);

    /// <summary>
    /// Adds or updates a cached secret value.
    /// </summary>
    public void Set(string key, SecretValue value);

    /// <summary>
    /// Removes a cached secret value.
    /// </summary>
    public void Remove(string key);

    /// <summary>
    /// Clears all cached values.
    /// </summary>
    public void Clear();
}
```

### Cache Flow

```
SecretCache.TryGet("api-key")
        │
        ▼
┌───────────────────────────────────┐
│ Is caching enabled?               │
└───────────────┬───────────────────┘
                │
        ┌───────┴───────┐
        │               │
       Yes              No
        │               │
        ▼               ▼
┌───────────────┐ ┌───────────────────┐
│ Check cache   │ │ Return false      │
│ for key       │ │ (force fetch)     │
└───────┬───────┘ └───────────────────┘
        │
   ┌────┴────┐
   │         │
  Hit       Miss
   │         │
   ▼         ▼
Log hit   Log miss
Return    Return
value     false
```

### Usage Example

```csharp
public sealed class CachingSecretStore(
    ISecretProvider provider,
    SecretCache cache,
    ILogger<CachingSecretStore> logger) : ISecretStore
{
    public async Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken ct)
    {
        var cacheKey = BuildCacheKey(identifier);

        // Try cache first
        if (cache.TryGet(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache hit for {SecretName}", identifier.Name);
            return cached!;
        }

        // Fetch from provider
        _logger.LogDebug("Cache miss for {SecretName}", identifier.Name);
        var secret = await provider.FetchSecretAsync(
            identifier.Name,
            identifier.Version,
            ct);

        // Cache the result
        cache.Set(cacheKey, secret);

        return secret;
    }

    private static string BuildCacheKey(SecretIdentifier identifier)
    {
        return identifier.Version is null
            ? identifier.Name
            : $"{identifier.Name}:{identifier.Version}";
    }
}
```

### Cache Configuration

```csharp
builder.Services.AddVault(options =>
{
    options.Cache.Enabled = true;
    options.Cache.DefaultTtl = TimeSpan.FromMinutes(15);
    options.Cache.MaxSize = 1000;
    options.Cache.SlidingExpiration = TimeSpan.FromMinutes(5);
});
```

### Cache Metrics

The cache tracks:
- **Count**: Current number of cached entries
- **Hit/Miss**: Logged for debugging and metrics
- **Evictions**: Automatic when MaxSize exceeded (LRU)

[↑ Back to top](#table-of-contents)

---

## ConfigSourceAdapter.cs

Adapter that wraps an `IConfigSource` to implement `IConfigProvider`. Enables legacy config sources to work with the new typed interface.

```csharp
internal sealed class ConfigSourceAdapter : IConfigProvider
{
    public ConfigSourceAdapter(IConfigSource configSource);

    public Task<string> GetValueAsync(
        string key, 
        CancellationToken cancellationToken = default);

    public Task<T> GetValueAsync<T>(
        string path, 
        T defaultValue, 
        CancellationToken cancellationToken = default);

    public Task<string?> TryGetValueAsync(
        string key, 
        CancellationToken cancellationToken = default);

    public Task<T> GetValueAsync<T>(
        string key, 
        CancellationToken cancellationToken = default);
}
```

### Adapter Flow

```
IConfigProvider.GetValueAsync<T>()
        │
        ▼
┌───────────────────────────────────┐
│ ConfigSourceAdapter               │
│ (wraps IConfigSource)             │
└───────────────┬───────────────────┘
                │
                ▼
┌───────────────────────────────────┐
│ IConfigSource.GetConfigValueAsync │
│ (returns string)                  │
└───────────────┬───────────────────┘
                │
                ▼
┌───────────────────────────────────┐
│ Convert string to T               │
│ (TypeDescriptor.GetConverter)     │
└───────────────────────────────────┘
```

### Usage Example

```csharp
// Automatic registration in VaultServiceCollectionExtensions
services.TryAddSingleton<IConfigProvider>(sp =>
{
    var configSource = sp.GetService<IConfigSource>();
    
    // If IConfigSource already implements IConfigProvider, use directly
    if (configSource is IConfigProvider provider)
    {
        return provider;
    }

    // Otherwise, wrap in adapter
    return configSource != null
        ? new ConfigSourceAdapter(configSource)
        : throw new InvalidOperationException("No IConfigSource registered");
});
```

[↑ Back to top](#table-of-contents)

---

## Summary

Core services provide the orchestration and caching layers:

| Service | Purpose | Thread-Safe |
|---------|---------|-------------|
| `VaultClient` | Unified orchestrator | ✅ |
| `SecretCache` | In-memory caching | ✅ (SemaphoreSlim) |
| `ConfigSourceAdapter` | Interface bridging | ✅ |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
