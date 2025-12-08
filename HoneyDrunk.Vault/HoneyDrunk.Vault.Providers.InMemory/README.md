# HoneyDrunk.Vault.Providers.InMemory

In-memory provider for HoneyDrunk.Vault. **Recommended for unit tests and deterministic integration tests.**

## Overview

This provider stores secrets and configuration entirely in memory. Perfect for testing where you want fast, deterministic secret access without external dependencies.

**Key Characteristics:**
- **Synchronous operations** - All operations complete immediately (no I/O)
- **Always available** - `IsAvailable` always returns true
- **No operational failures** - Cannot simulate network, file, or permission errors
- **Mutable state** - Secrets can be added/removed at runtime
- **Thread-safe** - Uses `ConcurrentDictionary` internally

**Best for:**
- Unit tests verifying business logic with predictable secrets
- Integration tests where deterministic flow is more important than resilience testing
- Development prototyping without external dependencies

**Not suitable for:**
- Resilience policy testing (cannot simulate transient failures)
- Production use (volatile, no persistence)
- Multi-process coordination (state is per-instance)

**Features:**
- No external dependencies (no files, no cloud services)
- Pre-configured secrets and configuration via options
- Runtime updates (add/remove/clear secrets)
- Fast access (O(1) dictionary lookup)
- Works great with xUnit, NUnit, MSTest

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.InMemory
```

## Quick Start

### Basic Setup

```csharp
using HoneyDrunk.Vault.Providers.InMemory.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVaultInMemory(options =>
{
    options.AddSecret("database-connection", "Server=localhost;Database=test;");
    options.AddSecret("api-key", "test-api-key-12345");
    options.AddConfigValue("logging:level", "Debug");
    options.AddConfigValue("cache:enabled", "true");
});

var app = builder.Build();
```

### In Unit Tests

**Critical:** In-memory state persists across test methods unless explicitly cleared. Always create a new instance or call `Clear()` between tests to avoid test pollution.

```csharp
[Fact]
public async Task MyService_GetDatabase_ReturnsConnection()
{
    // Arrange
    var secretStore = new InMemorySecretStore(NullLogger<InMemorySecretStore>.Instance);
    secretStore.SetSecret("db-connection", "Server=test;Database=testdb;");
    
    var service = new MyService(secretStore);

    // Act
    var connection = await service.GetDatabaseConnectionAsync();

    // Assert
    Assert.Equal("Server=test;Database=testdb;", connection);
}
```

## Usage Examples

### Set Secrets

```csharp
var secretStore = new InMemorySecretStore(logger);
secretStore.SetSecret("api-key", "my-secret-key");
secretStore.SetSecret("db-password", "secure-password");
```

### Get Secrets

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key"),
    ct);

Console.WriteLine($"Key: {secret.Value}");
```

### Handle Missing Secrets (Predictable Error Messages)

```csharp
var result = await secretStore.TryGetSecretAsync(
    new SecretIdentifier("optional-secret"),
    ct);

if (result.IsSuccess)
{
    Console.WriteLine($"Secret: {result.Value!.Value}");
}
else
{
    // Predictable message format: "Secret '{name}' not found: {details}"
    Console.WriteLine($"Not found: {result.ErrorMessage}");
}
```

**Note:** `TryGetSecretAsync` returns deterministic error messages suitable for assertion in tests.

### Set Configuration

```csharp
var configSource = new InMemoryConfigSource(logger);
configSource.SetConfigValue("database:timeout", "30");
configSource.SetConfigValue("cache:ttl", "00:15:00");
configSource.SetConfigValue("feature:new-ui", "true");
```

### Get Configuration

```csharp
var value = await configSource.GetConfigValueAsync("database:timeout", ct);
Console.WriteLine($"Timeout: {value}");
```

### Get Typed Configuration

```csharp
var timeout = await configSource.GetConfigValueAsync<int>("database:timeout", ct);
Console.WriteLine($"Timeout (int): {timeout}");

var enabled = await configSource.GetConfigValueAsync<bool>("feature:new-ui", ct);
Console.WriteLine($"Feature enabled: {enabled}");
```

