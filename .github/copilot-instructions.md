# HoneyDrunk.Vault - Copilot Instructions

## Project Overview
HoneyDrunk.Vault is a multi-provider secrets and configuration management library for .NET 10. It provides a unified abstraction layer over File, Azure Key Vault, AWS Secrets Manager, Configuration, and InMemory backends with built-in caching, resilience, and telemetry.

## Architecture Quick Reference

### Core Abstractions (`HoneyDrunk.Vault/Abstractions/`)
- **`ISecretStore`** - Primary interface for secret access. Inject this in application code.
- **`ISecretProvider`** - Backend-specific implementations (Azure, AWS, File, InMemory).
- **`IConfigSource`** / **`IConfigProvider`** - Configuration access with typed deserialization.
- **`IVaultClient`** - Central orchestrator combining secrets and config access.

### Provider Pattern
Each provider package follows this structure:
```
HoneyDrunk.Vault.Providers.{Name}/
├── Configuration/     # {Name}Options class
├── Extensions/        # AddVaultWith{Name}() extension method
└── Services/          # {Name}SecretStore implementing ISecretStore + ISecretProvider
```

Extension methods register both core services via `AddVaultCore()` and provider implementations.

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
```csharp
// Each provider has an AddVaultWith{Provider} extension
builder.Services.AddVaultWithFile(options => { ... });
builder.Services.AddVaultWithAzureKeyVault(options => { ... });
builder.Services.AddVaultWithAwsSecretsManager(options => { ... });
builder.Services.AddVaultWithInMemory(options => { ... });
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
