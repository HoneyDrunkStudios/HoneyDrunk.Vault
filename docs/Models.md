# 🔧 Models - Building Blocks

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [SecretIdentifier.cs](#secretidentifiercs)
- [SecretValue.cs](#secretvaluecs)
- [SecretVersion.cs](#secretversioncs)
- [VaultResult.cs](#vaultresultcs)
- [VaultResult{T}.cs](#vaultresulttcs)
- [VaultScope.cs](#vaultscopecs)

---

## Overview

Domain models that represent secrets, configuration, and operation results. These are immutable records for thread-safety and testability.

**Location:** `HoneyDrunk.Vault/Models/`

**Pure Models:** None of these models perform validation, provider resolution, access control, or caching. Those responsibilities belong to `VaultClient` and provider implementations.

**Relationship Diagram:**
```
SecretIdentifier  →  SecretValue
        │                  │
        ├───────────────┬──┘
                        ▼
                   ISecretProvider
                        ▼
                   VaultResult<T>
```

---

## SecretIdentifier.cs

Unique identifier for a secret in the vault.

```csharp
public sealed record SecretIdentifier
{
    /// <summary>
    /// Initializes with secret name only (latest version).
    /// </summary>
    public SecretIdentifier(string name);

    /// <summary>
    /// Initializes with secret name and optional version.
    /// </summary>
    public SecretIdentifier(string name, string? version);

    /// <summary>
    /// Gets the name of the secret.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the secret, if specified.
    /// </summary>
    public string? Version { get; }
}
```

**Provider Semantics:** Secret identifiers are logical names. `SecretIdentifier` does not encode provider routing—provider selection happens at runtime in `VaultClient` based on `VaultOptions`. Providers decide how to interpret identifiers (prefix, hierarchy, naming conventions).

### Usage Example

```csharp
// Latest version
var latestId = new SecretIdentifier("database-connection-string");

// Specific version
var versionedId = new SecretIdentifier("api-key", "v2.0.0");

// Pattern matching
if (identifier.Version is null)
{
    Console.WriteLine($"Getting latest version of {identifier.Name}");
}
else
{
    Console.WriteLine($"Getting version {identifier.Version} of {identifier.Name}");
}
```

[↑ Back to top](#table-of-contents)

---

## SecretValue.cs

The value of a secret retrieved from the vault.

```csharp
public sealed record SecretValue
{
    /// <summary>
    /// Initializes a new instance of the SecretValue class.
    /// </summary>
    public SecretValue(SecretIdentifier identifier, string value, string? version);

    /// <summary>
    /// Gets the identifier of the secret.
    /// </summary>
    public SecretIdentifier Identifier { get; }

    /// <summary>
    /// Gets the secret value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the version of the secret.
    /// </summary>
    public string? Version { get; }
}
```

**Version Resolution:** The `Version` on `SecretValue` always reflects the actual resolved version from the backend, which may differ from the version requested in `SecretIdentifier`. When requesting latest (null version), providers populate `SecretValue.Version` with the backend's version identifier.

**Security:** `SecretValue.Value` must never be written to logs, traces, metrics, or exception messages. This is enforced by convention, not by type system.

### Usage Example

```csharp
// Retrieve and use a secret
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("database-connection-string"),
    ct);

// Access properties
Console.WriteLine($"Secret: {secret.Identifier.Name}");
Console.WriteLine($"Version: {secret.Version ?? "latest"}");

// Use the value (carefully - don't log it!)
var connectionString = secret.Value;
using var connection = new SqlConnection(connectionString);
```

[↑ Back to top](#table-of-contents)

---

## SecretVersion.cs

Version information for a secret.

```csharp
public sealed record SecretVersion
{
    /// <summary>
    /// Initializes a new instance of the SecretVersion class.
    /// </summary>
    public SecretVersion(string version, DateTimeOffset createdOn);

    /// <summary>
    /// Gets the version identifier.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the date and time when the version was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; }
}
```

**Version Ordering:** Vault does not enforce any version ordering semantics. Providers decide ordering rules:
- **Azure Key Vault**: GUID-based versions (no inherent order)
- **AWS Secrets Manager**: Staging labels (`AWSCURRENT`, `AWSPREVIOUS`)
- **File Provider**: Single implicit version (no version history)

Consumers should sort by `CreatedOn` when chronological ordering is needed.

### Usage Example

```csharp
// List all versions of a secret
var versions = await secretStore.ListSecretVersionsAsync("api-key", ct);

foreach (var version in versions.OrderByDescending(v => v.CreatedOn))
{
    Console.WriteLine($"Version: {version.Version}, Created: {version.CreatedOn}");
}

// Get the most recent version
var latest = versions.MaxBy(v => v.CreatedOn);
if (latest != null)
{
    var secret = await secretStore.GetSecretAsync(
        new SecretIdentifier("api-key", latest.Version),
        ct);
}
```

[↑ Back to top](#table-of-contents)

---

## VaultResult.cs

Factory methods for creating vault results.

```csharp
public static class VaultResult
{
    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    public static VaultResult<T> Success<T>(T value);

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static VaultResult<T> Failure<T>(string errorMessage);
}
```

### Usage Example

```csharp
// In provider implementations
public async Task<VaultResult<SecretValue>> TryFetchSecretAsync(
    string key, 
    CancellationToken ct)
{
    try
    {
        var value = await FetchFromBackend(key, ct);
        return VaultResult.Success(value);
    }
    catch (SecretNotFoundException)
    {
        return VaultResult.Failure<SecretValue>($"Secret '{key}' not found");
    }
    catch (Exception ex)
    {
        return VaultResult.Failure<SecretValue>($"Failed to fetch: {ex.Message}");
    }
}
```

[↑ Back to top](#table-of-contents)

---

## VaultResult{T}.cs

Generic result type for operations that may succeed or fail.

```csharp
public sealed class VaultResult<T>
{
    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the value if successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message if failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static VaultResult<T> Success(T value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static VaultResult<T> Failure(string errorMessage);
}
```

**Non-throwing Pattern:** `VaultResult<T>` is the non-throwing path. `Try*` methods always return `VaultResult`, never throw exceptions (except for catastrophic failures like `ArgumentNullException`).

**Internal Wrapper:** `VaultResult<T>` is an internal operational wrapper and is not intended to be serialized or surfaced outside the node boundary. Do not expose `VaultResult` in public APIs or log its contents (which may contain secret names).

### Usage Example

```csharp
// Using TryGetSecretAsync with result pattern
var result = await secretStore.TryGetSecretAsync(
    new SecretIdentifier("optional-api-key"),
    ct);

if (result.IsSuccess)
{
    // Use the secret
    var apiKey = result.Value!.Value;
    await CallExternalApi(apiKey);
}
else
{
    // Log the failure (not the secret value!)
    _logger.LogWarning("Could not retrieve API key: {Error}", result.ErrorMessage);
    
    // Use fallback behavior
    await UseDefaultBehavior();
}

// Pattern matching style
var connectionString = result switch
{
    { IsSuccess: true, Value: var secret } => secret.Value,
    { ErrorMessage: var error } => throw new InvalidOperationException(error)
};
```

[↑ Back to top](#table-of-contents)

---

## VaultScope.cs

Represents the scope for vault operations (environment, tenant, node).

```csharp
public sealed record VaultScope
{
    /// <summary>
    /// Initializes with environment only.
    /// </summary>
    public VaultScope(string environment);

    /// <summary>
    /// Initializes with environment, tenant, and node.
    /// </summary>
    public VaultScope(string environment, string? tenant, string? node);

    /// <summary>
    /// Gets the environment name (e.g., Development, Production).
    /// </summary>
    public string Environment { get; }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string? Tenant { get; }

    /// <summary>
    /// Gets the node identifier.
    /// </summary>
    public string? Node { get; }
}
```

**Kernel Integration:** `VaultScope` is typically populated from Kernel's `GridContext` and `NodeContext`, not constructed manually by application code. Scopes are derived from Kernel context to ensure consistency with telemetry, identity, and operation context.

**Provider Scoping:** `VaultScope` does not directly participate in provider routing unless the provider opts into scoping (e.g., Azure's hierarchical naming, AWS prefixes). Scope is a representation, not a routing mechanism.

### Usage Example

```csharp
// Environment-only scope
var devScope = new VaultScope("Development");

// Full scope for multi-tenant applications
var tenantScope = new VaultScope(
    environment: "Production",
    tenant: "customer-123",
    node: "order-service");

// Use scope to build secret paths
public string BuildSecretPath(VaultScope scope, string secretName)
{
    var parts = new List<string> { scope.Environment };
    
    if (scope.Tenant != null)
        parts.Add(scope.Tenant);
    
    if (scope.Node != null)
        parts.Add(scope.Node);
    
    parts.Add(secretName);
    
    return string.Join("/", parts);
    // e.g., "Production/customer-123/order-service/db-connection"
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

The models provide type-safe representations for vault operations:

| Model | Purpose | Immutable |
|-------|---------|-----------|
| `SecretIdentifier` | Locate a secret by name/version | ✅ Record |
| `SecretValue` | Retrieved secret with metadata | ✅ Record |
| `SecretVersion` | Version history entry | ✅ Record |
| `VaultResult` | Factory for result creation | ✅ Static |
| `VaultResult<T>` | Success/failure result | ✅ Sealed |
| `VaultScope` | Environment/tenant context | ✅ Record |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
