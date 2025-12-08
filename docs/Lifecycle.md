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

**Kernel Integration:** `VaultStartupHook` runs in a fully initialized Kernel environment. Telemetry and `GridContext` are active, ensuring all warmup operations produce structured traces.

**Warmup Semantics:**
- **Opportunistic**: Warmup does not block application startup or fail the node
- **Non-blocking**: Failures are logged but do not prevent startup completion
- **Readiness-aware**: `VaultStartupHook` accelerates readiness but does not determine readiness. `VaultReadinessContributor` is the authoritative signal to Kubernetes.

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
│ (After core Kernel services)      │
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
        ┌───────┴───────┐
        │               │
  Providers > 0   Providers = 0
        │               │
        ▼               ▼
┌───────────────────┐ ┌──────────────────────┐
│ 2. WarmCache()    │ │ Skip warmup          │
│ For each warmup   │ │ VaultReadiness       │
│ key:              │ │ will block traffic   │
│ - Try fetch       │ └──────────────────────┘
│ - Cache result    │
│ - Log outcome     │
└───────┬───────────┘
        │
        ▼
┌───────────────────────────────────┐
│ Startup Complete                  │
│ (Readiness still evaluating)      │
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
            _logger.LogWarning(
                "No vault providers are configured and enabled. " +
                "This is acceptable in development (File provider only), " +
                "but VaultReadinessContributor will prevent the node from becoming Ready in deployed environments.");
            return;
        }

        _logger.LogInformation(
            "Vault configured with {ProviderCount} enabled provider(s)",
            enabledProviders.Count);

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
        // Skip warmup if no providers are enabled
        var enabledProviders = _options.Providers.Values.Count(p => p.IsEnabled);
        if (enabledProviders == 0)
        {
            _logger.LogDebug("Skipping cache warmup: no providers enabled");
            return;
        }

        _logger.LogInformation(
            "Warming vault cache with {Count} secrets",
            _options.WarmupKeys.Count);

        var tasks = _options.WarmupKeys.Select(async key =>
        {
            try
            {
                // Warmup uses the full VaultClient pipeline (retries, timeouts, circuit breaker)
                // Warmup operations participate in Kernel's tracing pipeline with proper spans
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
                        "Failed to warm cache for secret '{SecretName}': {Error}. " +
                        "Provider resolution will be re-evaluated on subsequent requests.",
                        key,
                        result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception warming cache for secret '{SecretName}'. " +
                    "Warmup failures do not permanently degrade the provider.",
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

**Provider Selection:** Warmup does not override provider selection or force early provider binding. Provider resolution still follows configured priority and availability checks at runtime. Cache warmup is opportunistic—failure to warm a key does not lock Vault into fallback mode.

**Resilience Integration:** `WarmCacheAsync` uses the full `VaultClient` pipeline including retries, timeouts, and circuit breaker rules. This prevents transient cloud faults from blocking startup.

**Telemetry Context:** Warmup operations participate in Kernel's tracing pipeline. Each warmup call creates proper spans so early provider latency and failures appear in telemetry dashboards.

**Secret Rotation:** Warmup does not pin secret versions. It always attempts to warm the latest available version. Applications requesting fixed versions will bypass warmup as expected.

**Health vs. Startup:** Provider health checks are the responsibility of `VaultHealthContributor`. Startup does not explicitly test provider connectivity unless a warmup key is configured.

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

| Phase | Action | On Failure | Impact on Readiness |
|-------|--------|------------|---------------------|
| Configuration Validation | Check enabled providers | Log warning | Zero providers → node not Ready (via `VaultReadinessContributor`) |
| Cache Warming | Fetch warmup keys (if providers exist) | Log error, continue | Does not block startup; `ReadinessContributor` authoritative |

**Key Guarantees:**
- Startup hook is **opportunistic** and **non-blocking**
- Warmup failures do not permanently degrade providers
- Provider resolution is re-evaluated on each request based on availability
- Warmup uses full resilience pipeline (retries, timeouts, circuit breaker)
- All warmup operations produce structured traces via Kernel telemetry

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
