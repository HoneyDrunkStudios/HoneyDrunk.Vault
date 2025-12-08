# HoneyDrunk.Vault.Providers.InMemory

In-memory provider for HoneyDrunk.Vault. **Recommended for unit tests and integration tests.**

## Overview

This provider stores secrets and configuration entirely in memory. Perfect for testing where you want fast, deterministic secret access without external dependencies.

**Features:**
- No external dependencies (no files, no cloud services)
- Pre-configured secrets and configuration
- Runtime updates (add/remove/clear secrets)
- Fast access (no I/O operations)
- Thread-safe operations
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

builder.Services.AddVaultWithInMemory(options =>
{
    options.SetSecret("database-connection", "Server=localhost;Database=test;");
    options.SetSecret("api-key", "test-api-key-12345");
    options.SetConfig("logging:level", "Debug");
    options.SetConfig("cache:enabled", "true");
});

var app = builder.Build();
```

### In Unit Tests

```csharp
[Fact]
public async Task MyService_GetDatabase_ReturnsConnection()
{
    // Arrange
    var secretStore = new InMemorySecretStore();
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
var secretStore = new InMemorySecretStore();
secretStore.SetSecret("api-key", "my-secret-key");
secretStore.SetSecret("db-password", "secure-password");
```

### Get Secrets

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key"));
console.WriteLine($"Key: {secret.Value}");
```

### Handle Missing Secrets

```csharp
var result = await secretStore.TryGetSecretAsync(
    new SecretIdentifier("optional-secret"));

if (result.IsSuccess)
{
    console.WriteLine($"Secret: {result.Value!.Value}");
}
else
{
    console.WriteLine($"Not found: {result.ErrorMessage}");
}
```

### Set Configuration

```csharp
var configSource = new InMemoryConfigSource();
configSource.SetConfigValue("database:timeout", "30");
configSource.SetConfigValue("cache:ttl", "00:15:00");
configSource.SetConfigValue("feature:new-ui", "true");
```

### Get Configuration

```csharp
var value = await configSource.GetConfigValueAsync("database:timeout");
console.WriteLine($"Timeout: {value}");
```

### Get Typed Configuration

```csharp
var timeout = await configSource.GetConfigValueAsync<int>(
    "database:timeout");
console.WriteLine($"Timeout (int): {timeout}");

var enabled = await configSource.GetConfigValueAsync<bool>(
    "feature:new-ui");
console.WriteLine($"Feature enabled: {enabled}");
```

### List Versions

```csharp
var versions = await secretStore.ListSecretVersionsAsync("api-key");
foreach (var version in versions)
{
    console.WriteLine($"Version: {version.VersionId}");
}
```

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

### InMemoryProviderOptions

```csharp
public class InMemoryProviderOptions
{
    public Dictionary<string, string> Secrets { get; }
    public Dictionary<string, string> ConfigValues { get; }
    
    public InMemoryProviderOptions AddSecret(string key, string value);
    public InMemoryProviderOptions AddConfigValue(string key, string value);
}
```

## Best Practices

1. **Reset Between Tests** - Call `Clear()` between test cases
2. **Use Specific Values** - Use realistic test data
3. **Test Error Cases** - Verify behavior with missing secrets
4. **Independent Tests** - Don't rely on other tests' state
5. **Clear Assertions** - Make test expectations obvious
6. **Thread Safety** - In-memory provider is thread-safe

## Use Cases

- Unit testing services that depend on ISecretStore
- Integration testing without external dependencies
- Development environments
- Local testing and debugging
- Performance testing (no I/O overhead)

## Performance

- **Access Time**: O(1) dictionary lookup
- **Memory**: Linear with number of secrets/configs
- **Thread Safety**: Concurrent dictionary operations
- **No I/O**: All operations in memory

## Limitations

- **Volatile**: Data lost on application restart
- **No Persistence**: Can't save to disk
- **No Versioning**: Only single value per key
- **Development Only**: Not suitable for production
- **Limited Scaling**: All data in memory

## Comparison with Other Providers

| Feature | InMemory | File | Azure KV | AWS SM | Config |
|---------|----------|------|----------|--------|--------|
| Development | ? | ? | ? | ? | ? |
| Testing | ? | ? | ? | ? | ? |
| Production | ? | ? | ? | ? | ? |
| Persistence | ? | ? | ? | ? | ? |
| Encryption | ? | Optional | ? | ? | ? |
| Versioning | ? | ? | ? | ? | ? |
| Rotation | ? | ? | ? | ? | ? |

## Related Providers

- [HoneyDrunk.Vault.Providers.File](../HoneyDrunk.Vault.Providers.File) - For development
- [HoneyDrunk.Vault.Providers.AzureKeyVault](../HoneyDrunk.Vault.Providers.AzureKeyVault) - For production
- [HoneyDrunk.Vault.Providers.Aws](../HoneyDrunk.Vault.Providers.Aws) - For AWS production
- [HoneyDrunk.Vault.Providers.Configuration](../HoneyDrunk.Vault.Providers.Configuration) - For .NET config

## License

MIT License - see LICENSE file for details.
