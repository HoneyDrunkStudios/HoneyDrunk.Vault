# HoneyDrunk.Vault

A secrets and configuration management library designed as a first-class Kernel-aware node for the HoneyDrunk.OS ecosystem.

## Overview

HoneyDrunk.Vault provides a unified abstraction for accessing secrets and configuration from multiple providers. It integrates seamlessly with HoneyDrunk.Kernel for lifecycle management, health reporting, and distributed telemetry.

## Features

- **Multiple Providers**: Support for File, Azure Key Vault, AWS Secrets Manager, and In-Memory providers
- **Kernel Integration**: Full integration with HoneyDrunk.Kernel for lifecycle, health, and telemetry
- **Caching**: Built-in caching with configurable TTL and size limits
- **Resilience**: Retry and circuit breaker policies for production reliability
- **Context-Aware**: Secrets can be scoped per Grid/Tenant/Node using Kernel's context model
- **Secure Telemetry**: Telemetry traces operations without leaking secret values

## Installation

```bash
# Core vault library
dotnet add package HoneyDrunk.Vault

# Provider packages (choose as needed)
dotnet add package HoneyDrunk.Vault.Providers.File
dotnet add package HoneyDrunk.Vault.Providers.AzureKeyVault
dotnet add package HoneyDrunk.Vault.Providers.Aws
dotnet add package HoneyDrunk.Vault.Providers.InMemory
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

Your `secrets/dev-secrets.json` file:
```json
{
  "database-connection-string": "Server=localhost;Database=myapp;User Id=dev;Password=devpass;",
  "api-key": "dev-api-key-12345"
}
```

### Switching to Azure Key Vault in Production

The beauty of Vault is that you can switch providers using only configuration:

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
services.AddVaultWithInMemory(options =>
{
    options.SetSecret("test-secret", "test-value");
    options.SetConfig("test-config", "config-value");
});
```

## Health and Readiness

When integrated with Kernel, Vault automatically provides health and readiness contributors:

```csharp
// Health endpoint will include vault status
app.MapGet("/health", async (IEnumerable<IHealthContributor> contributors) =>
{
    var results = await Task.WhenAll(
        contributors.Select(c => c.CheckHealthAsync()));
    
    var allHealthy = results.All(r => r.status == HealthStatus.Healthy);
    return allHealthy ? Results.Ok() : Results.StatusCode(503);
});
```

## Telemetry

All vault operations emit telemetry traces with:
- Provider name
- Operation type (get, list, etc.)
- Cache hit/miss status
- Duration
- Grid context (CorrelationId, NodeId, TenantId, etc.)

**Important**: Secret values are NEVER included in telemetry or logs.

## Architecture

```
HoneyDrunk.Vault (Core)
├── Abstractions
│   ├── ISecretStore      # Primary secret access
│   ├── ISecretProvider   # Provider abstraction
│   ├── IConfigSource     # Raw config access
│   └── IConfigProvider   # Typed config access
├── Services
│   ├── VaultClient       # Orchestrates providers
│   └── SecretCache       # In-memory caching
├── Health
│   ├── VaultHealthContributor
│   └── VaultReadinessContributor
├── Lifecycle
│   └── VaultStartupHook
└── Telemetry
    └── VaultTelemetry

Providers
├── HoneyDrunk.Vault.Providers.File
├── HoneyDrunk.Vault.Providers.AzureKeyVault
├── HoneyDrunk.Vault.Providers.Aws
├── HoneyDrunk.Vault.Providers.InMemory
└── HoneyDrunk.Vault.Providers.Configuration
```

## License

MIT License - see LICENSE file for details.
