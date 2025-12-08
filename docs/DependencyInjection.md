# 🔌 DependencyInjection - Service Registration

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [VaultServiceCollectionExtensions.cs](#vaultservicecollectionextensionscs)
- [HoneyDrunkBuilderExtensions.cs](#honeydrunkbuilderextensionscs)

---

## Overview

Extension methods for registering vault services with the dependency injection container.

**Location:** `HoneyDrunk.Vault/Extensions/`

---

## VaultServiceCollectionExtensions.cs

Extensions for `IServiceCollection` to add vault core services.

```csharp
public static class VaultServiceCollectionExtensions
{
    /// <summary>
    /// Adds core vault services to the service collection.
    /// This registers the VaultClient orchestrator but not provider implementations.
    /// </summary>
    public static IServiceCollection AddVaultCore(this IServiceCollection services);
}
```

### Usage Example

```csharp
// Basic registration (no providers)
builder.Services.AddVaultCore();

// Core services registered:
// - IVaultClient -> VaultClient
// - IConfigProvider -> ConfigSourceAdapter (wraps IConfigSource)
```

### What Gets Registered

```csharp
public static IServiceCollection AddVaultCore(this IServiceCollection services)
{
    ArgumentNullException.ThrowIfNull(services);

    // Register the central orchestrator
    services.TryAddSingleton<IVaultClient, VaultClient>();

    // Register IConfigProvider as a wrapper around IConfigSource
    services.TryAddSingleton<IConfigProvider>(sp =>
    {
        var configSource = sp.GetService<IConfigSource>();
        
        // If IConfigSource already implements IConfigProvider, use directly
        if (configSource is IConfigProvider provider)
        {
            return provider;
        }

        // Otherwise wrap in adapter
        return configSource != null
            ? new ConfigSourceAdapter(configSource)
            : throw new InvalidOperationException("No IConfigSource registered");
    });

    return services;
}
```

[↑ Back to top](#table-of-contents)

---

## HoneyDrunkBuilderExtensions.cs

Extensions for `IHoneyDrunkBuilder` to add vault with Kernel integration.

```csharp
public static class HoneyDrunkBuilderExtensions
{
    /// <summary>
    /// Adds vault services with full Kernel integration.
    /// </summary>
    public static IHoneyDrunkBuilder AddVault(
        this IHoneyDrunkBuilder builder,
        Action<VaultOptions> configure);
}
```

### Usage Example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHoneyDrunk(options =>
    {
        options.NodeId = "order-service";
        options.StudioId = "my-studio";
    })
    .AddVault(vault =>
    {
        // Configure caching
        vault.Cache.Enabled = true;
        vault.Cache.DefaultTtl = TimeSpan.FromMinutes(15);

        // Configure resilience
        vault.Resilience.RetryEnabled = true;
        vault.Resilience.MaxRetryAttempts = 3;

        // Add provider
        vault.AddAzureKeyVaultProvider(akv =>
        {
            akv.VaultUri = new Uri("https://my-vault.vault.azure.net/");
            akv.UseManagedIdentity = true;
        });

        // Warmup and health
        vault.WarmupKeys.Add("database-connection-string");
        vault.HealthCheckSecretKey = "health-check-secret";
    });
```

### What Gets Registered

```csharp
public static IHoneyDrunkBuilder AddVault(
    this IHoneyDrunkBuilder builder,
    Action<VaultOptions> configure)
{
    var services = builder.Services;
    
    // Configure options
    services.Configure(configure);
    
    // Register core services
    services.AddVaultCore();
    
    // Register cache
    services.TryAddSingleton<SecretCache>();
    
    // Register telemetry
    services.TryAddSingleton<VaultTelemetry>();
    
    // Register Kernel lifecycle hooks
    services.AddSingleton<IStartupHook, VaultStartupHook>();
    services.AddSingleton<IHealthContributor, VaultHealthContributor>();
    services.AddSingleton<IReadinessContributor, VaultReadinessContributor>();
    
    return builder;
}
```

### Complete Registration Flow

```
AddHoneyDrunk()
    │
    ├── Kernel services (IGridContext, etc.)
    │
    └── AddVault()
            │
            ├── VaultOptions
            ├── VaultClient
            ├── SecretCache
            ├── VaultTelemetry
            ├── VaultStartupHook
            ├── VaultHealthContributor
            └── VaultReadinessContributor
                    │
                    └── Provider Extensions
                            │
                            ├── AddVaultWithAzureKeyVault()
                            ├── AddVaultWithFile()
                            ├── AddVaultWithAws()
                            └── AddVaultWithInMemory()
```

[↑ Back to top](#table-of-contents)

---

## Provider Extension Methods

Each provider package includes its own extension method:

### File Provider

```csharp
// HoneyDrunk.Vault.Providers.File
services.AddVaultWithFile(options =>
{
    options.SecretsFilePath = "secrets/dev-secrets.json";
    options.ConfigFilePath = "secrets/dev-config.json";
    options.WatchForChanges = true;
    options.CreateIfNotExists = true;
});
```

### Azure Key Vault Provider

```csharp
// HoneyDrunk.Vault.Providers.AzureKeyVault
services.AddVaultWithAzureKeyVault(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    options.UseManagedIdentity = true;
});
```

### AWS Secrets Manager Provider

```csharp
// HoneyDrunk.Vault.Providers.Aws
services.AddVaultWithAwsSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.UseInstanceProfile = true;
    options.SecretPrefix = "prod/myapp/";
});
```

### InMemory Provider

```csharp
// HoneyDrunk.Vault.Providers.InMemory
services.AddVaultWithInMemory(options =>
{
    options.SetSecret("api-key", "test-key");
    options.SetSecret("db-connection", "Server=localhost;...");
    options.SetConfig("timeout", "30");
});
```

### Configuration Provider

```csharp
// HoneyDrunk.Vault.Providers.Configuration
services.AddVaultWithConfiguration();
// Uses IConfiguration from DI
```

[↑ Back to top](#table-of-contents)

---

## Environment-Based Registration

```csharp
var builder = WebApplication.CreateBuilder(args);

// Choose provider based on environment
if (builder.Environment.IsDevelopment())
{
    // File-based for local development
    builder.Services.AddVaultWithFile(options =>
    {
        options.SecretsFilePath = "secrets/dev-secrets.json";
        options.WatchForChanges = true;
    });
}
else if (builder.Environment.IsEnvironment("Testing"))
{
    // InMemory for tests
    builder.Services.AddVaultWithInMemory(options =>
    {
        options.SetSecret("api-key", "test-key");
    });
}
else
{
    // Azure Key Vault for production
    builder.Services.AddVaultWithAzureKeyVault(options =>
    {
        options.VaultUri = new Uri(
            builder.Configuration["KeyVault:Uri"]!);
        options.UseManagedIdentity = true;
    });
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

Registration is layered for flexibility:

| Extension | Package | Registers |
|-----------|---------|-----------|
| `AddVaultCore()` | Core | VaultClient, IConfigProvider |
| `AddVault()` | Core (Kernel) | Full Kernel integration |
| `AddVaultWithFile()` | File | FileSecretStore, FileConfigSource |
| `AddVaultWithAzureKeyVault()` | Azure | AzureKeyVaultSecretStore |
| `AddVaultWithAwsSecretsManager()` | AWS | AwsSecretsManagerSecretStore |
| `AddVaultWithInMemory()` | InMemory | InMemorySecretStore |
| `AddVaultWithConfiguration()` | Configuration | ConfigurationSecretStore |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
