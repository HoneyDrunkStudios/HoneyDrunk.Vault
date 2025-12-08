# 🏛️ Architecture - Dependency Flow and Integration Patterns

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Layer Diagram](#layer-diagram)
- [Dependency Flow](#dependency-flow)
- [Provider Resolution](#provider-resolution)
- [Caching Strategy](#caching-strategy)
- [Kernel Integration](#kernel-integration)

---

## Overview

HoneyDrunk.Vault follows a layered architecture that separates abstractions from implementations, enabling provider-agnostic secret and configuration access.

**Location:** `HoneyDrunk.Vault/docs/Architecture.md`

**Scope:** Node-level architecture for HoneyDrunk.Vault and its provider packages

The core library (`HoneyDrunk.Vault`) contains abstractions and orchestration logic, while provider packages (`HoneyDrunk.Vault.Providers.*`) implement backend-specific access.

---

## Layer Diagram

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Application Layer                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │
│  │   Web API       │  │ Background      │  │   Integration       │  │
│  │   Controllers   │  │ Workers         │  │   Services          │  │
│  └────────┬────────┘  └────────┬────────┘  └──────────┬──────────┘  │
└───────────┼─────────────────────┼─────────────────────┼─────────────┘
            │                     │                     │
            └─────────────────────┼─────────────────────┘
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     HoneyDrunk.Vault (Core)                         │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │         ISecretStore / IConfigProvider (Exported)           │    │
│  │              (Primary contracts for apps)                   │    │
│  └──────────────────────────┬───────────────────────────────────┘    │
│                             │                                        │
│  ┌──────────────────────────▼───────────────────────────────────┐   │
│  │         IVaultClient (Internal Orchestrator)                 │   │
│  │         (Infrastructure components only)                     │   │
│  │   ┌─────────────────┐  ┌─────────────────────────────────┐  │   │
│  │   │  VaultClient    │  │      VaultTelemetry             │  │   │
│  │   │  (Orchestrator) │  │      (Activity Tracing)         │  │   │
│  │   └────────┬────────┘  └─────────────────────────────────┘  │   │
│  │            │                                                 │   │
│  │   ┌────────▼────────┐  ┌─────────────────────────────────┐  │   │
│  │   │  SecretCache    │  │     VaultResilienceOptions      │  │   │
│  │   │  (TTL/Size)     │  │     (Retry/CircuitBreaker)      │  │   │
│  │   └────────┬────────┘  └─────────────────────────────────┘  │   │
│  │            │                                                 │   │
│  │   ┌────────▼────────────────────────────────────────────┐   │   │
│  │   │     ISecretProvider / IConfigSource (Internal)      │   │   │
│  │   │            (Provider implementations)               │   │   │
│  │   └──────────────────────────┬───────────────────────────┘   │   │
│  └──────────────────────────────┼───────────────────────────────┘   │
└─────────────────────────────────┼───────────────────────────────────┘
                                  │
           ┌──────────────────────┼──────────────────────┬──────────┬──────────┐
           │                      │                      │          │          │
           ▼                      ▼                      ▼          ▼          ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┬──────────┬──────────┐
│   Providers     │    │   Providers     │    │   Providers     │Providers │Providers │
│   .File         │    │   .AzureKeyVault│    │   .Aws          │.InMemory │.Config   │
├─────────────────┤    ├─────────────────┤    ├─────────────────┼──────────┼──────────┤
│FileSecretStore  │    │AzureKeyVault    │    │AwsSecrets       │InMemory  │Config    │
│FileConfigSource │    │SecretStore      │    │ManagerSecret    │Secret    │Secret    │
│                 │    │AzureKeyVault    │    │Store            │Store     │Store     │
│                 │    │ConfigSource     │    │                 │InMemory  │Config    │
│                 │    │                 │    │                 │Config    │Config    │
│                 │    │                 │    │                 │Source    │Source    │
└────────┬────────┘    └────────┬────────┘    └────────┬────────┴──────────┴──────────┘
         │                      │                      │
         ▼                      ▼                      ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Local Files    │    │  Azure Key      │    │  AWS Secrets    │
│  (JSON)         │    │  Vault Service  │    │  Manager        │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

[↑ Back to top](#table-of-contents)

---

## Dependency Flow

### Package Dependencies

```
HoneyDrunk.Vault (Core)
    ├── HoneyDrunk.Kernel.Abstractions
    ├── Microsoft.Extensions.Caching.Memory
    ├── Microsoft.Extensions.Logging.Abstractions
    └── Microsoft.Extensions.Options

HoneyDrunk.Vault.Providers.AzureKeyVault
    ├── HoneyDrunk.Vault
    ├── Azure.Security.KeyVault.Secrets
    └── Azure.Identity

HoneyDrunk.Vault.Providers.Aws
    ├── HoneyDrunk.Vault
    └── AWSSDK.SecretsManager

HoneyDrunk.Vault.Providers.File
    └── HoneyDrunk.Vault

HoneyDrunk.Vault.Providers.InMemory
    └── HoneyDrunk.Vault

HoneyDrunk.Vault.Providers.Configuration
    ├── HoneyDrunk.Vault
    └── Microsoft.Extensions.Configuration.Abstractions
```

**Grid-Level Dependency Note:**

At the Grid level, Vault consumes the `HoneyDrunk.Kernel` Node for lifecycle, health, and context. At the package level, Vault depends only on `HoneyDrunk.Kernel.Abstractions`. The full Kernel package is referenced in the hosting application, not inside Vault or its providers.

### Interface Hierarchy

**Exported Contracts (Application Layer):**
```
ISecretStore (Primary secret access, exported)
    └── Used by application code for secrets

IConfigProvider (Typed config access, exported)
    └── Used by application code for configuration
```

**Internal Orchestration:**
```
IVaultClient (Unified Orchestrator, internal)
    │
    ├── Uses ISecretStore (exported contract)
    └── Uses IConfigProvider (exported contract)
```

**Provider Implementations (Internal):**
```
ISecretProvider (Internal provider contract)
    └── Implemented by providers:
            ├── FileSecretStore (also implements ISecretStore for off-grid usage)
            ├── AzureKeyVaultSecretStore
            ├── AwsSecretsManagerSecretStore
            ├── InMemorySecretStore
            └── ConfigurationSecretStore

IConfigSource (Internal provider contract)
    └── Implemented by providers:
            ├── FileConfigSource
            ├── AzureKeyVaultConfigSource
            ├── InMemoryConfigSource
            └── ConfigurationConfigSource
```

[↑ Back to top](#table-of-contents)

---

## Provider Resolution

### Registration Order

```csharp
// Providers are registered in order of precedence
builder.Services.AddVault(options =>
{
    // First registered = highest priority
    options.AddAzureKeyVaultProvider(akv => { ... });
    
    // Fallback provider
    options.AddFileProvider(file => { ... });
    
    // DefaultProvider can override
    options.DefaultProvider = "azure-keyvault";
});
```

### Resolution Algorithm

```
1. Check if DefaultProvider is specified
   ├── Yes: Use DefaultProvider
   └── No: Use first registered provider

2. Check if provider IsAvailable
   ├── Yes: Use this provider
   └── No: Try next provider in order

3. If no providers available
   └── Throw VaultOperationException
```

**Provider Availability:**

A provider is considered available if:
- It is enabled by configuration
- Its basic connectivity check passes (credentials present, endpoint reachable)
- Its circuit breaker is not permanently open for this operation type

### Example Resolution

```csharp
// Given these providers registered:
options.AddAzureKeyVaultProvider(...)  // Priority 1
options.AddFileProvider(...)           // Priority 2

// If Azure Key Vault is available:
//   → AzureKeyVaultSecretStore is used

// If Azure Key Vault is unavailable (network issue):
//   → FileSecretStore is used as fallback
```

[↑ Back to top](#table-of-contents)

---

## Caching Strategy

### Cache Flow

```
GetSecretAsync("db-connection")
        │
        ▼
┌───────────────────┐
│   SecretCache     │
│   TryGet()        │
└────────┬──────────┘
         │
    ┌────┴────┐
    │         │
  Hit       Miss
    │         │
    ▼         ▼
┌───────┐ ┌────────────────┐
│Return │ │ ISecretStore   │
│Cached │ │ GetSecretAsync │
│Value  │ └───────┬────────┘
└───────┘         │
                  ▼
          ┌──────────────┐
          │ SecretCache  │
          │ Set()        │
          └──────┬───────┘
                 │
                 ▼
          ┌──────────────┐
          │ Return       │
          │ Fresh Value  │
          └──────────────┘
```

### Cache Configuration

```csharp
options.Cache.Enabled = true;           // Enable/disable cache
options.Cache.DefaultTtl = TimeSpan.FromMinutes(15);  // Absolute expiration
options.Cache.MaxSize = 1000;           // Max cached items
options.Cache.SlidingExpiration = TimeSpan.FromMinutes(5); // Sliding window
```

### Cache Key Generation

```csharp
// Cache key format:
// "{Scope}:{SecretName}:{Version}"
// or
// "{Scope}:{SecretName}" (if no version)
//
// Scope includes environment/tenant/node context
// to prevent cache collisions across contexts

var scopePrefix = scope?.ToString() ?? "default";
var key = identifier.Version != null
    ? $"{scopePrefix}:{identifier.Name}:{identifier.Version}"
    : $"{scopePrefix}:{identifier.Name}";
```

**Rotation Friendliness:**

Cache TTLs should be configured relative to secret rotation cadence. Rotation friendliness is a design goal—Vault does not cache indefinitely and never bypasses provider checks for rotation-sensitive keys.

[↑ Back to top](#table-of-contents)

---

## Kernel Integration

### Lifecycle Hooks

```
Application Startup
        │
        ▼
┌───────────────────────────────────────────┐
│  IStartupHook: VaultStartupHook           │
│  (Runs after core Kernel initialization)  │
│                                           │
│  1. Validate provider configuration       │
│  2. Check enabled providers               │
│  3. Warm cache with WarmupKeys           │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  IReadinessContributor:                   │
│  VaultReadinessContributor                │
│                                           │
│  Checks:                                  │
│  - At least one provider enabled          │
│  - Health check secret accessible         │
└───────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────┐
│  IHealthContributor:                      │
│  VaultHealthContributor                   │
│                                           │
│  Reports:                                 │
│  - Vault operational status               │
│  - Health check secret status             │
└───────────────────────────────────────────┘
```

**Kernel Contract Integration:**

All of these components implement Kernel's standard contracts (`IStartupHook`, `IReadinessContributor`, `IHealthContributor`), so Vault plugs into the same health and lifecycle pipeline as other Nodes. Vault does not invent its own health system—it integrates with Kernel's existing observability infrastructure.

### Health Check Flow

```csharp
// Kubernetes liveness probe
GET /health/live

// VaultHealthContributor checks:
// 1. Can we reach the configured provider?
// 2. Can we retrieve the health check secret (if configured)?

// Returns:
// - Healthy: Vault is operational
// - Unhealthy: Cannot reach provider
```

### Readiness Check Flow

```csharp
// Kubernetes readiness probe
GET /health/ready

// VaultReadinessContributor checks:
// 1. Is at least one provider enabled?
// 2. Are warmup secrets loaded successfully?

// Returns:
// - Ready: Vault is ready to serve requests
// - NotReady: Vault is still initializing
```

[↑ Back to top](#table-of-contents)

---

## Summary

The architecture provides a clean separation between abstraction and implementation, enabling:

1. **Provider Independence** - Swap backends without code changes
2. **Caching Efficiency** - Reduce remote calls with configurable TTL
3. **Resilience** - Retry and circuit breaker patterns
4. **Observability** - Full telemetry integration
5. **Kernel Awareness** - Lifecycle, health, and context propagation

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
