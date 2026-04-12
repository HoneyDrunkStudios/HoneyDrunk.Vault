# HoneyDrunk.Vault

A secrets and configuration management library designed as a first-class Kernel-aware node for the HoneyDrunk.OS ecosystem.

**Vault is the Grid-facing abstraction for secrets and configuration.** Applications depend on Vault, Vault depends on providers.

## Overview

**Think of this library as a secure lockbox for your application**

Just like how a bank vault stores valuables with multiple layers of security and access control, this library provides unified access to secrets and configuration from multiple providers. It abstracts away the complexity of different secret stores (Azure Key Vault, AWS Secrets Manager, File-based, In-Memory) with caching, resilience policies, Kernel-aware lifecycle integration, and consistent APIs for secrets and configuration—delivering security, operational reliability, and developer ergonomics.

**Key Concepts:**
- **SecretIdentifier** - The unique key to locate a secret (name + optional version)
- **SecretValue** - The retrieved secret with metadata (value + version)
- **ISecretStore** - Primary interface for secret access in application code
- **IConfigProvider** - Typed configuration access with defaults
- **VaultClient** - Central orchestrator that coordinates providers
- **SecretCache** - In-memory caching layer with TTL
- **Provider** - Backend-specific implementation (File, Azure, AWS, InMemory, Configuration)

**Application Dependency:** Typical application code depends on `ISecretStore` for secrets and `IConfigProvider` for configuration. `IVaultClient` is useful when you want a single façade that can do both. `VaultClient` is the internal orchestrator that coordinates providers behind the scenes.

## Features

- **Multiple Providers**: Support for File, Azure Key Vault, AWS Secrets Manager, Configuration, and In-Memory providers
- **Kernel Integration**: Full integration with HoneyDrunk.Kernel for lifecycle, health, and telemetry
- **Caching**: Built-in caching with configurable TTL and size limits
- **Resilience**: Retry and circuit breaker policies for production reliability
- **Context-Aware**: Grid context support for distributed tracing and correlation
- **Secure Telemetry**: Telemetry traces operations without leaking secret values
- **Provider Prioritization**: Automatic fallback through multiple configured providers based on registration order and optional default provider

## Installation

```bash
# Core abstractions and orchestrator
dotnet add package HoneyDrunk.Vault

# Choose provider implementation (one or more)
dotnet add package HoneyDrunk.Vault.Providers.File              # For development
dotnet add package HoneyDrunk.Vault.Providers.AzureKeyVault     # For Azure
dotnet add package HoneyDrunk.Vault.Providers.Aws               # For AWS
dotnet add package HoneyDrunk.Vault.Providers.InMemory          # For testing
dotnet add package HoneyDrunk.Vault.Providers.Configuration     # Bridge to IConfiguration
dotnet add package HoneyDrunk.Vault.EventGrid                   # Azure Event Grid webhook invalidation
```

## Quick Start

### Using File Provider in Development

This is ideal for local development where you want to store secrets in a JSON file:

```csharp
using HoneyDrunk.Vault.Providers.File.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add vault with file provider for development
builder.Services.AddVaultWithFile(options =>
{
    options.SecretsFilePath = "secrets/dev-secrets.json";
    options.ConfigFilePath = "secrets/dev-config.json";
    options.WatchForChanges = true;
});

var app = builder.Build();

// Use the secret store
app.MapGet("/api/connection-string", async (ISecretStore secretStore) =>
{
    var secret = await secretStore.GetSecretAsync(new SecretIdentifier("database-connection-string"));
    return Results.Ok("Secret retrieved successfully");
});

app.Run();
```

**Note:** `AddVaultWithFile` registers Vault core services and the File provider. For full Kernel integration (health contributors, lifecycle hooks, telemetry), use `builder.Services.AddHoneyDrunk(...).AddVault(...)` instead (see below).

Your `secrets/dev-secrets.json` file:
```json
{
  "database-connection-string": "Server=localhost;Database=myapp;User Id=dev;Password=devpass;",
  "api-key": "dev-api-key-12345"
}
```

### Switching to Azure Key Vault in Production

