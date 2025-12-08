# 📦 HoneyDrunk.Vault - Complete File Guide

## Overview

**Think of this library as a secure lockbox for your application**

Just like how a bank vault stores valuables with multiple layers of security and access control, this library provides unified access to secrets and configuration from multiple providers. It abstracts away the complexity of different secret stores (Azure Key Vault, AWS Secrets Manager, File-based, In-Memory) with caching, resilience policies, and Kernel-aware lifecycle integration.

**Key Concepts:**
- **SecretIdentifier**: The unique key to locate a secret (like a safety deposit box number)
- **SecretValue**: The retrieved secret with metadata (like the contents plus a receipt)
- **ISecretStore**: Primary interface for secret access (like the vault teller window)
- **IConfigProvider**: Typed configuration access (like a settings dashboard)
- **VaultClient**: Central orchestrator that coordinates providers (like the vault manager)
- **SecretCache**: In-memory caching layer (like a quick-access drawer)
- **Provider**: Backend-specific implementation (File, Azure Key Vault, AWS, InMemory)

---

## 📚 Documentation Structure

This guide is organized into focused documents by domain:

### 🏛️ Architecture

| Document | Description |
|----------|-------------|
| [Architecture.md](Architecture.md) | **Dependency flow, layer responsibilities, and integration patterns** |

### 🔷 HoneyDrunk.Vault (Core)

| Domain | Document | Description |
|--------|----------|-------------|
| 📋 **Abstractions** | [Abstractions.md](Abstractions.md) | Core contracts and vault-agnostic types (ISecretStore, ISecretProvider, IConfigSource, IConfigProvider, IVaultClient) |
| 🔧 **Models** | [Models.md](Models.md) | Building blocks (SecretIdentifier, SecretValue, SecretVersion, VaultResult, VaultScope) |
| ⚙️ **Configuration** | [Configuration.md](Configuration.md) | Settings (VaultOptions, VaultCacheOptions, VaultResilienceOptions, provider registrations) |
| 🔄 **Services** | [Services.md](Services.md) | Core services (VaultClient orchestrator, SecretCache, ConfigSourceAdapter) |
| ❤️ **Health** | [Health.md](Health.md) | Health monitoring (VaultHealthContributor, VaultReadinessContributor) |
| 🚀 **Lifecycle** | [Lifecycle.md](Lifecycle.md) | Startup integration (VaultStartupHook, cache warming) |
| 📈 **Telemetry** | [Telemetry.md](Telemetry.md) | Observability (VaultTelemetry, activity tracing, secure logging) |
| ❌ **Exceptions** | [Exceptions.md](Exceptions.md) | Error handling (SecretNotFoundException, ConfigurationNotFoundException, VaultOperationException) |
| 🔌 **DI** | [DependencyInjection.md](DependencyInjection.md) | Service registration (VaultServiceCollectionExtensions, HoneyDrunkBuilderExtensions) |

### 🔸 Provider Implementations

| Document | Description |
|----------|-------------|
| [File.md](File.md) | File-based provider for development (JSON storage, file watching, optional encryption) |
| [AzureKeyVault.md](AzureKeyVault.md) | Azure Key Vault provider (managed identity, enterprise-grade security) |
| [Aws.md](Aws.md) | AWS Secrets Manager provider (IAM roles, instance profiles, cross-region) |
| [InMemory.md](InMemory.md) | In-memory provider for testing (no external dependencies, runtime updates) |
| [ConfigurationProvider.md](ConfigurationProvider.md) | IConfiguration-based provider (bridges ASP.NET Core configuration) |

### 🧪 Testing

| Document | Description |
|----------|-------------|
| [Testing.md](Testing.md) | Test patterns, InMemory provider usage, mocking strategies |

---

## 🔷 Quick Start

### Basic Concepts

**Secret Resolution Flow:**
```
Application                  VaultClient                  Provider
    ↓                            ↓                            ↓
SecretIdentifier → Check Cache → Cache Miss → Fetch from Backend → Return SecretValue
    ↓                            ↓                            ↓
"db-connection"     SecretCache    Azure Key Vault      SecretValue
                    (hit/miss)     AWS Secrets Manager  (value + version)
                                   File / InMemory
```

**Provider Priority:**
```
VaultOptions
  ├─ DefaultProvider: "azure-keyvault"
  └─ Providers: [azure-keyvault, file]
       ↓
First available provider is used
       ↓
If unavailable, next in priority order
```

**Kernel Integration:**
```
HoneyDrunk.Kernel
  ├─ VaultStartupHook → Validates config, warms cache
  ├─ VaultHealthContributor → Reports vault health status
  └─ VaultReadinessContributor → Reports readiness for traffic
       ↓
All integrated via IHealthContributor / IReadinessContributor / IStartupHook
```

