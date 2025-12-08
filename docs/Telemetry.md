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

All vault operations emit telemetry that integrates with OpenTelemetry and Kernel's Grid context. **Security Note:** Secret values are never logged or included in telemetry.

---

## VaultTelemetry.cs

Provides telemetry integration for vault operations.

```csharp
public sealed class VaultTelemetry
{
    public const string ActivitySourceName = "HoneyDrunk.Vault";

    public VaultTelemetry(
        ILogger<VaultTelemetry> logger,
        IGridContextAccessor? gridContextAccessor = null,
        IOperationContextAccessor? operationContextAccessor = null);

    /// <summary>
    /// Executes a vault operation with telemetry tracking.
    /// </summary>
    public Task<T> ExecuteWithTelemetryAsync<T>(
        string operationName,
        string providerName,
        string key,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
```

### Telemetry Flow

```
VaultTelemetry.ExecuteWithTelemetryAsync()
        │
        ▼
┌───────────────────────────────────┐
│ 1. Start Activity                 │
│    - Name: "vault.{operationName}"│
│    - Kind: Internal               │
└───────────────┬───────────────────┘
                │
                ▼
┌───────────────────────────────────┐
│ 2. Create Log Scope               │
│    - Provider name                │
│    - Key (not value!)             │
│    - Grid context                 │
└───────────────┬───────────────────┘
                │
                ▼
┌───────────────────────────────────┐
│ 3. Execute Operation              │
│    - Track duration               │
│    - Detect cache hit/miss        │
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
│ or miss       │ │ - error.message       │
└───────────────┘ └───────────────────────┘
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
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var activityName = $"vault.{operationName}";
        using var activity = StartActivity(activityName, providerName, key);
        using var logScope = CreateLogScope(providerName, key);

        var startTime = Stopwatch.GetTimestamp();
        var resultStatus = "success";
        var cacheStatus = "miss";

        try
        {
            var result = await operation(cancellationToken);

            // Detect cache hit by timing (< 1ms typically indicates cache hit)
            var elapsed = Stopwatch.GetElapsedTime(startTime);
            if (elapsed.TotalMilliseconds < 1)
            {
                cacheStatus = "hit";
            }

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
                activity.SetTag("grid.node_id", gridContext.NodeId);
            }
        }

        return activity;
    }
}
```

### Activity Attributes

| Attribute | Description | Example |
|-----------|-------------|---------|
| `vault.provider` | Provider name | `azure-key-vault` |
| `vault.key` | Secret/config key | `database-connection-string` |
| `vault.status` | Operation result | `success`, `error` |
| `vault.cache` | Cache status | `hit`, `miss` |
| `grid.correlation_id` | Correlation ID | `abc-123-def` |
| `grid.node_id` | Node ID | `order-service` |

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
    });
```

### Security Considerations

**NEVER include in telemetry:**
- Secret values
- Connection strings
- API keys
- Passwords
- Tokens

**Safe to include:**
- Secret names (keys)
- Provider names
- Operation types
- Timestamps
- Duration
- Cache hit/miss
- Error types (not messages with secrets)

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
