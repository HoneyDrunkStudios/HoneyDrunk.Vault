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

Core contracts that define the vault abstraction layer. These interfaces enable provider-agnostic secret and configuration access.

**Location:** `HoneyDrunk.Vault/Abstractions/`

All providers implement these interfaces, allowing applications to swap backends without changing business logic.

---

## ISecretStore.cs

Primary interface for secret access. This is the main interface applications should depend on.

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

Backend-specific provider interface. Implementations provide access to specific secret stores (File, Azure KV, AWS, etc.).

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
// Providers implement both ISecretStore and ISecretProvider
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

Raw configuration access interface. Provides string-based configuration retrieval.

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
public class FeatureService(IConfigSource configSource)
{
    public async Task<int> GetTimeoutAsync(CancellationToken ct)
    {
        // Get typed value with default
        return await configSource.TryGetConfigValueAsync<int>(
            "operation-timeout-seconds",
            defaultValue: 30,
            ct);
    }

    public async Task<bool> IsFeatureEnabledAsync(string feature, CancellationToken ct)
    {
        var value = await configSource.TryGetConfigValueAsync(
            $"feature:{feature}",
            ct);
        
        return bool.TryParse(value, out var enabled) && enabled;
    }
}
```

[↑ Back to top](#table-of-contents)

---

## IConfigProvider.cs

Typed configuration access interface. Provides strongly-typed configuration retrieval with better error handling.

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
        string path, 
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

Unified orchestrator interface. Combines secret and configuration access in a single facade.

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

### Usage Example

```csharp
public class PaymentService(IVaultClient vaultClient)
{
    public async Task ProcessPaymentAsync(Payment payment, CancellationToken ct)
    {
        // Get API key secret
        var apiKeySecret = await vaultClient.GetSecretAsync(
            new SecretIdentifier("payment-gateway-api-key"),
            ct);
        
        // Get configuration
        var timeout = await vaultClient.TryGetConfigValueAsync<int>(
            "payment:timeout-seconds",
            defaultValue: 30,
            ct);
        
        var maxRetries = await vaultClient.TryGetConfigValueAsync<int>(
            "payment:max-retries",
            defaultValue: 3,
            ct);

        // Process payment with retrieved credentials and config
        await ProcessWithGateway(payment, apiKeySecret.Value, timeout, maxRetries, ct);
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

The abstractions layer provides a clean separation between vault operations and backend implementations:

| Interface | Purpose | Use Case |
|-----------|---------|----------|
| `ISecretStore` | Primary secret access | Application services needing secrets |
| `ISecretProvider` | Backend implementation | Provider implementations |
| `IConfigSource` | Raw config access | Simple string configuration |
| `IConfigProvider` | Typed config access | Strongly-typed settings |
| `IVaultClient` | Unified orchestrator | Combined secret + config access |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