### Installation

```bash
# Core abstractions and orchestrator
dotnet add package HoneyDrunk.Vault

# Choose provider implementation
dotnet add package HoneyDrunk.Vault.Providers.AzureKeyVault
# OR
dotnet add package HoneyDrunk.Vault.Providers.Aws
# OR
dotnet add package HoneyDrunk.Vault.Providers.File
# OR (for testing)
dotnet add package HoneyDrunk.Vault.Providers.InMemory
```

### Basic Usage

```csharp
// Program.cs - Setup with File Provider (Development)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVaultWithFile(options =>
{
    options.SecretsFilePath = "secrets/dev-secrets.json";
    options.ConfigFilePath = "secrets/dev-config.json";
    options.WatchForChanges = true;
});

var app = builder.Build();
app.Run();
```

```csharp
// Program.cs - Setup with Azure Key Vault (Production)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVaultWithAzureKeyVault(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    options.UseManagedIdentity = true;
});

var app = builder.Build();
app.Run();
```

```csharp
// Using with HoneyDrunk.Kernel (Full Integration)
var builder = WebApplication.CreateBuilder(args);

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

        // Add provider
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
app.Run();
```

```csharp
// Retrieving Secrets
public class DatabaseService(ISecretStore secretStore)
{
    public async Task<string> GetConnectionStringAsync(CancellationToken ct)
    {
        var secret = await secretStore.GetSecretAsync(
            new SecretIdentifier("db-connection-string"),
            ct);
        return secret.Value;
    }

    public async Task<string?> TryGetApiKeyAsync(CancellationToken ct)
    {
        var result = await secretStore.TryGetSecretAsync(
            new SecretIdentifier("api-key"),
            ct);
        
        return result.IsSuccess ? result.Value!.Value : null;
    }
}
```

```csharp
// Retrieving Configuration
public class FeatureService(IConfigProvider configProvider)
{
    public async Task<int> GetMaxConnectionsAsync(CancellationToken ct)
    {
        return await configProvider.GetValueAsync<int>(
            "max-connections",
            defaultValue: 100,
            ct);
    }

    public async Task<bool> IsFeatureEnabledAsync(string feature, CancellationToken ct)
    {
        return await configProvider.GetValueAsync<bool>(
            $"feature:{feature}",
            defaultValue: false,
            ct);
    }
}
```

### Using VaultClient (Unified Access)

```csharp
public class MyService(IVaultClient vaultClient)
{
    public async Task DoWorkAsync(CancellationToken ct)
    {
        // Get secret
        var secret = await vaultClient.GetSecretAsync(
            new SecretIdentifier("api-key"),
            ct);
        
        // Get configuration
        var timeout = await vaultClient.GetConfigValueAsync<int>(
            "operation-timeout",
            ct);
        
        // Try with default
        var retries = await vaultClient.TryGetConfigValueAsync<int>(
            "max-retries",
            defaultValue: 3,
            ct);
    }
}
```

---

## 🔷 Design Philosophy

### Core Principles

1. **Provider-agnostic** - Swap secret backends without changing business logic
2. **Kernel-aware** - First-class integration with HoneyDrunk.Kernel for lifecycle, health, and telemetry
3. **Caching by default** - Configurable TTL and size limits reduce backend calls
4. **Resilience built-in** - Retry policies and circuit breakers for production reliability
5. **Secure telemetry** - Traces operations without leaking secret values
6. **Context-aware** - Scoped secrets per environment/tenant/node

### Why These Patterns?

**SecretIdentifier Pattern:**
- Immutable record with name and optional version
- Enables versioned secret access
- Clear type safety over raw strings

**VaultResult Pattern:**
- Success/failure monad for Try* methods
- Avoids exceptions for expected "not found" cases
- Provides structured error messages

**Provider Abstraction:**
- `ISecretStore` for primary secret access
- `ISecretProvider` for backend-specific implementations
- `IConfigSource` / `IConfigProvider` for configuration values
- Adapters bridge legacy interfaces to new contracts

**Caching Layer:**
- Reduces calls to remote secret stores
- Configurable TTL prevents stale secrets
- Size limits prevent memory exhaustion
- Sliding expiration for frequently accessed secrets

**Kernel Integration:**
- `IStartupHook` validates configuration and warms cache
- `IHealthContributor` reports vault health to Kubernetes probes
- `IReadinessContributor` ensures vault is ready before accepting traffic
- Grid context propagation for distributed tracing

### Current Feature Set

