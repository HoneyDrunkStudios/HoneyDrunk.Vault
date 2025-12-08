# ⚙️ Configuration Provider - IConfiguration Bridge

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Configuration](#configuration)
- [ConfigurationSecretStore.cs](#configurationsecretstorecs)
- [ConfigurationConfigSource.cs](#configurationconfigsourcecs)
- [Use Cases](#use-cases)

---

## Overview

IConfiguration-based provider that bridges ASP.NET Core's configuration system to the vault abstractions. Allows secrets and configuration from `appsettings.json`, environment variables, or any `IConfiguration` source to be accessed through the vault interface.

**Location:** `HoneyDrunk.Vault.Providers.Configuration/`

**Use Cases:**
- Bridging existing configuration to vault interface
- Simple applications with configuration-based secrets
- Migrating from IConfiguration to vault pattern
- Local development fallback

**⚠️ Note:** For production secrets, consider Azure Key Vault or AWS Secrets Manager instead.

---

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.Configuration
```

---

## Configuration

### Basic Setup

```csharp
using HoneyDrunk.Vault.Providers.Configuration.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Uses the existing IConfiguration from the DI container
builder.Services.AddVaultWithConfiguration();

var app = builder.Build();
```

### Configuration Format

Secrets are read from a `Secrets:` section in configuration:

```json
// appsettings.json
{
  "Secrets": {
    "database-connection-string": "Server=localhost;Database=myapp;",
    "api-key": "my-api-key-12345"
  },
  "Settings": {
    "logging:level": "Debug",
    "cache:enabled": "true"
  }
}
```

### Environment Variables

Secrets can also come from environment variables:

```bash
# Environment variables are automatically flattened
Secrets__database-connection-string=Server=prod;Database=myapp;
Secrets__api-key=prod-api-key
```

---

## ConfigurationSecretStore.cs

IConfiguration-based implementation of `ISecretStore`.

```csharp
public sealed class ConfigurationSecretStore : ISecretStore
{
    public ConfigurationSecretStore(
        IConfiguration configuration,
        ILogger<ConfigurationSecretStore> logger);

    public Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
        string secretName,
        CancellationToken cancellationToken = default);
}
```

### Secret Resolution

Secrets are resolved from the `Secrets:` configuration section:

```csharp
// Request for "database-connection-string" maps to:
_configuration["Secrets:database-connection-string"]
```

### Usage Example

```csharp
// appsettings.json
{
  "Secrets": {
    "api-key": "my-secret-key"
  }
}

// Code
public class MyService(ISecretStore secretStore)
{
    public async Task<string> GetApiKeyAsync(CancellationToken ct)
    {
        var secret = await secretStore.GetSecretAsync(
            new SecretIdentifier("api-key"),
            ct);
        return secret.Value;
    }
}
```

---

## ConfigurationConfigSource.cs

IConfiguration-based implementation of `IConfigSource`.

```csharp
public sealed class ConfigurationConfigSource : IConfigSource
{
    public ConfigurationConfigSource(
        IConfiguration configuration,
        ILogger<ConfigurationConfigSource> logger);

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

### Configuration Resolution

Configuration values are resolved directly from IConfiguration:

```csharp
// Request for "cache:enabled" maps to:
_configuration["cache:enabled"]
```

### Usage Example

```csharp
// appsettings.json
{
  "Cache": {
    "Enabled": true,
    "TtlMinutes": 15
  }
}

// Code
public class CacheService(IConfigProvider configProvider)
{
    public async Task<bool> IsCacheEnabledAsync(CancellationToken ct)
    {
        return await configProvider.GetValueAsync<bool>(
            "Cache:Enabled",
            defaultValue: false,
            ct);
    }
}
```

---

## Use Cases

### 1. Simple Applications

For small applications without enterprise secret management needs:

```csharp
builder.Services.AddVaultWithConfiguration();

// Access secrets through uniform interface
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key"),
    ct);
```

### 2. Migration Path

Migrate from direct IConfiguration usage to vault pattern:

```csharp
// Before: Direct IConfiguration
var apiKey = configuration["Secrets:ApiKey"];

// After: Vault abstraction (same source, better interface)
var apiKey = await secretStore.GetSecretAsync(
    new SecretIdentifier("ApiKey"),
    ct);

// Future: Switch to Azure Key Vault without code changes
builder.Services.AddVaultWithAzureKeyVault(options => { ... });
```

### 3. Development Fallback

Use Configuration provider in development, cloud provider in production:

```csharp
if (builder.Environment.IsDevelopment())
{
    // Use appsettings.Development.json secrets
    builder.Services.AddVaultWithConfiguration();
}
else
{
    // Use Azure Key Vault in production
    builder.Services.AddVaultWithAzureKeyVault(options =>
    {
        options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
        options.UseManagedIdentity = true;
    });
}
```

### 4. User Secrets Integration

Leverage .NET User Secrets for development:

```bash
# Initialize user secrets
dotnet user-secrets init

# Set secrets
dotnet user-secrets set "Secrets:api-key" "dev-api-key"
dotnet user-secrets set "Secrets:db-password" "dev-password"
```

```csharp
// User secrets are automatically included in IConfiguration in Development
builder.Services.AddVaultWithConfiguration();
```

---

## Best Practices

### 1. Use for Non-Sensitive Configuration

```json
// ✅ Good - Non-sensitive settings
{
  "Settings": {
    "logging:level": "Debug",
    "cache:ttl": "00:15:00"
  }
}

// ⚠️ Caution - Sensitive data in config files
{
  "Secrets": {
    "api-key": "real-production-key"  // Don't commit this!
  }
}
```

### 2. Combine with User Secrets

```csharp
// appsettings.json - non-sensitive defaults
{
  "Secrets": {
    "api-key": "placeholder"
  }
}

// User secrets (secrets.json) - actual values
{
  "Secrets": {
    "api-key": "real-dev-key"
  }
}
```

### 3. Use Environment Variables for Secrets

```bash
# Set secrets via environment variables
export Secrets__api-key="production-key"
export Secrets__db-password="production-password"
```

---

## Summary

Configuration provider bridges IConfiguration to vault:

| Feature | Supported |
|---------|-----------|
| Secrets | ✅ |
| Configuration | ✅ |
| User Secrets | ✅ |
| Environment Variables | ✅ |
| Versioning | ❌ |
| Rotation | ❌ |
| Production use | ⚠️ Limited |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