Vault lets you switch providers without changing your application code. In simple cases you can bind provider options from configuration; in this example we use environment checks:

```csharp
using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // File provider for local development
    builder.Services.AddVaultWithFile(options =>
    {
        options.SecretsFilePath = "secrets/dev-secrets.json";
    });
}
else
{
    // Azure Key Vault for production
    builder.Services.AddVaultWithAzureKeyVault(options =>
    {
        options.VaultUri = new Uri(builder.Configuration["KeyVault:Uri"]!);
        options.UseManagedIdentity = true;
    });
}

var app = builder.Build();
app.Run();
```

### Using with HoneyDrunk.Kernel

For full Kernel integration with lifecycle, health, and telemetry:

```csharp
using HoneyDrunk.Kernel.Extensions;
using HoneyDrunk.Vault.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Create HoneyDrunk node with Vault
builder.Services
    .AddHoneyDrunk(options =>
    {
        options.NodeId = "my-service-node";
        options.StudioId = "my-studio";
    })
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

        // Configure provider
        options.AddAzureKeyVaultProvider(akv =>
        {
            akv.VaultUri = new Uri("https://my-vault.vault.azure.net/");
            akv.UseManagedIdentity = true;
        });

        // Warm up these secrets at startup
        options.WarmupKeys.Add("database-connection-string");
        options.WarmupKeys.Add("redis-connection-string");

        // Set health check secret
        options.HealthCheckSecretKey = "health-check-secret";
    });

var app = builder.Build();
app.Run();
```

**Kernel Integration:** When using `AddVault` with Kernel, `VaultHealthContributor` and `VaultReadinessContributor` are automatically registered and surface vault status on Kernel's health aggregation endpoints.

### Using ISecretStore

The primary interface for accessing secrets:

```csharp
public class MyService
{
    private readonly ISecretStore _secretStore;

    public MyService(ISecretStore secretStore)
    {
        _secretStore = secretStore;
    }

    public async Task<string> GetDatabaseConnectionAsync()
    {
        var secret = await _secretStore.GetSecretAsync(
            new SecretIdentifier("database-connection-string"));
        return secret.Value;
    }

    public async Task<string?> TryGetApiKeyAsync()
    {
        var result = await _secretStore.TryGetSecretAsync(
            new SecretIdentifier("optional-api-key"));
        
        return result.IsSuccess ? result.Value!.Value : null;
    }
}
```

**Provider Independence:** You rarely need to know which provider is backing the secret store. Vault resolves providers based on configuration and environment, letting you write provider-agnostic application code.

### Using IConfigProvider

For typed configuration access:

```csharp
public class MyService
{
    private readonly IConfigProvider _configProvider;

    public MyService(IConfigProvider configProvider)
    {
        _configProvider = configProvider;
    }

    public async Task<int> GetMaxConnectionsAsync()
    {
        return await _configProvider.GetValueAsync<int>("database:max-connections", 100);
    }

    public async Task<string> GetFeatureFlagAsync()
    {
        return await _configProvider.GetValueAsync("feature:new-ui", "enabled");
    }
}
```

## Provider-Specific Setup

### File Provider

Best for local development and testing:

```csharp
services.AddVaultWithFile(options =>
{
    options.SecretsFilePath = "secrets.json";
    options.ConfigFilePath = "config.json";
    options.WatchForChanges = true;  // Auto-reload on file changes
    options.CreateIfNotExists = true; // Create empty file if not exists
});
```

### Azure Key Vault Provider

For Azure-hosted applications:

```csharp
services.AddVaultWithAzureKeyVault(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    
    // Option 1: Managed Identity (recommended for Azure)
    options.UseManagedIdentity = true;
    
    // Option 2: Service Principal
    // options.TenantId = "your-tenant-id";
    // options.ClientId = "your-client-id";
});
```

### AWS Secrets Manager Provider

For AWS-hosted applications:

```csharp
services.AddVaultWithAwsSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.SecretPrefix = "prod/myapp/";
    
    // Uses default credential chain (instance profile, env vars, etc.)
});
```

### In-Memory Provider

For unit testing:

