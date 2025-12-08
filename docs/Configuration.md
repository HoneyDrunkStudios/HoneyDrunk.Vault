# ⚙️ Configuration - Settings and Options

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [VaultOptions.cs](#vaultoptionscs)
- [VaultCacheOptions.cs](#vaultcacheoptionscs)
- [VaultResilienceOptions.cs](#vaultresilienceoptionscs)
- [ProviderRegistration.cs](#providerregistrationcs)
- [ProviderType.cs](#providertypecs)
- [Provider-Specific Options](#provider-specific-options)

---

## Overview

Configuration classes for the vault system. These options control caching, resilience, and provider behavior.

**Location:** `HoneyDrunk.Vault/Configuration/`

---

## VaultOptions.cs

Main configuration class for the vault system.

```csharp
public sealed class VaultOptions
{
    /// <summary>
    /// Gets the provider registrations by logical name.
    /// </summary>
    public Dictionary<string, ProviderRegistration> Providers { get; }

    /// <summary>
    /// Gets or sets the default provider name.
    /// </summary>
    public string? DefaultProvider { get; set; }

    /// <summary>
    /// Gets or sets the caching options.
    /// </summary>
    public VaultCacheOptions Cache { get; set; }

    /// <summary>
    /// Gets or sets the resilience options.
    /// </summary>
    public VaultResilienceOptions Resilience { get; set; }

    /// <summary>
    /// Gets or sets whether telemetry is enabled.
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Gets the list of secret keys to warm cache with on startup.
    /// </summary>
    public List<string> WarmupKeys { get; }

    /// <summary>
    /// Gets or sets the health check secret key.
    /// </summary>
    public string? HealthCheckSecretKey { get; set; }

    // Fluent configuration methods
    public VaultOptions AddProvider(string name, Action<ProviderRegistration> configure);
    public VaultOptions AddFileProvider(Action<FileProviderOptions>? configure = null);
    public VaultOptions AddAzureKeyVaultProvider(Action<AzureKeyVaultProviderOptions> configure);
    public VaultOptions AddAwsSecretsManagerProvider(Action<AwsSecretsManagerProviderOptions> configure);
    public VaultOptions AddInMemoryProvider(Action<InMemoryProviderOptions>? configure = null);
}
```

### Usage Example

```csharp
builder.Services.AddVault(options =>
{
    // Configure caching
    options.Cache.Enabled = true;
    options.Cache.DefaultTtl = TimeSpan.FromMinutes(15);
    options.Cache.MaxSize = 1000;

    // Configure resilience
    options.Resilience.RetryEnabled = true;
    options.Resilience.MaxRetryAttempts = 3;
    options.Resilience.CircuitBreakerEnabled = true;

    // Enable telemetry
    options.EnableTelemetry = true;

    // Add providers
    options.AddAzureKeyVaultProvider(akv =>
    {
        akv.VaultUri = new Uri("https://my-vault.vault.azure.net/");
        akv.UseManagedIdentity = true;
    });

    // Warm up critical secrets
    options.WarmupKeys.Add("database-connection-string");
    options.WarmupKeys.Add("redis-connection-string");

    // Health check secret
    options.HealthCheckSecretKey = "health-check-secret";
});
```

[↑ Back to top](#table-of-contents)

---

## VaultCacheOptions.cs

Caching configuration for the vault.

```csharp
public sealed class VaultCacheOptions
{
    /// <summary>
    /// Gets or sets whether caching is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default TTL for cached secrets.
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum number of secrets to cache.
    /// Default: 1000
    /// </summary>
    public int MaxSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the sliding expiration time.
    /// Default: null (disabled)
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }
}
```

### Usage Example

```csharp
options.Cache.Enabled = true;
options.Cache.DefaultTtl = TimeSpan.FromMinutes(15);
options.Cache.MaxSize = 500;
options.Cache.SlidingExpiration = TimeSpan.FromMinutes(5);
```

### Cache Behavior

| Setting | Effect |
|---------|--------|
| `Enabled = false` | All requests go directly to provider |
| `DefaultTtl` | Absolute expiration time for cached entries |
| `MaxSize` | LRU eviction when size exceeded |
| `SlidingExpiration` | Resets expiration on each access |

[↑ Back to top](#table-of-contents)

---

## VaultResilienceOptions.cs

Resilience configuration (retry, circuit breaker, timeout).

```csharp
public sealed class VaultResilienceOptions
{
    /// <summary>
    /// Gets or sets whether retry is enabled.
    /// Default: true
    /// </summary>
    public bool RetryEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum retry attempts.
    /// Default: 3
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial retry delay.
    /// Default: 200ms
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Gets or sets whether circuit breaker is enabled.
    /// Default: true
    /// </summary>
    public bool CircuitBreakerEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the failure threshold before circuit opens.
    /// Default: 5
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration the circuit stays open.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan CircuitBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the operation timeout.
    /// Default: 10 seconds
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
```

### Usage Example

```csharp
options.Resilience.RetryEnabled = true;
options.Resilience.MaxRetryAttempts = 5;
options.Resilience.RetryDelay = TimeSpan.FromMilliseconds(500);

options.Resilience.CircuitBreakerEnabled = true;
options.Resilience.FailureThreshold = 10;
options.Resilience.CircuitBreakDuration = TimeSpan.FromMinutes(1);

options.Resilience.Timeout = TimeSpan.FromSeconds(30);
```

### Retry Behavior

```
Attempt 1: Immediate
Attempt 2: Wait 200ms (RetryDelay)
Attempt 3: Wait 400ms (exponential backoff)
Attempt 4: Wait 800ms
...up to MaxRetryAttempts
```

### Circuit Breaker States

```
Closed (Normal)
    ↓ FailureThreshold failures
Open (Failing)
    ↓ CircuitBreakDuration expires
Half-Open (Testing)
    ↓ Success → Closed
    ↓ Failure → Open
```

[↑ Back to top](#table-of-contents)

---

## ProviderRegistration.cs

Registration information for a vault provider.

```csharp
public sealed class ProviderRegistration
{
    /// <summary>
    /// Gets or sets the logical name (e.g., "file", "azure-keyvault").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the provider type.
    /// </summary>
    public ProviderType ProviderType { get; set; }

    /// <summary>
    /// Gets or sets whether the provider is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets the provider-specific settings.
    /// </summary>
    public Dictionary<string, string> Settings { get; }
}
```

### Usage Example

```csharp
options.AddProvider("azure-keyvault", reg =>
{
    reg.ProviderType = ProviderType.AzureKeyVault;
    reg.IsEnabled = true;
    reg.Settings["VaultUri"] = "https://my-vault.vault.azure.net/";
    reg.Settings["UseManagedIdentity"] = "true";
});
```

[↑ Back to top](#table-of-contents)

---

## ProviderType.cs

Enumeration of vault provider types.

```csharp
public enum ProviderType
{
    /// <summary>
    /// File-based provider for local development.
    /// </summary>
    File,

    /// <summary>
    /// Azure Key Vault provider.
    /// </summary>
    AzureKeyVault,

    /// <summary>
    /// AWS Secrets Manager provider.
    /// </summary>
    AwsSecretsManager,

    /// <summary>
    /// In-memory provider for testing.
    /// </summary>
    InMemory,

    /// <summary>
    /// Configuration-based provider.
    /// </summary>
    Configuration,
}
```

[↑ Back to top](#table-of-contents)

---

## Provider-Specific Options

### FileProviderOptions

```csharp
public sealed class FileProviderOptions
{
    public string FilePath { get; set; } = "secrets.json";
    public string? EncryptionKeySource { get; set; }
}
```

### AzureKeyVaultProviderOptions

```csharp
public sealed class AzureKeyVaultProviderOptions
{
    public Uri? VaultUri { get; set; }
    public bool UseManagedIdentity { get; set; } = true;
    public string? ClientId { get; set; }
    public string? TenantId { get; set; }
}
```

### AwsSecretsManagerProviderOptions

```csharp
public sealed class AwsSecretsManagerProviderOptions
{
    public string? Region { get; set; }
    public string? AccessKeyId { get; set; }
    public bool UseInstanceProfile { get; set; } = true;
}
```

### InMemoryProviderOptions

```csharp
public sealed class InMemoryProviderOptions
{
    public Dictionary<string, string> Secrets { get; }
    public Dictionary<string, string> Config { get; }
    
    public void SetSecret(string key, string value);
    public void SetConfig(string key, string value);
}
```

### Usage Examples

```csharp
// File provider
options.AddFileProvider(file =>
{
    file.FilePath = "secrets/dev-secrets.json";
    file.EncryptionKeySource = "ENCRYPTION_KEY";
});

// Azure Key Vault
options.AddAzureKeyVaultProvider(akv =>
{
    akv.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    akv.UseManagedIdentity = true;
});

// AWS Secrets Manager
options.AddAwsSecretsManagerProvider(aws =>
{
    aws.Region = "us-east-1";
    aws.UseInstanceProfile = true;
});

// InMemory (for testing)
options.AddInMemoryProvider(mem =>
{
    mem.SetSecret("api-key", "test-key");
    mem.SetConfig("timeout", "30");
});
```

[↑ Back to top](#table-of-contents)

---

## Summary

Configuration is organized into logical groups:

| Class | Purpose | Scope |
|-------|---------|-------|
| `VaultOptions` | Main configuration | Global |
| `VaultCacheOptions` | Caching behavior | Global |
| `VaultResilienceOptions` | Retry/circuit breaker | Global |
| `ProviderRegistration` | Provider settings | Per-provider |
| `*ProviderOptions` | Provider-specific | Per-provider |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
