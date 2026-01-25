# 📦 HoneyDrunk.Vault - Complete File Guide

## Overview

**Think of this Node as the Grid's secure lockbox**

Just like how a bank vault stores valuables with multiple layers of security and access control, Vault provides unified access to secrets and configuration from multiple providers. It abstracts away the complexity of different secret stores (Azure Key Vault, AWS Secrets Manager, File-based, In-Memory) with caching, resilience policies, and Kernel-aware lifecycle integration.

---

## Node Placement

| Attribute | Value |
|-----------|-------|
| **Sector** | Core |
| **Cluster** | Security |
| **Slot** | Provider |
| **Exported Contracts** | `ISecretStore`, `IConfigProvider` |
| **Package** | `HoneyDrunk.Vault` |
| **Consumes** | `HoneyDrunk.Kernel` (runtime contracts) |
| **Compile-time** | `HoneyDrunk.Kernel.Abstractions` in Vault packages; full `HoneyDrunk.Kernel` only at app/node level |

**Vault is the Grid's canonical source of secrets and configuration.** Other Nodes consume it via `ISecretStore` and `IConfigProvider`—never provider SDKs directly. Provider packages (Azure Key Vault, AWS, File, InMemory, Configuration) are child packages that plug into Vault's provider slot via shared abstractions.

---

## Key Concepts

**Models:**
- **SecretIdentifier** — The unique key to locate a secret (name + optional version)
- **SecretValue** — The retrieved secret with metadata (value + version)
- **VaultResult\<T\>** — Success/failure monad for Try* methods

**Exported Contracts** (cross-Node consumption):
- **ISecretStore** — Primary interface for secret access. Inject this in application code.
- **IConfigProvider** — Typed configuration access with defaults. Inject this for config values.

**Internal Contracts** (Vault + provider packages only):
- **ISecretProvider** — Backend-specific provider implementation. Provider packages reference this; "internal" means Vault ecosystem, not same-assembly.
- **IConfigSource** — Raw configuration source (wrapped by IConfigProvider). Provider packages reference this.
- **IVaultClient** — Internal orchestrator combining secrets + config. Reserved for infrastructure and advanced orchestration only.

> **Guidance:** Application and business code should depend on `ISecretStore` and `IConfigProvider`. Do not cargo-cult `IVaultClient` everywhere—it is for infrastructure code that needs unified access to both secrets and configuration.

**Runtime Components:**
- **VaultClient** — Internal orchestrator that coordinates providers
- **SecretCache** — In-memory caching layer with TTL
- **Provider** — Backend-specific implementation (File, Azure Key Vault, AWS, InMemory)

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
| 📋 **Abstractions** | [Abstractions.md](Abstractions.md) | **Exported:** ISecretStore, IConfigProvider · **Internal:** ISecretProvider, IConfigSource, IVaultClient |
| 🔧 **Models** | [Models.md](Models.md) | Building blocks (SecretIdentifier, SecretValue, SecretVersion, VaultResult, VaultScope) |
| ⚙️ **Configuration** | [Configuration.md](Configuration.md) | Core: VaultOptions, VaultCacheOptions, VaultResilienceOptions, ProviderRegistration |
| 🔄 **Services** | [Services.md](Services.md) | Core services (VaultClient orchestrator, SecretCache, CompositeSecretStore, CompositeConfigSource) |
| ❤️ **Health** | [Health.md](Health.md) | Health monitoring (VaultHealthContributor, VaultReadinessContributor) |
| 🚀 **Lifecycle** | [Lifecycle.md](Lifecycle.md) | Startup integration (VaultStartupHook, cache warming) |
| 📈 **Telemetry** | [Telemetry.md](Telemetry.md) | Observability (VaultTelemetry, activity tracing, secure logging) |
| ❌ **Exceptions** | [Exceptions.md](Exceptions.md) | Error handling (SecretNotFoundException, ConfigurationNotFoundException, VaultOperationException) |
| 🔌 **DI** | [DependencyInjection.md](DependencyInjection.md) | Service registration (VaultServiceCollectionExtensions, HoneyDrunkBuilderExtensions) |

### 🔸 Provider Slot Implementations

Vault is a **provider slot Node**—provider packages are child packages that implement `ISecretProvider` / `IConfigSource` and register themselves via `VaultOptions.Providers`.