**Core Infrastructure:**
- ✅ **VaultClient** - Central orchestrator for secrets and configuration
- ✅ **SecretCache** - In-memory caching with configurable TTL
- ✅ **VaultOptions** - Fluent configuration with provider registration

**Resilience:**
- ✅ **Retry policies** - Configurable retry with exponential backoff
- ✅ **Circuit breaker** - Prevents cascading failures
- ✅ **Timeout** - Operation timeout limits

**Health & Lifecycle:**
- ✅ **VaultHealthContributor** - Reports vault operational status
- ✅ **VaultReadinessContributor** - Reports vault readiness
- ✅ **VaultStartupHook** - Validates config and warms cache

**Telemetry:**
- ✅ **VaultTelemetry** - Activity tracing for all operations
- ✅ **Secure logging** - Logs operation metadata, never secret values
- ✅ **Grid context** - Propagates correlation through distributed calls

**Providers:**
- ✅ **File** - JSON file storage with file watching
- ✅ **Azure Key Vault** - Managed identity, service principal
- ✅ **AWS Secrets Manager** - IAM roles, instance profiles
- ✅ **InMemory** - Unit testing and integration testing
- ✅ **Configuration** - Bridges IConfiguration to vault abstractions

---

## 📦 Project Structure

```
HoneyDrunk.Vault/
├── HoneyDrunk.Vault/                  # Core abstractions and orchestrator
│   ├── Abstractions/                  # Contracts and interfaces
│   │   ├── IConfigProvider.cs         # Typed configuration access
│   │   ├── IConfigSource.cs           # Raw configuration source
│   │   ├── ISecretProvider.cs         # Backend-specific provider
│   │   ├── ISecretStore.cs            # Primary secret access
│   │   └── IVaultClient.cs            # Unified orchestrator contract
│   ├── Configuration/                 # Settings and options
│   │   ├── AwsSecretsManagerProviderOptions.cs
│   │   ├── AzureKeyVaultProviderOptions.cs
│   │   ├── FileProviderOptions.cs
│   │   ├── InMemoryProviderOptions.cs
│   │   ├── ProviderRegistration.cs    # Provider configuration
│   │   ├── ProviderType.cs            # Provider type enumeration
│   │   ├── VaultCacheOptions.cs       # Cache configuration
│   │   ├── VaultOptions.cs            # Main vault options
│   │   └── VaultResilienceOptions.cs  # Retry/circuit breaker config
│   ├── Exceptions/                    # Custom exceptions
│   │   ├── ConfigurationNotFoundException.cs
│   │   ├── SecretNotFoundException.cs
│   │   └── VaultOperationException.cs
│   ├── Extensions/                    # DI extensions
│   │   ├── HoneyDrunkBuilderExtensions.cs # Kernel integration
│   │   └── VaultServiceCollectionExtensions.cs # IServiceCollection
│   ├── Health/                        # Health monitoring
│   │   ├── VaultHealthContributor.cs  # Liveness health check
│   │   └── VaultReadinessContributor.cs # Readiness check
│   ├── Lifecycle/                     # Startup integration
│   │   └── VaultStartupHook.cs        # Validates and warms cache
│   ├── Models/                        # Domain models
│   │   ├── SecretIdentifier.cs        # Secret lookup key
│   │   ├── SecretValue.cs             # Retrieved secret
│   │   ├── SecretVersion.cs           # Version metadata
│   │   ├── VaultResult.cs             # Factory methods
│   │   ├── VaultResult{T}.cs          # Success/failure result
│   │   └── VaultScope.cs              # Environment/tenant scope
│   ├── Services/                      # Core services
│   │   ├── ConfigSourceAdapter.cs     # IConfigSource → IConfigProvider
│   │   ├── SecretCache.cs             # In-memory caching
│   │   └── VaultClient.cs             # Central orchestrator
│   └── Telemetry/                     # Observability
│       ├── VaultTelemetry.cs          # Activity tracing
│       └── VaultTelemetryTags.cs      # Standard tag names
│
├── HoneyDrunk.Vault.Providers.File/   # File-based provider
│   ├── Configuration/
│   │   └── FileVaultOptions.cs        # File provider options
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # AddVaultWithFile
│   └── Services/
│       ├── FileConfigSource.cs        # File-based config
│       └── FileSecretStore.cs         # File-based secrets
│
├── HoneyDrunk.Vault.Providers.AzureKeyVault/ # Azure Key Vault provider
│   ├── Configuration/
│   │   └── AzureKeyVaultOptions.cs    # Azure KV options
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # AddVaultWithAzureKeyVault
│   └── Services/
│       ├── AzureKeyVaultConfigSource.cs # Azure KV config
│       └── AzureKeyVaultSecretStore.cs  # Azure KV secrets
│
├── HoneyDrunk.Vault.Providers.Aws/    # AWS Secrets Manager provider
│   ├── Configuration/
│   │   └── AwsSecretsManagerOptions.cs # AWS options
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # AddVaultWithAwsSecretsManager
│   └── Services/
│       └── AwsSecretsManagerSecretStore.cs # AWS secrets
│
├── HoneyDrunk.Vault.Providers.InMemory/ # In-memory provider (testing)
│   ├── Configuration/
│   │   └── InMemoryVaultOptions.cs    # InMemory options
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # AddVaultWithInMemory
│   └── Services/
│       ├── InMemoryConfigSource.cs    # InMemory config
│       └── InMemorySecretStore.cs     # InMemory secrets
│
├── HoneyDrunk.Vault.Providers.Configuration/ # IConfiguration provider
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # AddVaultWithConfiguration
│   └── Services/
│       ├── ConfigurationConfigSource.cs # IConfiguration config
│       └── ConfigurationSecretStore.cs  # IConfiguration secrets
│
└── HoneyDrunk.Vault.Tests/            # Unit and integration tests
    ├── Configuration/                 # Configuration tests
    ├── Services/
    │   ├── InMemorySecretStoreTests.cs
    │   └── SecretCacheTests.cs
    └── Telemetry/                     # Telemetry tests
```

