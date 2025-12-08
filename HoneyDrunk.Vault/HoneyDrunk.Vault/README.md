# HoneyDrunk.Vault

Core secrets and configuration management library for HoneyDrunk.OS. This package provides the abstractions, caching, orchestration, telemetry, and Kernel lifecycle integration that all Vault providers plug into.

**This package contains no provider implementations.** It defines the Vault contract, runtime behavior, cache, telemetry, and lifecycle integration used by provider packages. Vault itself does not talk to Azure, AWS, files, or configuration—providers do.

## Overview

Vault gives applications a unified, Kernel-aware interface for secrets and configuration no matter where those values live. Providers supply the values; Vault handles resilience, caching, lifecycle behavior, and distributed tracing.

You'll need at least one provider package (File, Azure Key Vault, AWS, InMemory, or Configuration) to store and retrieve secrets.

**Key Abstractions:**
- **`ISecretStore`** - Primary interface for accessing secrets (inject this in your services)
- **`IConfigProvider`** - Typed configuration access with defaults
- **`IVaultClient`** - Combined orchestrator for secrets and config (use when you need both)
- **`SecretIdentifier`** - Immutable identifier (name + optional version)
- **`SecretValue`** - Immutable secret data + metadata

**Application code injects `ISecretStore` and `IConfigProvider`, not `IVaultClient`.** `IVaultClient` is useful when your service needs a unified façade for both secrets and config, but most apps won't need it.

## Features

- **Multiple provider support** (File, Azure, AWS, Configuration, InMemory)
- **Kernel lifecycle integration** (startup, health, readiness)
- **In-memory caching** with TTL and optional sliding expiration
- **Retry and circuit breaker** resilience policies
- **Grid context propagation** for tracing and correlation
- **Secure telemetry** (never logs secret values)
- **Pluggable provider model**

## Installation

```bash
dotnet add package HoneyDrunk.Vault
```

## Quick Start

### Consuming Secrets

```csharp
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;

public class MyService
{
    private readonly ISecretStore _store;

    public MyService(ISecretStore store)
    {
        _store = store;
    }

    public async Task<string> GetConnectionStringAsync()
    {
        var secret = await _store.GetSecretAsync(
            new SecretIdentifier("db-connection-string"));
        return secret.Value;
    }
}
```

### Registering Vault Inside a HoneyDrunk Node

**`AddVault(options => ...)` only exists when `HoneyDrunk.Kernel` is referenced.** This is the Kernel "builder" API, not a general DI API.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHoneyDrunkGrid(grid => { grid.StudioId = "my-studio"; })
    .AddHoneyDrunkNode(node => { node.NodeId = "my-service-node"; })
    .AddVault(options =>
    {
        options.Cache.Enabled = true;
        options.Cache.DefaultTtl = TimeSpan.FromMinutes(15);

        options.Resilience.RetryEnabled = true;
        options.Resilience.MaxRetryAttempts = 3;

        options.AddAzureKeyVaultProvider(akv =>
        {
            akv.VaultUri = new Uri("https://my-vault.vault.azure.net/");
            akv.UseManagedIdentity = true;
        });

        options.WarmupKeys.Add("db-connection-string");
        options.HealthCheckSecretKey = "health-check-secret";
    });

var app = builder.Build();
```

### Plain ASP.NET Core (No Kernel)

For apps that don't use Kernel, register a provider directly using provider-level DI extensions:

```csharp
builder.Services.AddVaultWithFile(o =>
{
    o.SecretsFilePath = "secrets.json";
});

builder.Services.AddVaultWithAzureKeyVault(o =>
{
    o.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    o.UseManagedIdentity = true;
});
```

## Architecture

```
HoneyDrunk.Vault (Core)
├── Abstractions
│   ├── ISecretStore / IConfigProvider
│   ├── ISecretProvider / IConfigSource
│   └── SecretIdentifier / SecretValue / SecretVersion
├── Services
│   ├── VaultClient
│   └── SecretCache
├── Lifecycle
│   └── VaultStartupHook
├── Health
│   ├── VaultHealthContributor
│   └── VaultReadinessContributor
└── Telemetry
    └── VaultTelemetry
