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
    /// This registers the VaultClient orchestrator and ISecretStore/IConfigProvider contracts.
    /// Does not register provider implementations - use AddVaultWithX methods.
    /// </summary>
    public static IServiceCollection AddVaultCore(this IServiceCollection services);
}
```

**Provider Requirement:** `AddVaultCore()` does not register any providers. At least one provider must be registered using `AddVaultWithX()` (File, AzureKeyVault, AWS, InMemory, Configuration). If no providers are registered, provider resolution will fail at runtime.

### Usage Example

```csharp
// Basic registration (no providers yet - must add at least one)
builder.Services.AddVaultCore();

// Core services registered:
// - ISecretStore -> CompositeSecretStore (exported contract)
// - IConfigSource -> CompositeConfigSource (internal)
// - IConfigProvider -> CompositeConfigSource (exported contract)
```

### What Gets Registered

```csharp
public static IServiceCollection AddVaultCore(this IServiceCollection services)
{
    ArgumentNullException.ThrowIfNull(services);

    // Register composite stores that orchestrate multiple providers
    services.TryAddSingleton<CompositeSecretStore>();
    services.TryAddSingleton<CompositeConfigSource>();

    // Register the exported contracts pointing to composites
    services.TryAddSingleton<ISecretStore>(sp => sp.GetRequiredService<CompositeSecretStore>());
    services.TryAddSingleton<IConfigSource>(sp => sp.GetRequiredService<CompositeConfigSource>());
    services.TryAddSingleton<IConfigProvider>(sp => sp.GetRequiredService<CompositeConfigSource>());

    // Register supporting services
    services.TryAddSingleton<ResiliencePipelineFactory>();
    services.TryAddSingleton<VaultTelemetry>();

    return services;
}
```

**Composite Architecture:** `CompositeConfigSource` implements both `IConfigSource` and `IConfigProvider`. It orchestrates registered `IConfigSourceProvider` instances with priority-based selection and fallback. Individual providers are registered via `AddConfigSourceProvider()`.

**Provider Resolution:** Providers registered via `AddVaultWithX()` call `AddSecretProvider()` and/or `AddConfigSourceProvider()` to register themselves with the composite stores. The composites handle provider selection based on priority, availability, and health status.

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
    .AddHoneyDrunkGrid(grid =>
    {
        grid.StudioId = "my-studio";
    })
    .AddHoneyDrunkNode(node =>
    {
        node.NodeId = "order-service";
    })
    .AddVault(vault =>
    {
        // Configure caching
        vault.Cache.Enabled = true;
        vault.Cache.DefaultTtl = TimeSpan.FromMinutes(15);

        // Configure resilience
        vault.Resilience.RetryEnabled = true;
        vault.Resilience.MaxRetryAttempts = 3;

        // Warmup and health
        vault.WarmupKeys.Add("database-connection-string");
        vault.HealthCheckSecretKey = "health-check-secret";
    })
    .AddVaultWithAzureKeyVault(akv =>
    {
        akv.VaultUri = new Uri("https://my-vault.vault.azure.net/");
        akv.UseManagedIdentity = true;
    });
```
```

### What Gets Registered

```csharp
public static IHoneyDrunkBuilder AddVault(
    this IHoneyDrunkBuilder builder,
    Action<VaultOptions> configure)
{
    var services = builder.Services;
    
    // Configure options
    services.Configure<VaultOptions>(configure);
    
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

**What AddVaultCore Registers:**
- `ISecretStore` → Vault implementation (exported contract)
- `IVaultClient` → Internal orchestrator
- `IConfigProvider` → Adapter over `IConfigSource` (if available)
- `SecretCache` → In-memory caching
- `VaultTelemetry` → ActivitySource tracing

**What AddVault Adds:**
- Configures `VaultOptions`
- Registers Kernel lifecycle hooks (`IStartupHook`, `IHealthContributor`, `IReadinessContributor`)

### Complete Registration Flow

```
AddHoneyDrunkGrid()
    │
    ├── Grid services (IGridContext, etc.)
    │
    └── AddHoneyDrunkNode()
            │
            ├── Node services
            │
            └── AddVault()
                    │
                    ├── VaultOptions
                    ├── ISecretStore (exported)
                    ├── IVaultClient (internal)
                    ├── IConfigProvider (exported)
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
                                    ├── AddVaultWithAwsSecretsManager()
                                    └── AddVaultInMemory()
```

[↑ Back to top](#table-of-contents)

---

## Provider Extension Methods

Each provider package includes its own extension method. Provider extension methods must be called after `AddVault()` or `AddVaultCore()` so the DI container has `VaultOptions` available.

**What Provider Extensions Register:**
- Provider-specific `ISecretProvider` implementation
- Provider-specific `IConfigSource` implementation (if supported)
- Provider registration in `VaultOptions.Providers`

### File Provider

*Registers: `ISecretProvider`, `IConfigSource`*

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

*Registers: `ISecretProvider` (secrets only)*

```csharp
// HoneyDrunk.Vault.Providers.AzureKeyVault
services.AddVaultWithAzureKeyVault(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    options.UseManagedIdentity = true;
});
```

### AWS Secrets Manager Provider

*Registers: `ISecretProvider` (secrets only)*

```csharp
// HoneyDrunk.Vault.Providers.Aws
services.AddVaultWithAwsSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.SecretPrefix = "prod/myapp/";
});
```

### InMemory Provider

*Registers: `ISecretProvider`, `IConfigSource` (for testing)*

```csharp
// HoneyDrunk.Vault.Providers.InMemory
services.AddVaultInMemory(options =>
{
    options.AddSecret("api-key", "test-key");
    options.AddSecret("db-connection", "Server=localhost;...");
    options.AddConfigValue("timeout", "30");
});
```

### Configuration Provider

*Registers: `ISecretProvider`, `IConfigSource` (IConfiguration bridge)*

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
    builder.Services.AddVaultInMemory(options =>
    {
        options.AddSecret("api-key", "test-key");
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
|-----------|---------|-----------||
| `AddVaultCore()` | Core | `ISecretStore`, `IVaultClient`, `IConfigProvider` (exported contracts) |
| `AddVault()` | Core (Kernel) | Adds Kernel lifecycle hooks to `AddVaultCore()` |
| `AddVaultWithFile()` | File | `ISecretProvider`, `IConfigSource` |
| `AddVaultWithAzureKeyVault()` | Azure | `ISecretProvider` (secrets only) |
| `AddVaultWithAwsSecretsManager()` | AWS | `ISecretProvider` (secrets only) |
| `AddVaultInMemory()` | InMemory | `ISecretProvider`, `IConfigSource` (testing) |
| `AddVaultWithConfiguration()` | Configuration | `ISecretProvider`, `IConfigSource` (IConfiguration bridge) |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