| Document | Description |
|----------|-------------|
| [File.md](File.md) | File-based provider for development (JSON storage, file watching) |
| [AzureKeyVault.md](AzureKeyVault.md) | Azure Key Vault provider (managed identity, enterprise-grade security) |
| [Aws.md](Aws.md) | AWS Secrets Manager provider (IAM roles, instance profiles) |
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

# Choose provider implementation (one or more)
dotnet add package HoneyDrunk.Vault.Providers.AzureKeyVault   # Production (Azure)
dotnet add package HoneyDrunk.Vault.Providers.Aws             # Production (AWS)
dotnet add package HoneyDrunk.Vault.Providers.File            # Development
dotnet add package HoneyDrunk.Vault.Providers.InMemory        # Testing
```

### Grid-Integrated Usage (Recommended)

`AddVault()` is an extension on the HoneyDrunk node builder—Vault's lifecycle hooks plug into Kernel's `IStartupHook`, `IHealthContributor`, and `IReadinessContributor` contracts.

```csharp
// Program.cs - Full Grid Integration with Azure Key Vault
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHoneyDrunkGrid(grid =>
    {
        grid.StudioId = "my-studio";
    })
    .AddHoneyDrunkNode(node =>
    {
        node.NodeId = "my-service-node";
    })
    .AddVault(vault =>
    {
        // Configure caching
        vault.Cache.Enabled = true;
        vault.Cache.DefaultTtl = TimeSpan.FromMinutes(15);
        vault.Cache.MaxSize = 1000;

        // Configure resilience
        vault.Resilience.RetryEnabled = true;
        vault.Resilience.MaxRetryAttempts = 3;
        vault.Resilience.CircuitBreakerEnabled = true;

        // Warm up critical secrets at startup
        vault.WarmupKeys.Add("database-connection-string");
        vault.HealthCheckSecretKey = "health-check-secret";
    })
    .AddVaultWithAzureKeyVault(akv =>
    {
        akv.VaultUri = new Uri("https://my-vault.vault.azure.net/");
        akv.UseManagedIdentity = true;
    });

var app = builder.Build();
app.Run();
```

### Off-Grid Usage (Development / Early Adoption)

For development or standalone usage without full Kernel integration:

```csharp
// Program.cs - File Provider (Development)
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
// Program.cs - Azure Key Vault (Production, Off-Grid)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVaultWithAzureKeyVault(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    options.UseManagedIdentity = true;
});

var app = builder.Build();
app.Run();
``` ISecretStore)

```csharp
// Business logic should inject ISecretStore, not IVaultClient
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

### Consuming Configuration (Inject IConfigProvider)

