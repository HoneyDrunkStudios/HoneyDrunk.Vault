# HoneyDrunk.Vault

Core secrets and configuration management library with unified provider abstraction.

## Overview

This is the **core package** that provides abstractions, orchestration, caching, and Kernel integration. You'll also need at least one provider package (File, Azure Key Vault, AWS, InMemory, or Configuration) to store and retrieve secrets.

**Key Abstractions:**
- **`ISecretStore`** - Primary interface for accessing secrets (inject this in your services)
- **`IConfigProvider`** - Typed configuration access with defaults
- **`IVaultClient`** - Central orchestrator combining secrets and configuration
- **`SecretIdentifier`** - Immutable record for identifying secrets (name + optional version)
- **`SecretValue`** - Immutable record containing secret value and metadata

## Features

- **Provider Abstraction**: Works with File, Azure Key Vault, AWS Secrets Manager, Configuration, and In-Memory providers
- **Kernel Integration**: Lifecycle hooks, health checks, and distributed telemetry
- **Caching**: In-memory caching with configurable TTL and size limits
- **Resilience**: Retry and circuit breaker policies for production reliability
- **Grid Context**: Distributed tracing and correlation via HoneyDrunk.Kernel
- **Secure Telemetry**: Traces operations without logging secret values

## Installation

```bash
dotnet add package HoneyDrunk.Vault
```

## Quick Start

### Basic Usage

```csharp
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;

// Inject ISecretStore
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
            new SecretIdentifier("db-connection-string"));
        return secret.Value;
    }
}
```

### Configuration

```csharp
var builder = WebApplication.CreateBuilder(args);

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

    // Add providers
    options.AddAzureKeyVaultProvider(akv =>
    {
        akv.VaultUri = new Uri("https://my-vault.vault.azure.net/");
        akv.UseManagedIdentity = true;
    });

    // Warm up critical secrets
    options.WarmupKeys.Add("database-connection-string");
    options.HealthCheckSecretKey = "health-check-secret";
});

var app = builder.Build();
```

## Architecture

```
HoneyDrunk.Vault (Core)
??? Abstractions
?   ??? ISecretStore      # Primary secret access
?   ??? ISecretProvider   # Provider abstraction
?   ??? IConfigSource     # Raw config access
?   ??? IConfigProvider   # Typed config access
??? Services
?   ??? VaultClient       # Orchestrates providers
?   ??? SecretCache       # In-memory caching
??? Health
?   ??? VaultHealthContributor
?   ??? VaultReadinessContributor
??? Lifecycle
?   ??? VaultStartupHook
??? Telemetry
    ??? VaultTelemetry
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

The library automatically provides health and readiness checks when integrated with HoneyDrunk.Kernel:

```csharp
// Health endpoint will include vault status
var contributors = app.Services.GetRequiredService<IEnumerable<IHealthContributor>>();
var results = await Task.WhenAll(contributors.Select(c => c.CheckHealthAsync()));
var allHealthy = results.All(r => r.status == HealthStatus.Healthy);
```

## Telemetry

All vault operations emit telemetry with:
- Provider name
- Operation type (get, list, etc.)
- Cache hit/miss status
- Execution duration
- Grid context (CorrelationId, NodeId, TenantId, etc.)

?? **Note**: Secret values are NEVER included in telemetry or logs.

## Configuration Options

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

```csharp
try
{
    var secret = await _secretStore.GetSecretAsync(identifier);
}
catch (SecretNotFoundException ex)
{
    // Handle missing secret
}
catch (VaultOperationException ex)
{
    // Handle vault operation errors
}
```

## Best Practices

1. **Always use dependency injection** - Let the DI container manage lifecycle
2. **Enable caching** - Improves performance and reduces provider load
3. **Configure resilience** - Protect against transient failures
4. **Use context-aware methods** - For distributed tracing correlation
5. **Never log secrets** - Use secret names only
6. **Warm up critical secrets** - Reduce latency on startup
7. **Configure health checks** - Enable monitoring

## License

MIT License - see LICENSE file for details.

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault).