```

## Key Interfaces

### ISecretStore
Main interface for secret access:
```csharp
public interface ISecretStore
{
    string ProviderName { get; }
    bool IsAvailable { get; }
    
    Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default);
    Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default);
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
```

### IConfigProvider
Typed configuration access:
```csharp
public interface IConfigProvider
{
    Task<string> GetValueAsync(string key, CancellationToken cancellationToken = default);
    Task<T> GetValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default);
    Task<string?> TryGetValueAsync(string key, CancellationToken cancellationToken = default);
}
```

## Health Checks

**Inside a HoneyDrunk node, Vault participates automatically in health and readiness checks through Kernel's aggregation model.** No extra wiring is required.

When using `AddVault` with Kernel, `VaultHealthContributor` and `VaultReadinessContributor` are automatically registered and surface vault status on Kernel's health aggregation endpoints.

## Telemetry

Vault emits OpenTelemetry activities for all operations. Traces include:
- Provider name
- Operation type (get, list, etc.)
- Cache hit/miss status
- Execution duration
- Grid correlation metadata

**Security Note**: Secret values are never logged or emitted in telemetry. Only secret names and provider metadata appear in traces.

## Configuration Options

Key configurable components include:
- **VaultCacheOptions** - TTL, max size, sliding expiration
- **VaultResilienceOptions** - Retry, circuit breaker, timeout
- **Provider registration** - Multiple providers with optional default
- **Warmup keys** - Preload critical secrets on startup
- **Health check secret** - Secret used for readiness checks

For full documentation of all configuration options, see the `/docs` directory.

### VaultOptions

```csharp
public class VaultOptions
{
    public Dictionary<string, ProviderRegistration> Providers { get; }
    public string? DefaultProvider { get; set; }
    public VaultCacheOptions Cache { get; set; }
    public VaultResilienceOptions Resilience { get; set; }
    public bool EnableTelemetry { get; set; }
    public List<string> WarmupKeys { get; }
    public string? HealthCheckSecretKey { get; set; }
}
```

### VaultCacheOptions

```csharp
public class VaultCacheOptions
{
    public bool Enabled { get; set; }
    public TimeSpan DefaultTtl { get; set; }
    public int MaxSize { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
}
```

### VaultResilienceOptions

```csharp
public class VaultResilienceOptions
{
    public bool RetryEnabled { get; set; }
    public int MaxRetryAttempts { get; set; }
    public TimeSpan RetryDelay { get; set; }
    public bool CircuitBreakerEnabled { get; set; }
    public int FailureThreshold { get; set; }
    public TimeSpan CircuitBreakDuration { get; set; }
    public TimeSpan Timeout { get; set; }
}
```

## Error Handling

Use `Get*` methods for required values and `TryGet*` for optional flows:

```csharp
try
{
    var secret = await _store.GetSecretAsync(id);
}
catch (SecretNotFoundException) { ... }
catch (VaultOperationException) { ... }

// For optional secrets
var result = await _store.TryGetSecretAsync(id);
if (result.IsSuccess)
{
    var secret = result.Value;
}
```

## Best Practices

1. **Inject `ISecretStore` or `IConfigProvider`, not concrete providers**
2. **Enable caching in production** - Improves performance and reduces provider load
3. **Use resilience settings for external stores** - Protect against transient failures
4. **Use warmup keys for latency-sensitive secrets** - Preload on startup
5. **Never log secret values** - Use secret names only in logs and telemetry
6. **Prefer `TryGetSecretAsync` for optional secrets and `GetSecretAsync` for required ones** - Keeps exception paths meaningful

## License

MIT License - see LICENSE file for details.

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault).
