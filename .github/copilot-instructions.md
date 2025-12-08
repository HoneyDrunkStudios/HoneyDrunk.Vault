# HoneyDrunk.Vault - Copilot Instructions

## Node Identity

| Attribute | Value |
|-----------|-------|
| **Sector** | Core |
| **Cluster** | Security |
| **Slot** | Provider |
| **Exported Contracts** | `ISecretStore`, `IConfigProvider` |
| **Package** | `HoneyDrunk.Vault` |
| **Consumes** | `HoneyDrunk.Kernel` (runtime contracts) |
| **Compile-time** | `HoneyDrunk.Kernel.Abstractions` in Vault packages; full `HoneyDrunk.Kernel` only at app/node level |

Vault is the Grid's canonical source of secrets and configuration. Other Nodes consume it via `ISecretStore` and `IConfigProvider`—never provider SDKs directly.

## Architecture Quick Reference

### Core Abstractions (`HoneyDrunk.Vault/Abstractions/`)

**Exported Contracts** (cross-Node consumption):
- **`ISecretStore`** - Primary interface for secret access. Inject this in business logic.
- **`IConfigProvider`** - Typed configuration access with defaults.

**Internal Contracts** (Vault + provider packages only):
- **`ISecretProvider`** - Backend-specific implementations (Azure, AWS, File, InMemory). Provider packages reference this.
- **`IConfigSource`** - Raw configuration source (wrapped by IConfigProvider). Provider packages reference this.
- **`IVaultClient`** - Internal orchestrator. Reserved for infrastructure/advanced orchestration; never cargo-cult into business logic.

### Provider Slot Pattern
Vault is a **provider slot Node**—provider packages implement `ISecretProvider` / `IConfigSource` and plug into the slot:
```
HoneyDrunk.Vault.Providers.{Name}/
├── Configuration/     # {Name}Options class (provider-specific)
├── Extensions/        # AddVaultWith{Name}() or AddVault{Name}() extension method
└── Services/          # {Name}SecretStore implementing ISecretStore + ISecretProvider
```

**Extension method naming:**
- `AddVaultWithAzureKeyVault()`, `AddVaultWithAwsSecretsManager()`, `AddVaultWithFile()`, `AddVaultWithConfiguration()` - cloud and file providers
- `AddVaultInMemory()` - InMemory provider (testing)

Core Vault does not know about Azure/AWS/File specifics. Provider-specific options belong in provider packages.

## Key Conventions

### Models (Immutable Records)
- **`SecretIdentifier`** - Name + optional Version. Always use this for secret lookups.
- **`SecretValue`** - Contains Identifier, Value (the secret), and Version.
- **`VaultResult<T>`** - Result pattern for Try* methods. Check `IsSuccess` before accessing `Value`.

### Exception Handling
- `GetSecretAsync()` throws `SecretNotFoundException` when not found.
- `TryGetSecretAsync()` returns `VaultResult.Failure()` instead of throwing.
- Wrap provider errors in `VaultOperationException` for consistent error handling.

### Service Registration Pattern

**Grid-Integrated (Recommended):**
```csharp
builder.Services
    .AddHoneyDrunkGrid(grid => { grid.StudioId = "..."; })
    .AddHoneyDrunkNode(node => { node.NodeId = "..."; })
    .AddVault(vault => { /* cache, resilience, warmup */ })
    .AddVaultWithAzureKeyVault(akv => { akv.VaultUri = ...; });
```

**Off-Grid (Development):**
```csharp
builder.Services.AddVaultWithFile(options => { ... });
builder.Services.AddVaultWithAzureKeyVault(options => { ... });
builder.Services.AddVaultWithAwsSecretsManager(options => { ... });
```

## Development Commands

```powershell
# Build solution from HoneyDrunk.Vault/ directory
dotnet build HoneyDrunk.Vault.slnx

# Run all tests
dotnet test HoneyDrunk.Vault.slnx

# Run tests with coverage
dotnet test HoneyDrunk.Vault.slnx --collect:"XPlat Code Coverage"

# Pack NuGet packages
dotnet pack HoneyDrunk.Vault.slnx -c Release
```

## Testing Patterns

### Use InMemorySecretStore for Unit Tests
```csharp
var secretStore = new InMemorySecretStore(NullLogger<InMemorySecretStore>.Instance);
secretStore.SetSecret("api-key", "test-value");
// Pass secretStore to your SUT
```

### Mocking with ISecretStore
```csharp
var mockStore = new Mock<ISecretStore>();
mockStore.Setup(s => s.GetSecretAsync(It.IsAny<SecretIdentifier>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(new SecretValue(new SecretIdentifier("key"), "value", "v1"));
```

## Important Implementation Details

### Telemetry Security
Secret values are **never** logged or included in telemetry. Only secret names/identifiers are traced. See `VaultTelemetry.cs`.

### Cache Behavior
`SecretCache` uses in-memory caching with configurable TTL via `VaultCacheOptions`. Cache keys are secret names; values are `SecretValue` objects.

### Primary Constructor Pattern
Services use C# 12 primary constructors. When adding new services:
```csharp
public sealed class MyService(ISecretStore secretStore, ILogger<MyService> logger) : IMyService
{
    private readonly ISecretStore _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
}
```

## File Structure Reference
- **`/docs/`** - Comprehensive documentation by domain (start with `FILE_GUIDE.md`)
- **`/HoneyDrunk.Vault/HoneyDrunk.Vault/`** - Core library with abstractions
- **`/HoneyDrunk.Vault/HoneyDrunk.Vault.Providers.*/`** - Provider implementations
- **`/HoneyDrunk.Vault/HoneyDrunk.Vault.Tests/`** - Unit tests using xUnit + Moq

## Dependencies
- **HoneyDrunk.Kernel** - Lifecycle, health, and telemetry integration (external)
- **HoneyDrunk.Standards** - Shared analyzers and conventions (build-time only)
- Provider-specific SDKs: `Azure.Security.KeyVault.Secrets`, `AWSSDK.SecretsManager`