---

## 🆕 Key Features

### Caching
- **SecretCache** - In-memory cache with configurable TTL
- Size limits prevent memory exhaustion
- Sliding expiration for frequently accessed secrets
- Cache warmup at startup for critical secrets

### Resilience
- **Retry policies** - Exponential backoff with jitter
- **Circuit breaker** - Open after threshold failures, auto-reset
- **Timeout** - Per-operation timeout limits
- Configurable via `VaultResilienceOptions`

### Health Monitoring
- **VaultHealthContributor** - Reports to Kubernetes liveness probes
- **VaultReadinessContributor** - Reports to Kubernetes readiness probes
- Optionally verifies health check secret accessibility
- Integrates with Kernel's health aggregation

### Telemetry
- **Activity tracing** - Creates spans for all vault operations
- **Secure logging** - Never logs secret values
- **Cache hit/miss** - Tracks cache effectiveness
- **Grid context** - Propagates correlation IDs

### Provider Features

| Feature | File | Azure KV | AWS | InMemory | Configuration |
|---------|------|----------|-----|----------|---------------|
| Secrets | ✅ | ✅ | ✅ | ✅ | ✅ |
| Config | ✅ | ✅ | ❌ | ✅ | ✅ |
| Versioning | ❌ | ✅ | ✅ | ❌ | ❌ |
| File Watch | ✅ | ❌ | ❌ | ❌ | ❌ |
| Encryption | ✅ | ✅ | ✅ | ❌ | ❌ |
| Managed Identity | ❌ | ✅ | ✅ | ❌ | ❌ |
| Runtime Update | ❌ | ❌ | ❌ | ✅ | ❌ |
| Production Use | ❌ | ✅ | ✅ | ❌ | ⚠️ |

---

## 🔗 Relationships

### Upstream Dependencies

- **HoneyDrunk.Kernel.Abstractions** - IHealthContributor, IReadinessContributor, IStartupHook, IGridContext
- **Microsoft.Extensions.Logging.Abstractions** - Logging infrastructure
- **Microsoft.Extensions.Caching.Memory** - In-memory caching
- **Microsoft.Extensions.Options** - Options pattern
- **Azure.Security.KeyVault.Secrets** - Azure Key Vault SDK (provider-specific)
- **AWSSDK.SecretsManager** - AWS Secrets Manager SDK (provider-specific)

### Downstream Consumers

Applications using HoneyDrunk.Vault:

- **Web APIs** - Database connection strings, API keys
- **Background Workers** - Service credentials, encryption keys
- **Integration Services** - Third-party API tokens
- **Multi-tenant Applications** - Tenant-specific secrets

---

## 📖 Additional Resources

### Official Documentation
- [README.md](../README.md) - Project overview and quick start
- [CHANGELOG.md](../HoneyDrunk.Vault/HoneyDrunk.Vault/CHANGELOG.md) - Version history and migration guides

### Related Projects
- [HoneyDrunk.Kernel](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) - Core Grid primitives
- [HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards) - Analyzers and conventions

### External References
- [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/) - Azure secret management
- [AWS Secrets Manager](https://docs.aws.amazon.com/secretsmanager/) - AWS secret management
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/) - Observability

---

## 💡 Motto

**"Your secrets, safely abstracted."** - Focus on business logic, not secret management infrastructure.

---

*Last Updated: 2025-12-08*  
*Target Framework: .NET 10.0*
