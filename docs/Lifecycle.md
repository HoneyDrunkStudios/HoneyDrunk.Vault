# 🚀 Lifecycle - Startup Integration

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [VaultStartupHook.cs](#vaultstartuphookcs)

---

## Overview

Startup integration for the vault system. Validates configuration and warms caches during application startup.

**Location:** `HoneyDrunk.Vault/Lifecycle/`

The startup hook integrates with Kernel's lifecycle system to ensure the vault is properly configured before the application accepts traffic.

---

## VaultStartupHook.cs

Startup hook that validates provider configuration and optionally warms caches.

```csharp
public sealed class VaultStartupHook : IStartupHook
{
    public VaultStartupHook(
        ISecretStore secretStore,
        IOptions<VaultOptions> options,
        ILogger<VaultStartupHook> logger);

    /// <summary>
    /// Gets the priority of this hook (lower runs first).
    /// </summary>
    public int Priority => 100;  // Run after core services

    /// <summary>
    /// Executes the startup hook.
    /// </summary>
    public Task ExecuteAsync(CancellationToken cancellationToken = default);
}
```

### Startup Flow

```
Application Startup
        │
        ▼
┌───────────────────────────────────┐
│ IStartupHook: VaultStartupHook    │
│ Priority: 100                     │
└───────────────┬───────────────────┘
                │
                ▼
┌───────────────────────────────────┐
│ 1. ValidateConfiguration()        │
│    - Check enabled providers      │
│    - Log provider details         │
│    - Warn if none configured      │
└───────────────┬───────────────────┘
                │
                ▼
┌───────────────────────────────────┐
│ 2. WarmCacheAsync()               │
│    - For each key in WarmupKeys   │
│    - Fetch secret from provider   │
│    - Cache the result             │
│    - Log success/failure          │
└───────────────┬───────────────────┘
                │
                ▼
┌───────────────────────────────────┐
│ Startup Complete                  │
└───────────────────────────────────┘
```

### Usage Example

```csharp
public sealed class VaultStartupHook(
    ISecretStore secretStore,
    IOptions<VaultOptions> options,
    ILogger<VaultStartupHook> logger) : IStartupHook
{
    public int Priority => 100;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Vault startup hook executing");

        // Validate provider configuration
        ValidateConfiguration();

        // Warm cache if configured
        if (_options.WarmupKeys.Count > 0)
        {
            await WarmCacheAsync(cancellationToken);
        }

        _logger.LogInformation("Vault startup hook completed");
    }

    private void ValidateConfiguration()
    {
        var enabledProviders = _options.Providers.Values
            .Where(p => p.IsEnabled)
            .ToList();

        if (enabledProviders.Count == 0)
        {
            _logger.LogWarning("No vault providers are configured and enabled");
            return;
        }

        foreach (var provider in enabledProviders)
        {
            _logger.LogDebug(
                "Vault provider '{ProviderName}' is enabled with type '{ProviderType}'",
                provider.Name,
                provider.ProviderType);
        }
    }

    private async Task WarmCacheAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Warming vault cache with {Count} secrets",
            _options.WarmupKeys.Count);

        var tasks = _options.WarmupKeys.Select(async key =>
        {
            try
            {
                var result = await _secretStore.TryGetSecretAsync(
                    new SecretIdentifier(key),
                    cancellationToken);

                if (result.IsSuccess)
                {
                    _logger.LogDebug("Warmed cache for secret '{SecretName}'", key);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to warm cache for secret '{SecretName}': {Error}",
                        key,
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception warming cache for secret '{SecretName}'",
                    key);
            }
        });

        await Task.WhenAll(tasks);
    }
}
```

### Configuration

```csharp
builder.Services.AddVault(options =>
{
    // Configure providers
    options.AddAzureKeyVaultProvider(akv => { ... });

    // Specify secrets to warm up at startup
    options.WarmupKeys.Add("database-connection-string");
    options.WarmupKeys.Add("redis-connection-string");
    options.WarmupKeys.Add("api-key");
});
```

### Benefits of Cache Warming

1. **Reduced Cold Start Latency** - Critical secrets are already cached when first request arrives
2. **Early Failure Detection** - Configuration issues discovered at startup, not at runtime
3. **Predictable Performance** - No cache miss penalty on first access
4. **Validation** - Ensures required secrets exist before accepting traffic

### Priority Ordering

The startup hook uses `Priority = 100` to run after core services but before the application starts accepting traffic:

| Priority | Typical Use |
|----------|-------------|
| 0-50 | Core infrastructure (logging, metrics) |
| 50-100 | Data stores, caches |
| **100** | **VaultStartupHook** |
| 100-200 | Application-specific initialization |
| 200+ | Non-critical background tasks |

[↑ Back to top](#table-of-contents)

---

## Summary

The startup hook ensures vault readiness before the application accepts traffic:

| Phase | Action | On Failure |
|-------|--------|------------|
| Configuration Validation | Check enabled providers | Log warning |
| Cache Warming | Fetch warmup keys | Log error, continue |

The hook is non-blocking for missing secrets but provides valuable logging for debugging configuration issues.

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