```csharp
services.AddVaultInMemory(options =>
{
    options.AddSecret("test-secret", "test-value");
    options.AddConfigValue("test-config", "config-value");
});
```

## Health and Readiness

When integrated with Kernel, Vault automatically provides health and readiness contributors:

- **`VaultHealthContributor`** - Reports operational status (liveness probe): provider availability, optional health check secret accessibility
- **`VaultReadinessContributor`** - Reports readiness (readiness probe): provider enablement, warmup completion

These contributors are picked up by Kernel's health aggregation model and exposed on the node's health endpoints. See [Health.md](docs/Health.md) for detailed behavior and configuration.

## Telemetry

All vault operations emit telemetry traces with:
- Provider name (resolved after fallback)
- Operation type (vault.secret.get, vault.config.get, etc.)
- Cache hit/miss status (explicit flag from cache layer)
- Duration
- Grid context (CorrelationId, NodeId, OperationId, etc.)
- Resilience metadata (retry count, circuit breaker state)

**Important**: Secret values are NEVER included in telemetry or logs.

**OpenTelemetry Integration**: To capture Vault traces, register the `honeydrunk.vault` activity source in your OpenTelemetry tracing configuration. See [Telemetry.md](docs/Telemetry.md) for details.

## Architecture

**Secret Resolution Flow:**
```
Application              ISecretStore/VaultClient     Provider
    ↓                        ↓                           ↓
SecretIdentifier → Check Cache → Cache Miss → Fetch from Backend → Return SecretValue
    ↓                        ↓                           ↓
"db-connection"    SecretCache       Azure Key Vault      SecretValue
                   (hit/miss)        AWS Secrets Manager  (value + version)
                                     File / InMemory
```

**Note:** In simple setups, `ISecretStore` is backed directly by a provider. In more advanced pipelines, `VaultClient` coordinates multiple providers, caching, and telemetry.

**Component Structure:**
```
HoneyDrunk.Vault (Core)
├── Abstractions/
│   ├── ISecretStore      # Primary secret access interface
│   ├── ISecretProvider   # Backend provider abstraction
│   ├── IConfigSource     # Raw configuration access
│   ├── IConfigProvider   # Typed configuration with defaults
│   └── IVaultClient      # Central orchestrator
├── Models/
│   ├── SecretIdentifier  # Name + optional version
│   ├── SecretValue       # Value + metadata
│   ├── SecretVersion     # Version info
│   └── VaultResult<T>    # Result pattern for Try* methods
├── Services/
│   ├── VaultClient       # Coordinates providers
│   ├── SecretCache       # In-memory caching with TTL
│   ├── CompositeSecretStore # Provider orchestration for secrets
│   └── CompositeConfigSource # Provider orchestration for config
├── Health/
│   ├── VaultHealthContributor
│   └── VaultReadinessContributor
├── Lifecycle/
│   └── VaultStartupHook  # Config validation & cache warming
└── Telemetry/
    └── VaultTelemetry    # Secure tracing (no secret values)

Providers (Separate Packages)
├── HoneyDrunk.Vault.Providers.File
├── HoneyDrunk.Vault.Providers.AzureKeyVault
├── HoneyDrunk.Vault.Providers.Aws
├── HoneyDrunk.Vault.Providers.InMemory
├── HoneyDrunk.Vault.Providers.Configuration
└── HoneyDrunk.Vault.EventGrid
```

## Documentation

For comprehensive documentation, see the [`/docs`](docs/) directory:

- **[FILE_GUIDE.md](docs/FILE_GUIDE.md)** - Complete file-by-file documentation
- **[Architecture.md](docs/Architecture.md)** - Dependency flow and integration patterns
- **[Abstractions.md](docs/Abstractions.md)** - Core interfaces and contracts
- **[Testing.md](docs/Testing.md)** - Test patterns and strategies
- **Provider Guides**: [File](docs/File.md), [Azure](docs/AzureKeyVault.md), [AWS](docs/Aws.md), [InMemory](docs/InMemory.md), [Configuration](docs/ConfigurationProvider.md)

## License

MIT License - see LICENSE file for details.
