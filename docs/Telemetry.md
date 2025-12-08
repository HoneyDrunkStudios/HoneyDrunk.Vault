# 📈 Telemetry - Observability

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [VaultTelemetry.cs](#vaulttelemetrycs)
- [VaultTelemetryTags.cs](#vaulttelemetrytagscs)

---

## Overview

Telemetry integration for vault operations. Creates activities for distributed tracing and enriches logs with context information.

**Location:** `HoneyDrunk.Vault/Telemetry/`

**Pipeline Placement:** `VaultTelemetry` is invoked inside the runtime store pipeline (a decorator around provider operations). `VaultClient` does not invoke telemetry directly—telemetry wraps the composed `ISecretStore` pipeline:

```
VaultClient (façade, stateless)
   ↓
TelemetryDecoratedSecretStore  ← calls VaultTelemetry.ExecuteWithTelemetryAsync
   ↓
CachingSecretStore
   ↓
ProviderResolutionStore
   ↓
ISecretProvider
```

**Kernel Integration:** `VaultTelemetry` enriches spans with `OperationContext` metadata when available, enabling end-to-end tracing across Node pipelines.

All vault operations emit telemetry that integrates with OpenTelemetry and Kernel's Grid context. **Security Note:** Secret values are never logged or included in telemetry. Secret names are emitted by default; for heightened security, masking rules can be configured via Kernel's PII policy pipeline.

---

## VaultTelemetry.cs

Provides telemetry integration for vault operations.

```csharp
public sealed class VaultTelemetry
{
    public const string ActivitySourceName = "honeydrunk.vault";  // Lowercase per OpenTelemetry conventions

    public VaultTelemetry(
        ILogger<VaultTelemetry> logger,
        IGridContextAccessor? gridContextAccessor = null,
        INodeContextAccessor? nodeContextAccessor = null,
        IOperationContextAccessor? operationContextAccessor = null);

    /// <summary>
    /// Executes a vault operation with telemetry tracking.
    /// </summary>
    public Task<T> ExecuteWithTelemetryAsync<T>(
        string operationName,
        string providerName,
        string key,
        bool cacheHit,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
```

**Provider Name:** `providerName` reflects the provider used for the resolved call, after fallback resolution. If Azure is unavailable and File provider is used, `providerName` should be `"file"`.

**Cache Detection:** Cache decorators explicitly report hit/miss via the `cacheHit` flag passed into `ExecuteWithTelemetryAsync`. Telemetry does not infer hit/miss from latency.

### Telemetry Flow

```
VaultTelemetry.ExecuteWithTelemetryAsync()
        │
        ▼
┌───────────────────────────────────┐
│ 1. Start Activity                 │
│    - Name: canonical operation    │
│      (vault.secret.get, etc.)     │
│    - Kind: Internal               │
└───────────────┬───────────────────┘
                │
                ▼
┌───────────────────────────────────┐
│ 2. Create Log Scope               │
│    - Provider name (resolved)     │
│    - Key (not value!)             │
│    - Version (if requested)       │
│    - Grid/Node/OperationContext   │
└───────────────┬───────────────────┘
                │
                ▼
┌───────────────────────────────────┐
│ 3. Execute Operation              │
│    - Track duration               │
│    - Use explicit cache flag      │
└───────────────┬───────────────────┘
                │
        ┌───────┴───────┐
        │               │
     Success         Failure
        │               │
        ▼               ▼
┌───────────────┐ ┌───────────────────────┐
│ Set tags:     │ │ Set tags:             │
│ - status=ok   │ │ - status=error        │
│ - cache=hit   │ │ - error.type          │
│   or miss     │ │ - error.message       │
│ - resilience  │ │ - resilience tags     │
│   tags        │ │   (retry, CB state)   │
└───────────────┘ └───────────────────────┘
```

**Always Emit:** All vault operations emit telemetry, even cache hits, because cache behavior itself is observable infrastructure.
```

### Usage Example

```csharp
public sealed class VaultTelemetry(
    ILogger<VaultTelemetry> logger,
    IGridContextAccessor? gridContextAccessor = null,
    IOperationContextAccessor? operationContextAccessor = null)
{
    public const string ActivitySourceName = "HoneyDrunk.Vault";

    private static readonly ActivitySource VaultActivitySource = new(ActivitySourceName);

    public async Task<T> ExecuteWithTelemetryAsync<T>(
        string operationName,
        string providerName,
        string key,
        bool cacheHit,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var activityName = GetCanonicalActivityName(operationName);
        using var activity = StartActivity(activityName, providerName, key);
        using var logScope = CreateLogScope(providerName, key);

        var startTime = Stopwatch.GetTimestamp();
        var resultStatus = "success";
        var cacheStatus = cacheHit ? "hit" : "miss";

        try
        {
            var result = await operation(cancellationToken);

            SetActivityTags(activity, resultStatus, cacheStatus);

            _logger.LogDebug(
                "Vault operation '{Operation}' completed for key '{Key}' " +
                "from provider '{Provider}' ({Status}/{Cache})",
                operationName,
                key,
                providerName,
                resultStatus,
                cacheStatus);

            return result;
        }
        catch (Exception ex)
        {
            resultStatus = "error";
            SetActivityTags(activity, resultStatus, cacheStatus, ex);

            _logger.LogError(
                ex,
                "Vault operation '{Operation}' failed for key '{Key}' " +
                "from provider '{Provider}'",
                operationName,
                key,
                providerName);

            throw;
        }
    }

    private static string GetCanonicalActivityName(string operationName)
    {
        // Map to canonical OpenTelemetry activity names
        return operationName.ToLowerInvariant() switch
        {
            "getsecret" => "vault.secret.get",
            "trygetsecret" => "vault.secret.try_get",
            "listsecretversions" => "vault.secret.list_versions",
            "getconfig" => "vault.config.get",
            "trygetconfig" => "vault.config.try_get",
            _ => $"vault.{operationName.ToLowerInvariant()}"
        };
    }

    private Activity? StartActivity(string name, string providerName, string key)
    {
        var activity = VaultActivitySource.StartActivity(name);

        if (activity != null)
        {
            activity.SetTag("vault.provider", providerName);
            activity.SetTag("vault.key", key);

            // Add Grid context if available
            var gridContext = _gridContextAccessor?.CurrentContext;
            if (gridContext != null)
            {
                activity.SetTag("grid.correlation_id", gridContext.CorrelationId);
                activity.SetTag("grid.trace_id", gridContext.TraceId);
            }

            // Add Node context if available
            var nodeContext = _nodeContextAccessor?.CurrentContext;
            if (nodeContext != null)
            {
                activity.SetTag("grid.node_id", nodeContext.NodeId);
                activity.SetTag("grid.node_instance", nodeContext.InstanceId);
            }

            // Add Operation context if available
            var operationContext = _operationContextAccessor?.CurrentContext;
            if (operationContext != null)
            {
                activity.SetTag("grid.operation_id", operationContext.OperationId);
            }
        }

        return activity;
    }
}
```

### Activity Attributes

**Core Attributes:**

| Attribute | Description | Example |
|-----------|-------------|---------||
| `vault.provider` | Resolved provider name (after fallback) | `azure-key-vault`, `file` |
| `vault.key` | Secret/config key | `database-connection-string` |
| `vault.status` | Operation result | `success`, `error` |
| `vault.cache` | Cache status (explicit flag) | `hit`, `miss` |
| `vault.version` | Secret version (only if explicitly requested) | `v2.0.0` |
| `grid.correlation_id` | Correlation ID | `abc-123-def` |
| `grid.trace_id` | Trace ID | `trace-xyz` |
| `grid.node_id` | Node ID | `order-service` |
| `grid.node_instance` | Node instance ID | `order-service-7d8f9` |
| `grid.operation_id` | Operation ID | `op-12345` |

**Resilience Attributes** (added by resilience layer):

| Attribute | Description | Example |
|-----------|-------------|---------||
| `vault.retry.count` | Number of retries used | `2` |
| `vault.retry.max` | Max retries allowed | `3` |
| `vault.circuit.state` | Circuit breaker state | `open`, `half-open`, `closed` |
| `vault.circuit.opened_on` | Circuit breaker opened timestamp | `2025-12-08T10:30:00Z` |

**Canonical Activity Names:**

| Logical Operation | Activity Name |
|-------------------|---------------|
| Get Secret | `vault.secret.get` |
| Try Get Secret | `vault.secret.try_get` |
| List Versions | `vault.secret.list_versions` |
| Get Config | `vault.config.get` |
| Try Get Config | `vault.config.try_get` |

[↑ Back to top](#table-of-contents)

---

## VaultTelemetryTags.cs

Standard tag names for vault telemetry.

```csharp
public static class VaultTelemetryTags
{
    /// <summary>
    /// The provider name (e.g., "azure-key-vault", "file").
    /// </summary>
    public const string Provider = "vault.provider";

    /// <summary>
    /// The secret or configuration key (not the value!).
    /// </summary>
    public const string Key = "vault.key";

    /// <summary>
    /// The operation name (e.g., "get_secret", "get_config").
    /// </summary>
    public const string Operation = "vault.operation";

    /// <summary>
    /// The operation status (e.g., "success", "error").
    /// </summary>
    public const string Status = "vault.status";

    /// <summary>
    /// The cache status (e.g., "hit", "miss").
    /// </summary>
    public const string Cache = "vault.cache";

    /// <summary>
    /// The secret version if specified.
    /// </summary>
    public const string Version = "vault.version";
}
```

### Usage Example

```csharp
// In activity creation
activity.SetTag(VaultTelemetryTags.Provider, "azure-key-vault");
activity.SetTag(VaultTelemetryTags.Key, identifier.Name);
activity.SetTag(VaultTelemetryTags.Operation, "get_secret");
activity.SetTag(VaultTelemetryTags.Status, "success");
activity.SetTag(VaultTelemetryTags.Cache, "hit");

if (identifier.Version != null)
{
    activity.SetTag(VaultTelemetryTags.Version, identifier.Version);
}
```

### OpenTelemetry Integration

```csharp
// Program.cs - Configure OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource(VaultTelemetry.ActivitySourceName);
        tracing.AddAspNetCoreInstrumentation();
        tracing.AddOtlpExporter();
        
        // Sampling: Vault events should use parent-based sampling
        // or always-on sampling for infrastructure observability
        tracing.SetSampler(new ParentBasedSampler(new AlwaysOnSampler()));
    });
```

**Sampling Guidance:** Vault events should be always-sampled or use parent-based sampling to ensure infrastructure operations are observable. Vault is foundational infrastructure; its telemetry is critical for diagnosing system-wide issues.

### Security Considerations

**NEVER include in telemetry:**
- Secret values
- Connection strings
- API keys
- Passwords
- Tokens

**Safe to include (with policy consideration):**
- Secret names (keys) - **Default: emitted; for heightened security, configure masking via Kernel's PII policy**
- Provider names
- Operation types
- Timestamps
- Duration
- Cache hit/miss
- Error types (not messages with secrets)

**Version Metadata:** Version is included only when explicitly provided in the request. Resolved versions from providers are not emitted to avoid leaking sensitive metadata.

[↑ Back to top](#table-of-contents)

---

## Summary

Telemetry provides observability without compromising security:

| Component | Purpose | Security |
|-----------|---------|----------|
| `VaultTelemetry` | Activity tracing | ✅ No secret values |
| `VaultTelemetryTags` | Standardized tag names | ✅ Key names only |

All telemetry integrates with:
- OpenTelemetry tracing
- Kernel Grid context
- Structured logging
- Metrics collection

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