### List Versions (Always Returns Single Synthetic Version)

```csharp
var versions = await secretStore.ListSecretVersionsAsync("api-key", ct);

// In-memory provider always returns a single "latest" version
foreach (var version in versions)
{
    Console.WriteLine($"Version: {version.Version}");
}
```

**Note:** In-memory provider does not support true versioning. `ListSecretVersionsAsync` always returns a single synthetic version labeled "latest".

## Unit Testing Examples

### Testing with xUnit

```csharp
public class MyServiceTests
{
    [Fact]
    public async Task GetApiKey_WithValidKey_ReturnsSecret()
    {
        // Arrange
        var secretStore = new InMemorySecretStore();
        secretStore.SetSecret("api-key", "test-key-123");
        
        var service = new MyService(secretStore);

        // Act
        var result = await service.GetApiKeyAsync();

        // Assert
        Assert.Equal("test-key-123", result);
    }

    [Fact]
    public async Task GetApiKey_WithMissingKey_ThrowsException()
    {
        // Arrange
        var secretStore = new InMemorySecretStore();
        var service = new MyService(secretStore);

        // Act & Assert
        await Assert.ThrowsAsync<SecretNotFoundException>(
            () => service.GetApiKeyAsync());
    }

    [Theory]
    [InlineData("db-connection")]
    [InlineData("api-key")]
    [InlineData("jwt-secret")]
    public async Task GetSecret_WithMultipleKeys_ReturnsCorrectSecret(string key)
    {
        // Arrange
        var secretStore = new InMemorySecretStore();
        secretStore.SetSecret("db-connection", "connection-value");
        secretStore.SetSecret("api-key", "key-value");
        secretStore.SetSecret("jwt-secret", "secret-value");

        // Act
        var result = await secretStore.GetSecretAsync(
            new SecretIdentifier(key));

        // Assert
        Assert.NotNull(result);
    }
}
```

### Testing with Moq

```csharp
public class MyServiceTests
{
    [Fact]
    public async Task MyMethod_WithDependency_CallsSecretStore()
    {
        // Arrange
        var secretStoreMock = new Mock<ISecretStore>();
        secretStoreMock
            .Setup(s => s.GetSecretAsync(
                It.IsAny<SecretIdentifier>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecretValue(
                new SecretIdentifier("key"), 
                "value", 
                "1"));

        var service = new MyService(secretStoreMock.Object);

        // Act
        var result = await service.MyMethodAsync();

        // Assert
        secretStoreMock.Verify(
            s => s.GetSecretAsync(
                It.IsAny<SecretIdentifier>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
```

## API Reference

### InMemorySecretStore

```csharp
public class InMemorySecretStore : ISecretStore
{
    // ISecretStore methods
    Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken);
    Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken);
    Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken);
    
    // Management methods
    void SetSecret(string name, string value);
    bool RemoveSecret(string name);
    void Clear();
}
```

### InMemoryConfigSource

```csharp
public class InMemoryConfigSource : IConfigSource
{
    // IConfigSource methods
    Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken);
    Task<string?> TryGetConfigValueAsync(string key, CancellationToken cancellationToken);
    Task<T> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken);
    Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken);
    
    // Management methods
    void SetConfigValue(string key, string value);
    bool RemoveConfigValue(string key);
    void Clear();
}
```

## Configuration

### InMemoryVaultOptions

```csharp
public class InMemoryVaultOptions
{
    public Dictionary<string, string> Secrets { get; }
    public Dictionary<string, string> ConfigurationValues { get; }
    
    public InMemoryVaultOptions AddSecret(string name, string value);
    public InMemoryVaultOptions AddConfigValue(string key, string value);
}
```

## Best Practices