```csharp
// Business logic should inject IConfigProvider for typed config access
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

### Using VaultClient (Advanced / Infrastructure Code Only)

> **Note:** `IVaultClient` is an internal orchestrator. Business logic should inject `ISecretStore` or `IConfigProvider` directly. Use `IVaultClient` only for infrastructure code that needs unified access to both secrets and configuration.

```csharp
// Infrastructure code only - prefer ISecretStore / IConfigProvider in business logic
public class InfrastructureHelper(IVaultClient vaultClient)
{
    public async Task InitializeAsync(CancellationToken ct)
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

**Provider Abstraction (Slot Model):**
- Core Vault does not know about Azure/AWS/File specifics
- Provider packages implement `ISecretProvider` / `IConfigSource` and register via `VaultOptions.Providers`
- Vault is a **provider slot Node**; provider packages are child packages that plug into the slot
- `ISecretStore` and `IConfigProvider` are **exported contracts** for cross-Node consumption
- `ISecretProvider`, `IConfigSource`, `IVaultClient` are **internal** to Vault and its providers

**Caching Layer:**
- Reduces calls to remote secret stores
- Configurable TTL prevents stale secrets
- Size limits prevent memory exhaustion
- Sliding expiration for frequently accessed secrets

**Kernel Integration (Implements Kernel Contracts):**
- `VaultStartupHook` implements `IStartupHook` — validates configuration and warms cache
- `VaultHealthContributor` implements `IHealthContributor` — reports vault health to Kubernetes probes
- `VaultReadinessContributor` implements `IReadinessContributor` — ensures vault is ready before accepting traffic
- Grid context propagation via `IGridContext` for distributed tracing
- Vault depends on `HoneyDrunk.Kernel.Abstractions` at compile time; full `HoneyDrunk.Kernel` referenced only at app/node layer
- From the Grid's perspective, Vault consumes the Kernel node and is wired to its runtime contracts (context, lifecycle, health, telemetry, agents, etc.)

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
│   │   ├── IConfigProvider.cs         # EXPORTED: Typed configuration access
│   │   ├── IConfigSource.cs           # INTERNAL: Raw configuration source
│   │   ├── ISecretProvider.cs         # INTERNAL: Backend-specific provider
│   │   ├── ISecretStore.cs            # EXPORTED: Primary secret access
│   │   └── IVaultClient.cs            # INTERNAL: Unified orchestrator
│   ├── Configuration/                 # Core settings (provider-agnostic)
│   │   ├── VaultOptions.cs            # Main vault options
│   │   ├── VaultCacheOptions.cs       # Cache configuration
│   │   ├── VaultResilienceOptions.cs  # Retry/circuit breaker config
│   │   ├── ProviderRegistration.cs    # Generic provider registration
│   │   └── ProviderType.cs            # Provider type enumeration
│   ├── Exceptions/                    # Custom exceptions
│   │   ├── ConfigurationNotFoundException.cs
│   │   ├── SecretNotFoundException.cs
│   │   └── VaultOperationException.cs
│   ├── Extensions/                    # DI extensions
│   │   ├── HoneyDrunkBuilderExtensions.cs # AddVault() on IHoneyDrunkBuilder
│   │   └── VaultServiceCollectionExtensions.cs # AddVaultCore()
│   ├── Health/                        # Implements Kernel health contracts
│   │   ├── VaultHealthContributor.cs  # Implements IHealthContributor
│   │   └── VaultReadinessContributor.cs # Implements IReadinessContributor
│   ├── Lifecycle/                     # Implements Kernel lifecycle contracts
│   │   └── VaultStartupHook.cs        # Implements IStartupHook
│   ├── Models/                        # Domain models
│   │   ├── SecretIdentifier.cs        # Secret lookup key
│   │   ├── SecretValue.cs             # Retrieved secret
│   │   ├── SecretVersion.cs           # Version metadata
│   │   ├── VaultResult.cs             # Factory methods
│   │   ├── VaultResult{T}.cs          # Success/failure result
│   │   └── VaultScope.cs              # Environment/tenant scope
│   ├── Services/                      # Internal services
│   │   ├── CompositeConfigSource.cs   # Provider orchestration for config
│   │   ├── CompositeSecretStore.cs    # Provider orchestration for secrets
│   │   ├── SecretCache.cs             # In-memory caching with TTL
│   │   └── VaultClient.cs             # Internal orchestrator
│   └── Telemetry/                     # Observability (Pulse-compatible)
│       ├── VaultTelemetry.cs          # ActivitySource spans
│       └── VaultTelemetryTags.cs      # Standard tag names
│
├── HoneyDrunk.Vault.Providers.File/   # Provider slot: File-based
│   ├── Configuration/
│   │   └── FileVaultOptions.cs        # Provider-specific options
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # AddVaultWithFile()
│   └── Services/
│       ├── FileConfigSource.cs        # Implements IConfigSource
│       └── FileSecretStore.cs         # Implements ISecretStore + ISecretProvider (dual role for off-grid usage)
│
├── HoneyDrunk.Vault.Providers.AzureKeyVault/ # Provider slot: Azure Key Vault
│   ├── Configuration/
│   │   └── AzureKeyVaultOptions.cs    # Provider-specific options
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # AddVaultWithAzureKeyVault()
│   └── Services/
│       ├── AzureKeyVaultConfigSource.cs # Implements IConfigSource
│       └── AzureKeyVaultSecretStore.cs  # Implements ISecretStore + ISecretProvider
│
├── HoneyDrunk.Vault.Providers.Aws/    # Provider slot: AWS Secrets Manager
│   ├── Configuration/
│   │   └── AwsSecretsManagerOptions.cs # Provider-specific options
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # AddVaultWithAwsSecretsManager()
│   └── Services/
│       └── AwsSecretsManagerSecretStore.cs # Implements ISecretStore + ISecretProvider
│
├── HoneyDrunk.Vault.Providers.InMemory/ # Provider slot: In-memory (testing)
│   ├── Configuration/
│   │   └── InMemoryVaultOptions.cs    # Provider-specific options
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # AddVaultInMemory()
│   └── Services/
│       ├── InMemoryConfigSource.cs    # Implements IConfigSource
│       └── InMemorySecretStore.cs     # Implements ISecretStore + ISecretProvider
│
├── HoneyDrunk.Vault.Providers.Configuration/ # Provider slot: IConfiguration bridge
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs # AddVaultWithConfiguration()
│   └── Services/
│       ├── ConfigurationConfigSource.cs # Implements IConfigSource
│       └── ConfigurationSecretStore.cs  # Implements ISecretStore + ISecretProvider
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

### Health Monitoring (Implements Kernel Contracts)
- **VaultHealthContributor** implements `IHealthContributor` — reports to Kubernetes liveness probes
- **VaultReadinessContributor** implements `IReadinessContributor` — reports to Kubernetes readiness probes
- Optionally verifies health check secret accessibility
- Integrates with Kernel's health aggregation pipeline

### Telemetry (Pulse-Compatible)
- **ActivitySource spans** — `HoneyDrunk.Vault` source that Pulse can ingest
- **Secure logging** — Never logs secret values, only operation metadata
- **Cache hit/miss** — Tracks cache effectiveness for optimization
- **Grid context** — Propagates correlation IDs via `IGridContext`
- Logs and metrics designed to be Pulse-friendly for Grid observability

### Provider Features

| Feature | File | Azure KV | AWS | InMemory | Configuration |
|---------|------|----------|-----|----------|---------------|
| Secrets | ✅ | ✅ | ✅ | ✅ | ✅ |
| Config | ✅ | ✅ | ❌ | ✅ | ✅ |
| Versioning | ❌ | ✅ | ✅ | ❌ | ❌ |
| File Watch | ✅ | ❌ | ❌ | ❌ | ❌ |
| Encryption | ⚠️ | ✅ | ✅ | ❌ | ❌ |
| Managed Identity | ❌ | ✅ | ✅* | ❌ | ❌ |
| Runtime Update | ❌ | ❌ | ❌ | ✅ | ❌ |
| Production Use | ❌ | ✅ | ✅ | ❌ | ⚠️ |

*AWS uses IAM roles/instance profiles rather than Azure-style managed identity.

⚠️ File encryption is optional—plain JSON by default with opt-in encryption via environment variable or key file.

---

## 🔗 Relationships

### Upstream Dependencies

- **HoneyDrunk.Kernel.Abstractions** — `IHealthContributor`, `IReadinessContributor`, `IStartupHook`, `IGridContext`
- **Microsoft.Extensions.Logging.Abstractions** — Logging infrastructure
- **Microsoft.Extensions.Caching.Memory** — In-memory caching
- **Microsoft.Extensions.Options** — Options pattern

**Provider-specific (in provider packages only):**
- **Azure.Security.KeyVault.Secrets** — Azure Key Vault SDK
- **AWSSDK.SecretsManager** — AWS Secrets Manager SDK

### Node-Level Downstream Consumers

At the Grid level, Vault is consumed by:

| Cluster | Consuming Nodes | Usage |
|---------|-----------------|-------|
| **Identity** | HoneyDrunk.Auth | Key material, tokens, credential rotation |
| **AI** | HoneyDrunk.AgentKit, Signal, Clarity, Governor | Provider API keys, model selection, runtime policies |
| **Observability** | HoneyDrunk.Pulse | Secure observability configuration, telemetry ingestion |
| **Security** | HoneyDrunk.Sentinel, BreachLab.exe | Lab environments, secret handling policies |
| **Business** | Invoice, MarketCore | Payment rails, third-party integrations (billing stack via Invoice → Ledger) |
| **Application** | HoneyDrunk.Console, ClientPortalOS | Multi-tenant secrets, per-tenant configuration |

### Application-Level Consumers

Typical app-level usage patterns:

- **Web APIs** — Database connection strings, API keys, JWT signing keys
- **Background Workers** — Service credentials, encryption keys
- **Integration Services** — Third-party API tokens, webhook secrets

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

**"Trust, by design. Secrets and config for the whole Grid."** — Vault is the Grid's canonical source of truth for secrets and configuration. Focus on business logic, not secret management infrastructure.

---

*Last Updated: 2025-12-08*  
*Target Framework: .NET 10.0*
