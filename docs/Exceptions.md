# ❌ Exceptions - Error Handling

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [SecretNotFoundException.cs](#secretnotfoundexceptioncs)
- [ConfigurationNotFoundException.cs](#configurationnotfoundexceptioncs)
- [VaultOperationException.cs](#vaultoperationexceptioncs)

---

## Overview

Custom exception types for vault operations. These exceptions provide structured error information for handling vault-specific failures.

**Location:** `HoneyDrunk.Vault/Exceptions/`

**Design Philosophy:** Vault does not define a common `VaultException` base type. This is intentional to avoid overly broad catch patterns and encourage precise error handling.

**Security:** Vault never includes secret values in exceptions. Only metadata (key names, provider names, and generic error messages) are surfaced to prevent accidental leakage.

---

## SecretNotFoundException.cs

Exception thrown when a secret is not found in the vault.

```csharp
public sealed class SecretNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance with the secret name.
    /// </summary>
    public SecretNotFoundException(string secretName);

    /// <summary>
    /// Initializes a new instance with the secret name and inner exception.
    /// </summary>
    public SecretNotFoundException(string secretName, Exception innerException);

    /// <summary>
    /// Gets the name of the secret that was not found.
    /// </summary>
    public string SecretName { get; }
}
```

**Provider Selection:** `SecretNotFoundException` indicates that the selected provider did not contain the requested secret. It does not imply that no other provider contains the secret. Vault queries the active provider based on provider priority and availability, not all providers simultaneously.

### Usage Example

**Note:** The examples below simplify provider resolution for clarity. In reality, `VaultClient` performs provider selection (priority order, `IsAvailable` checks, fallback chain) before invoking the backend provider.

```csharp
// Throwing the exception
public async Task<SecretValue> GetSecretAsync(
    SecretIdentifier identifier,
    CancellationToken ct)
{
    var value = await FetchFromBackend(identifier.Name, ct);
    
    if (value is null)
    {
        throw new SecretNotFoundException(identifier.Name);
    }
    
    return new SecretValue(identifier, value, null);
}

// Catching the exception
try
{
    var secret = await secretStore.GetSecretAsync(
        new SecretIdentifier("missing-secret"),
        ct);
}
catch (SecretNotFoundException ex)
{
    _logger.LogWarning(
        "Secret '{SecretName}' was not found",
        ex.SecretName);
    
    // Use fallback or re-throw
    throw;
}
```

### When to Use

| Scenario | Exception |
|----------|-----------||
| Secret does not exist | `SecretNotFoundException` |
| Secret was deleted | `SecretNotFoundException` |
| Wrong secret name | `SecretNotFoundException` (providers normalize "invalid name" errors) |
| Version does not exist | `SecretNotFoundException` |

[↑ Back to top](#table-of-contents)

---

## ConfigurationNotFoundException.cs

Exception thrown when a configuration value is not found.

```csharp
public sealed class ConfigurationNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance with the configuration key.
    /// </summary>
    public ConfigurationNotFoundException(string key);

    /// <summary>
    /// Initializes a new instance with the key and inner exception.
    /// </summary>
    public ConfigurationNotFoundException(string key, Exception innerException);

    /// <summary>
    /// Gets the configuration key that was not found.
    /// </summary>
    public string Key { get; }
}
```

**Contract Scope:** Thrown only when using the `Get*` methods on `IConfigProvider` (exported contract). `Try*` methods never throw. `IConfigSource` is internal and does not guarantee this exception type.

### Usage Example

```csharp
// Throwing the exception
public async Task<string> GetConfigValueAsync(
    string key,
    CancellationToken ct)
{
    var value = await FetchConfigFromBackend(key, ct);
    
    if (value is null)
    {
        throw new ConfigurationNotFoundException(key);
    }
    
    return value;
}

// Catching the exception
try
{
    var value = await configProvider.GetValueAsync("missing-key", ct);
}
catch (ConfigurationNotFoundException ex)
{
    _logger.LogWarning(
        "Configuration key '{Key}' was not found",
        ex.Key);
    
    // Use default value
    return "default-value";
}
```

### When to Use

| Scenario | Exception |
|----------|-----------|
| Key does not exist | `ConfigurationNotFoundException` |
| Key was removed | `ConfigurationNotFoundException` |
| Wrong key path | `ConfigurationNotFoundException` |

[↑ Back to top](#table-of-contents)

---

## VaultOperationException.cs

Exception thrown when an operation on the vault fails.

```csharp
public sealed class VaultOperationException : Exception
{
    /// <summary>
    /// Initializes a new instance with an error message.
    /// </summary>
    public VaultOperationException(string message);

    /// <summary>
    /// Initializes a new instance with a message and inner exception.
    /// </summary>
    public VaultOperationException(string message, Exception innerException);
}
```

**Scope:** `VaultOperationException` represents operational failures in provider access (network, IAM, authentication, service unavailability). It does not represent missing secrets or config keys—use `SecretNotFoundException` or `ConfigurationNotFoundException` for those cases.

**Provider Rule:** Providers must convert backend "not found" responses (e.g., HTTP 404) into `SecretNotFoundException`, not `VaultOperationException`. `VaultOperationException` is reserved for operational failures.

### Usage Example

```csharp
// Throwing the exception
public async Task<SecretValue> GetSecretAsync(
    SecretIdentifier identifier,
    CancellationToken ct)
{
    try
    {
        return await _provider.FetchSecretAsync(identifier.Name, ct);
    }
    catch (RequestFailedException ex) when (ex.Status != 404)
    {
        // Wrap provider-specific exceptions
        throw new VaultOperationException(
            $"Failed to retrieve secret '{identifier.Name}' from Azure Key Vault",
            ex);
    }
}

// Catching the exception
try
{
    var secret = await secretStore.GetSecretAsync(
        new SecretIdentifier("api-key"),
        ct);
}
catch (VaultOperationException ex)
{
    _logger.LogError(
        ex,
        "Vault operation failed: {Message}",
        ex.Message);
    
    // Handle operational failure (retry, alert, etc.)
    throw;
}
```

### When to Use

| Scenario | Exception |
|----------|-----------|
| Network failure | `VaultOperationException` |
| Authentication failure | `VaultOperationException` |
| Permission denied | `VaultOperationException` |
| Provider unavailable | `VaultOperationException` |
| Timeout | `VaultOperationException` |
| Serialization failure | `VaultOperationException` |

[↑ Back to top](#table-of-contents)

---

## 🔗 How Exceptions Drive Kernel Health

Vault exceptions directly influence Kernel health and readiness:

| Exception | Warmup (Startup) | Health Check | Readiness Check |
|-----------|------------------|--------------|------------------|
| `SecretNotFoundException` | **Readiness failure** (warmup key missing) | No impact | **Readiness failure** |
| `VaultOperationException` | **Startup failure** (provider unreachable) | **Liveness failure** | **Readiness failure** |
| `ConfigurationNotFoundException` | No impact (unless explicitly configured) | No impact | No impact |

**Integration Points:**
- **`VaultStartupHook`**: Warms cache using `WarmupKeys`. `SecretNotFoundException` → readiness failure.
- **`VaultHealthContributor`**: Checks provider connectivity. `VaultOperationException` → liveness failure.
- **`VaultReadinessContributor`**: Validates providers are available. Exceptions → readiness failure.

See [Lifecycle.md](Lifecycle.md) and [Health.md](Health.md) for details.

[↑ Back to top](#table-of-contents)

---

## Exception Handling Patterns

### Try Pattern (Recommended)

**Philosophy:** Vault follows a "use exceptions for exceptional conditions" philosophy. Missing secrets or config keys are not exceptional when using `Try*` methods, so use `Try*` for optional or user-dependent values.

```csharp
// Use Try* methods to avoid exceptions for expected cases
var result = await secretStore.TryGetSecretAsync(
    new SecretIdentifier("optional-secret"),
    ct);

if (result.IsSuccess)
{
    // Use the secret
    UseSecret(result.Value!.Value);
}
else
{
    // Handle gracefully
    _logger.LogInformation(
        "Optional secret not available: {Error}",
        result.ErrorMessage);
    UseFallback();
}
```

### Exception Pattern (When Required)

```csharp
// Use Get* methods when secret is required
try
{
    var secret = await secretStore.GetSecretAsync(
        new SecretIdentifier("required-secret"),
        ct);
    
    UseSecret(secret.Value);
}
catch (SecretNotFoundException ex)
{
    // Configuration error - secret should exist
    _logger.LogCritical(
        "Required secret '{SecretName}' is missing",
        ex.SecretName);
    throw;
}
catch (VaultOperationException ex)
{
    // Operational error - transient failure
    _logger.LogError(
        ex,
        "Failed to retrieve required secret");
    throw;
}
```

### Global Exception Handler

**Security Note:** Vault never includes secret values in exceptions. Only metadata (key names, provider names, error types) should be exposed. Do not log entire exception objects that might contain sensitive inner exception details.

```csharp
// In ASP.NET Core middleware
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        
        var response = exception switch
        {
            SecretNotFoundException ex => new
            {
                Error = "Configuration Error",
                Message = $"Secret '{ex.SecretName}' is not configured"
            },
            ConfigurationNotFoundException ex => new
            {
                Error = "Configuration Error",
                Message = $"Configuration '{ex.Key}' is missing"
            },
            VaultOperationException ex => new
            {
                Error = "Service Unavailable",
                Message = "Unable to access secrets store"
            },
            _ => new
            {
                Error = "Internal Error",
                Message = "An unexpected error occurred"
            }
        };

        context.Response.StatusCode = exception switch
        {
            SecretNotFoundException => 500,
            ConfigurationNotFoundException => 500,
            VaultOperationException => 503,
            _ => 500
        };

        await context.Response.WriteAsJsonAsync(response);
    });
});
```

[↑ Back to top](#table-of-contents)

---

## Summary

Exception hierarchy for vault operations:

| Exception | Cause | Recovery |
|-----------|-------|----------|
| `SecretNotFoundException` | Secret doesn't exist | Use fallback or fail fast |
| `ConfigurationNotFoundException` | Config key missing | Use default or fail fast |
| `VaultOperationException` | Operational failure | Retry or circuit break |

**Best Practices:**
1. Use `Try*` methods when value is optional
2. Use `Get*` methods when value is required
3. Catch specific exceptions, not `Exception`
4. Log with structured data (key names, not values)
5. **Never expose secret values in exception messages or logs**
6. Providers: Convert 404/not-found → `SecretNotFoundException`, everything else → `VaultOperationException`
7. Don't expose internal exception details to clients

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