1. **Always reset state between tests** - Call `Clear()` or create new instances to avoid test pollution
2. **Use specific, realistic values** - Make test data meaningful for debugging
3. **Test error cases** - Verify behavior with missing secrets using `TryGetSecretAsync`
4. **Keep tests independent** - Don't rely on secrets set by other tests
5. **Assert on predictable error messages** - `TryGetSecretAsync` returns deterministic messages
6. **Thread safety is guaranteed** - Safe to use from multiple threads/tasks
7. **For resilience testing, use mocks** - In-memory cannot simulate operational failures

## Kernel Integration

When using `AddVaultInMemory`, the provider integrates with Vault's caching, telemetry, and lifecycle hooks:

```csharp
builder.Services
    .AddHoneyDrunkGrid(grid => { grid.StudioId = "test"; })
    .AddHoneyDrunkNode(node => { node.NodeId = "test-node"; })
    .AddVault(vault =>
    {
        vault.Cache.Enabled = true;  // Cache still applies
        vault.WarmupKeys.Add("db-connection");  // Warmup works (trivially)
    })
    .AddVaultInMemory(options =>
    {
        options.AddSecret("db-connection", "test");
    });
```

**Even though in-memory is always fast, Vault caching and warmup still apply** for consistency with other providers.

## Use Cases

**Good for:**
- Unit testing services that depend on `ISecretStore` or `IConfigProvider`
- Integration testing without external dependencies
- Testing business logic with deterministic, fast secret access
- Development prototyping and quick iteration

**Not suitable for:**
- Resilience policy testing (cannot produce operational failures)
- Long-running development sessions expecting persistence (state is volatile)
- Multi-instance scenarios (each process has independent state)
- Production use (no persistence, encryption, or rotation)

## Performance

- **Access Time**: O(1) dictionary lookup
- **Memory**: Linear with number of secrets/configs
- **Thread Safety**: Concurrent dictionary operations
- **No I/O**: All operations in memory

## Limitations

- **Volatile** - All data lost on application restart
- **No persistence** - Cannot save to disk
- **No true versioning** - Only returns synthetic "latest" version
- **No operational failures** - Always succeeds (cannot simulate network/file/permission errors)
- **Development/testing only** - Not suitable for production
- **Per-instance state** - Multiple processes don't share state
- **Mutable during runtime** - Secrets can change mid-execution (breaks persistence mental model)

## Operational Characteristics

- **Access Time**: O(1) dictionary lookup
- **Memory**: Linear with number of secrets/configs
- **Thread Safety**: `ConcurrentDictionary` ensures safe concurrent access
- **No I/O**: All operations complete synchronously in memory
- **Always available**: `IsAvailable` always returns `true`
- **Never fails transiently**: Cannot simulate cloud provider outages or rate limiting

## Comparison with Other Providers

| Feature | InMemory | File | Azure KV | AWS SM | Configuration |
|---------|----------|------|----------|--------|---------------|
| **Best for** | Unit tests | Local dev | Azure prod | AWS prod | Migration |
| **Persistence** | No | Yes | Yes | Yes | Yes |
| **Versioning** | Synthetic | Synthetic | Yes | Yes | Synthetic |
| **Encryption** | No | Optional | Yes | Yes | No |
| **Rotation** | No | No | Yes | Yes | No |
| **Operational failures** | No | Yes (file I/O) | Yes (network) | Yes (network) | No |
| **Setup complexity** | None | Minimal | Moderate | Moderate | None |

## Related Providers

- [HoneyDrunk.Vault.Providers.File](../HoneyDrunk.Vault.Providers.File) - For development
- [HoneyDrunk.Vault.Providers.AzureKeyVault](../HoneyDrunk.Vault.Providers.AzureKeyVault) - For production
- [HoneyDrunk.Vault.Providers.Aws](../HoneyDrunk.Vault.Providers.Aws) - For AWS production
- [HoneyDrunk.Vault.Providers.Configuration](../HoneyDrunk.Vault.Providers.Configuration) - For .NET config

## License

MIT License - see LICENSE file for details.
