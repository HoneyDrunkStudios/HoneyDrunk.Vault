# 📋 Abstractions - Core Contracts and Interfaces

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [ISecretStore.cs](#isecretstorecs)
- [ISecretProvider.cs](#isecretprovidercs)
- [IConfigSource.cs](#iconfigsourcecs)
- [IConfigProvider.cs](#iconfigprovidercs)
- [IVaultClient.cs](#ivaultclientcs)

---

## Overview

Core contracts for Vault, split into exported and internal:

**Exported** for other Nodes and app code:
- **`ISecretStore`** — Primary secret access
- **`IConfigProvider`** — Typed configuration access

**Internal** to Vault and provider packages:
- **`ISecretProvider`** — Backend-specific secret provider
- **`IConfigSource`** — Raw configuration source
- **`IVaultClient`** — Internal orchestrator

**Location:** `HoneyDrunk.Vault/Abstractions/`

Providers implement the internal interfaces. Applications and other Nodes depend only on the exported ones.

---

## ISecretStore.cs

**Exported Contract**

Primary interface for secret access. This is one of Vault's exported contracts and the main interface application and Node code should depend on.

```csharp
public interface ISecretStore
{
    /// <summary>
    /// Gets a secret by its identifier.
    /// </summary>
    /// <param name="identifier">The secret identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The secret value.</returns>
    /// <exception cref="SecretNotFoundException">Thrown when secret is not found.</exception>
    Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get a secret by its identifier.
    /// </summary>
    /// <param name="identifier">The secret identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the secret or failure info.</returns>
    Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all versions of a secret.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of secret versions.</returns>
    Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
        string secretName, 
        CancellationToken cancellationToken = default);
}
```

### Usage Example

```csharp
public class DatabaseService(ISecretStore secretStore)
{
    public async Task<string> GetConnectionStringAsync(CancellationToken ct)
    {
        // Get secret - throws if not found
        var secret = await secretStore.GetSecretAsync(
            new SecretIdentifier("db-connection-string"),
            ct);
        return secret.Value;
    }

    public async Task<string?> TryGetApiKeyAsync(CancellationToken ct)
    {
        // Try get - returns result with success/failure
        var result = await secretStore.TryGetSecretAsync(
            new SecretIdentifier("api-key"),
            ct);
        
        return result.IsSuccess ? result.Value!.Value : null;
    }

    public async Task<IReadOnlyList<SecretVersion>> GetVersionsAsync(CancellationToken ct)
    {
        // List all versions of a secret
        return await secretStore.ListSecretVersionsAsync("api-key", ct);
    }
}
```

[↑ Back to top](#table-of-contents)

---

## ISecretProvider.cs

**Internal Contract**

Backend-specific provider interface. Implementations provide access to specific secret stores (File, Azure KV, AWS, etc.).

**Note:** `IsAvailable` is a cheap, config-based check (credentials present, enabled flag). `CheckHealthAsync` is a deeper check, potentially involving remote calls, used by health contributors.

```csharp
public interface ISecretProvider
{
    /// <summary>
    /// Gets the logical name of this provider (e.g., "file", "azure-keyvault").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets a value indicating whether this provider is available.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Fetches a secret from the backend.
    /// </summary>
    Task<SecretValue> FetchSecretAsync(
        string key, 
        string? version = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to fetch a secret from the backend.
    /// </summary>
    Task<VaultResult<SecretValue>> TryFetchSecretAsync(
        string key, 
        string? version = null, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all versions of a secret from the backend.
    /// </summary>
    Task<IReadOnlyList<SecretVersion>> ListVersionsAsync(
        string key, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the provider is healthy.
    /// </summary>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
```

### Usage Example

```csharp
// Example provider that implements both ISecretStore and ISecretProvider for off-grid scenarios.
// In practice, providers may implement only ISecretProvider and let Vault core own the ISecretStore facade.
public sealed class AzureKeyVaultSecretStore : ISecretStore, ISecretProvider
{
    public string ProviderName => "azure-key-vault";
    public bool IsAvailable => true;

    public async Task<SecretValue> FetchSecretAsync(
        string key, 
        string? version = null, 
        CancellationToken ct = default)
    {
        // Fetch from Azure Key Vault
        var secret = await _secretClient.GetSecretAsync(key, version, ct);
        return new SecretValue(
            new SecretIdentifier(key, version),
            secret.Value.Value,
            secret.Value.Properties.Version);
    }

    public async Task<bool> CheckHealthAsync(CancellationToken ct)
    {
        try
        {
            // Simple connectivity check
            await _secretClient.GetPropertiesOfSecretsAsync(ct).AsPages().FirstAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
```

[↑ Back to top](#table-of-contents)

---

## IConfigSource.cs

**Internal Contract**

Raw configuration access for provider implementations and internal adapters. Not intended for injection into application services; app code should use `IConfigProvider`.

```csharp
public interface IConfigSource
{
    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <exception cref="ConfigurationNotFoundException">Thrown when key not found.</exception>
    Task<string> GetConfigValueAsync(
        string key, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get a configuration value by key.
    /// </summary>
    Task<string?> TryGetConfigValueAsync(
        string key, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed configuration value by key.
    /// </summary>
    Task<T> GetConfigValueAsync<T>(
        string key, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get a typed configuration value with default.
    /// </summary>
    Task<T> TryGetConfigValueAsync<T>(
        string key, 
        T defaultValue, 
        CancellationToken cancellationToken = default);
}
```

### Usage Example

```csharp
// Internal adapter example: ConfigSourceAdapter wraps IConfigSource to provide IConfigProvider
internal sealed class ConfigSourceAdapter(IConfigSource configSource) : IConfigProvider
{
    public async Task<string> GetValueAsync(string key, CancellationToken ct)
    {
        return await configSource.GetConfigValueAsync(key, ct);
    }

    public async Task<T> GetValueAsync<T>(string key, T defaultValue, CancellationToken ct)
    {
        return await configSource.TryGetConfigValueAsync(key, defaultValue, ct);
    }
}
```

[↑ Back to top](#table-of-contents)

---

## IConfigProvider.cs

**Exported Contract**

Typed configuration access interface. This is Vault's exported configuration contract. Application and Node code should inject this rather than `IConfigSource`.

```csharp
public interface IConfigProvider
{
    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <exception cref="ConfigurationNotFoundException">Thrown when key not found.</exception>
    Task<string> GetValueAsync(
        string key, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed configuration value with a default fallback.
    /// </summary>
    Task<T> GetValueAsync<T>(
        string key, 
        T defaultValue, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to get a configuration value by key.
    /// </summary>
    Task<string?> TryGetValueAsync(
        string key, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a typed configuration value by key.
    /// </summary>
    /// <exception cref="ConfigurationNotFoundException">Thrown when key not found.</exception>
    Task<T> GetValueAsync<T>(
        string key, 
        CancellationToken cancellationToken = default);
}
```

### Usage Example

```csharp
public class AppSettingsService(IConfigProvider configProvider)
{
    public async Task<AppSettings> GetSettingsAsync(CancellationToken ct)
    {
        return new AppSettings
        {
            MaxConnections = await configProvider.GetValueAsync<int>(
                "max-connections",
                defaultValue: 100,
                ct),
            
            EnableCaching = await configProvider.GetValueAsync<bool>(
                "caching:enabled",
                defaultValue: true,
                ct),
            
            LogLevel = await configProvider.GetValueAsync(
                "logging:level",
                ct)
        };
    }
}
```

[↑ Back to top](#table-of-contents)

---

## IVaultClient.cs

**Internal Contract**

Unified orchestrator interface used by Vault core and infrastructure components. Application and business code should depend on `ISecretStore` and `IConfigProvider` instead.

```csharp
public interface IVaultClient
{
    // Secret operations
    Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier, 
        CancellationToken cancellationToken = default);
    
    Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier, 
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
        string secretName, 
        CancellationToken cancellationToken = default);

    // Configuration operations
    Task<string> GetConfigValueAsync(
        string key, 
        CancellationToken cancellationToken = default);
    
    Task<string?> TryGetConfigValueAsync(
        string key, 
        CancellationToken cancellationToken = default);
    
    Task<T> GetConfigValueAsync<T>(
        string key, 
        CancellationToken cancellationToken = default);
    
    Task<T> TryGetConfigValueAsync<T>(
        string key, 
        T defaultValue, 
        CancellationToken cancellationToken = default);
}
```

### Usage Example - Infrastructure Only

```csharp
// Infrastructure component example - NOT business logic
// Business logic should inject ISecretStore and IConfigProvider separately
internal sealed class VaultBootstrapHelper(IVaultClient vaultClient, ILogger logger)
{
    public async Task WarmupCriticalResourcesAsync(CancellationToken ct)
    {
        // Infrastructure code can use IVaultClient for convenience
        // when it needs both secrets and config
        
        var dbSecret = await vaultClient.GetSecretAsync(
            new SecretIdentifier("database-connection"),
            ct);
        logger.LogInformation("Warmed up database secret");
        
        var maxConnections = await vaultClient.TryGetConfigValueAsync<int>(
            "database:max-connections",
            defaultValue: 100,
            ct);
        logger.LogInformation("Max connections: {MaxConnections}", maxConnections);
    }
}

// Preferred pattern for business logic:
public class PaymentService(ISecretStore secretStore, IConfigProvider configProvider)
{
    public async Task ProcessPaymentAsync(Payment payment, CancellationToken ct)
    {
        // Get API key secret via ISecretStore
        var apiKeySecret = await secretStore.GetSecretAsync(
            new SecretIdentifier("payment-gateway-api-key"),
            ct);
        
        // Get configuration via IConfigProvider
        var timeout = await configProvider.GetValueAsync(
            "payment:timeout-seconds",
            defaultValue: 30,
            ct);
        
        var maxRetries = await configProvider.GetValueAsync(
            "payment:max-retries",
            defaultValue: 3,
            ct);

        await ProcessWithGateway(payment, apiKeySecret.Value, timeout, maxRetries, ct);
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

The abstractions layer provides a clean separation between vault operations and backend implementations:

| Interface | Purpose | Use Case | Scope |
|-----------|---------|----------|-------|
| `ISecretStore` | Primary secret access | Application and Node services needing secrets | **Exported** |
| `IConfigProvider` | Typed config access | Strongly-typed settings and feature flags | **Exported** |
| `ISecretProvider` | Backend secret access | Provider implementations | **Internal** |
| `IConfigSource` | Raw config source | Provider implementations and adapters | **Internal** |
| `IVaultClient` | Unified orchestrator | Vault core or infra components only | **Internal** |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
