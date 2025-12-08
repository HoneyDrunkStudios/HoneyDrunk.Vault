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

    // Generic provider registration (advanced hook for custom providers)
    public VaultOptions AddProvider(string name, Action<ProviderRegistration> configure);
}
```

**Telemetry and Kernel Integration:** When `EnableTelemetry` is `true`, Vault emits ActivitySource spans and log enrichment that Pulse can ingest. Telemetry configuration is global and independent of specific providers.

**Provider Registration:** `AddProvider` is an advanced hook used by custom providers. Built-in providers should be registered via the `AddVaultWithXxx()` extension methods in their own packages.

> **Note:** Provider-specific configuration (Azure Key Vault, AWS, File, InMemory) is handled by each provider package via `AddVaultWithXxx()` extension methods. See provider documentation for details.

### Usage Example

**Grid-Integrated (Recommended):**

```csharp
// Configure with full Grid integration
builder.Services
    .AddHoneyDrunkGrid(grid => { grid.StudioId = "my-studio"; })
    .AddHoneyDrunkNode(node => { node.NodeId = "my-service"; })
    .AddVault(options =>
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

        // Warm up critical secrets
        options.WarmupKeys.Add("database-connection-string");
        options.WarmupKeys.Add("redis-connection-string");

        // Health check secret
        options.HealthCheckSecretKey = "health-check-secret";
    })
    .AddVaultWithAzureKeyVault(akv =>
    {
        akv.VaultUri = new Uri("https://my-vault.vault.azure.net/");
        akv.UseManagedIdentity = true;
    });
```

**Off-Grid (Development/Early Adoption):**

```csharp
// Configure without Grid integration
builder.Services.AddVault(options =>
{
    // Same configuration as above
    options.Cache.Enabled = true;
    options.Cache.DefaultTtl = TimeSpan.FromMinutes(15);
    // ...
});

// Add provider
builder.Services.AddVaultWithAzureKeyVault(akv =>
{
    akv.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    akv.UseManagedIdentity = true;
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

**Rotation-Friendly Caching:** `DefaultTtl` should be tuned relative to your secret rotation cadence. Vault is rotation-aware and should not cache secrets longer than they remain valid.

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

**Cache Key Scoping:** Cache keys include scope (environment, tenant, node) as well as secret name and version, so secrets are isolated between tenants and environments. This prevents cross-contamination in multi-tenant scenarios.

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

**Scope:** These settings apply to both secret and configuration retrieval from remote providers.

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

**Relationship to Provider Options:** Built-in providers map their `*Options` types to `ProviderRegistration` internally. Application code should prefer the strongly-typed `*Options` classes exposed by provider packages. `ProviderRegistration.Settings` is primarily for custom providers and advanced override scenarios.

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

    /// <summary>
    /// Custom or third-party provider.
    /// </summary>
    Custom,
}
```

**Third-Party Providers:** Third-party or application-specific providers should use `ProviderType.Custom` and carry any additional metadata in `ProviderRegistration.Settings`.

[↑ Back to top](#table-of-contents)

---

## Provider-Specific Options

Provider-specific options are defined in their respective provider packages. Each provider has its own options class with appropriate defaults.

**Package Location:** These options types live in their respective provider packages. Vault core does not depend on them directly.

### FileVaultOptions (HoneyDrunk.Vault.Providers.File)

*Supports: Secrets and Configuration*

```csharp
public sealed class FileVaultOptions
{
    public string SecretsFilePath { get; set; } = "secrets.json";
    public string? ConfigFilePath { get; set; }
    public bool WatchForChanges { get; set; } = false;
    public bool CreateIfNotExists { get; set; } = false;
}
```

### AzureKeyVaultOptions (HoneyDrunk.Vault.Providers.AzureKeyVault)

*Supports: Secrets only*

```csharp
public sealed class AzureKeyVaultOptions
{
    public Uri? VaultUri { get; set; }
    public bool UseManagedIdentity { get; set; } = true;
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}
```

### AwsSecretsManagerOptions (HoneyDrunk.Vault.Providers.Aws)

*Supports: Secrets only*

```csharp
public sealed class AwsSecretsManagerOptions
{
    public string? Region { get; set; }
    public string? ProfileName { get; set; }
    public string? ServiceUrl { get; set; }
    public string? SecretPrefix { get; set; }
    public bool UseVersionId { get; set; } = true;
    public string VersionStage { get; set; } = "AWSCURRENT";
}
```

### InMemoryVaultOptions (HoneyDrunk.Vault.Providers.InMemory)

*Supports: Secrets and Configuration (for testing)*

```csharp
public sealed class InMemoryVaultOptions
{
    public Dictionary<string, string> Secrets { get; }
    public Dictionary<string, string> ConfigurationValues { get; }
    
    public InMemoryVaultOptions AddSecret(string name, string value);
    public InMemoryVaultOptions AddConfigValue(string key, string value);
}
```

### Usage Examples

```csharp
// File provider (development)
builder.Services.AddVaultWithFile(options =>
{
    options.SecretsFilePath = "secrets/dev-secrets.json";
    options.WatchForChanges = true;
});

// Azure Key Vault (production)
builder.Services.AddVaultWithAzureKeyVault(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    options.UseManagedIdentity = true;
});

// AWS Secrets Manager
builder.Services.AddVaultWithAwsSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.SecretPrefix = "prod/myapp/";
});

// InMemory (for testing)
builder.Services.AddVaultInMemory(options =>
{
    options.AddSecret("api-key", "test-key");
    options.AddConfigValue("timeout", "30");
});
```

[↑ Back to top](#table-of-contents)

---

## Summary

Configuration is organized into logical groups:

| Class | Purpose | Scope | Consumer |
|-------|---------|-------|----------|
| `VaultOptions` | Main configuration | Global | Node / app |
| `VaultCacheOptions` | Caching behavior | Global | Node / app |
| `VaultResilienceOptions` | Retry/circuit breaker | Global | Node / app |
| `ProviderRegistration` | Provider registry | Per-provider | Vault core / advanced users |
| `*ProviderOptions` | Provider-specific options | Per-provider | Provider packages |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
