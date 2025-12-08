# 🧪 InMemory Provider - Testing

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Configuration](#configuration)
- [InMemorySecretStore.cs](#inmemorysecretstorecs)
- [InMemoryConfigSource.cs](#inmemoryconfigsourcecs)
- [Testing Patterns](#testing-patterns)

---

## Overview

In-memory provider for unit testing and integration testing. Stores secrets and configuration entirely in memory with no external dependencies.

**Location:** `HoneyDrunk.Vault.Providers.InMemory/`

**Use Cases:**
- Unit tests
- Integration tests
- Local development
- Mocking secret stores
- Fast iteration

**⚠️ Warning:** Not for production use. All data is lost on application restart.

---

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.InMemory
```

---

## Configuration

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

### InMemoryVaultOptions

```csharp
public sealed class InMemoryVaultOptions
{
    /// <summary>
    /// Pre-configured secrets.
    /// </summary>
    public Dictionary<string, string> Secrets { get; }

    /// <summary>
    /// Pre-configured configuration values.
    /// </summary>
    public Dictionary<string, string> Config { get; }

    /// <summary>
    /// Sets a secret value.
    /// </summary>
    public void SetSecret(string key, string value);

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    public void SetConfig(string key, string value);
}
```

---

## InMemorySecretStore.cs

In-memory implementation of `ISecretStore` and `ISecretProvider`.

```csharp
public sealed class InMemorySecretStore : ISecretStore, ISecretProvider
{
    public string ProviderName => "in-memory";
    public bool IsAvailable => true;

    public InMemorySecretStore(ILogger<InMemorySecretStore> logger);

    public InMemorySecretStore(
        ConcurrentDictionary<string, string> secrets,
        ILogger<InMemorySecretStore> logger);

    // ISecretStore implementation
    public Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    // Runtime modification
    public void SetSecret(string key, string value);
    public bool RemoveSecret(string key);
    public void Clear();
}
```

### Features

- **Thread-safe** using `ConcurrentDictionary`
- **Runtime updates** via `SetSecret`, `RemoveSecret`, `Clear`
- **Grid context support** for distributed tracing
- **No external dependencies**

### Usage Example

```csharp
// Create with initial secrets
var secrets = new ConcurrentDictionary<string, string>();
secrets["api-key"] = "test-key";
secrets["db-password"] = "test-password";

var store = new InMemorySecretStore(secrets, logger);

// Retrieve secret
var secret = await store.GetSecretAsync(
    new SecretIdentifier("api-key"),
    ct);

// Update at runtime
store.SetSecret("new-key", "new-value");

// Remove at runtime
store.RemoveSecret("old-key");

// Clear all
store.Clear();
```

---

## InMemoryConfigSource.cs

In-memory implementation of `IConfigSource`.

```csharp
public sealed class InMemoryConfigSource : IConfigSource
{
    public InMemoryConfigSource(ILogger<InMemoryConfigSource> logger);

    public InMemoryConfigSource(
        ConcurrentDictionary<string, string> config,
        ILogger<InMemoryConfigSource> logger);

    // IConfigSource implementation
    public Task<string> GetConfigValueAsync(
        string key,
        CancellationToken cancellationToken = default);

    public Task<T> TryGetConfigValueAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default);

    // Runtime modification
    public void SetConfig(string key, string value);
    public bool RemoveConfig(string key);
    public void Clear();
}
```

---

## Testing Patterns

### Unit Test Setup

```csharp
[Fact]
public async Task MyService_GetDatabase_ReturnsConnection()
{
    // Arrange
    var logger = NullLogger<InMemorySecretStore>.Instance;
    var secretStore = new InMemorySecretStore(logger);
    secretStore.SetSecret("db-connection", "Server=test;Database=testdb;");
    
    var service = new MyService(secretStore);

    // Act
    var connection = await service.GetDatabaseConnectionAsync();

    // Assert
    Assert.Equal("Server=test;Database=testdb;", connection);
}
```

### Integration Test Setup

```csharp
public class MyServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public MyServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real vault with in-memory
                services.AddVaultWithInMemory(options =>
                {
                    options.SetSecret("api-key", "test-key");
                    options.SetConfig("timeout", "5");
                });
            });
        });
    }

    [Fact]
    public async Task Api_WithValidKey_ReturnsSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/resource");

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
```

### Test Helper Class

```csharp
public static class TestVaultHelper
{
    public static InMemorySecretStore CreateSecretStore(
        params (string key, string value)[] secrets)
    {
        var store = new InMemorySecretStore(
            NullLogger<InMemorySecretStore>.Instance);

        foreach (var (key, value) in secrets)
        {
            store.SetSecret(key, value);
        }

        return store;
    }

    public static IServiceCollection AddTestVault(
        this IServiceCollection services,
        params (string key, string value)[] secrets)
    {
        services.AddVaultWithInMemory(options =>
        {
            foreach (var (key, value) in secrets)
            {
                options.SetSecret(key, value);
            }
        });

        return services;
    }
}

// Usage
var store = TestVaultHelper.CreateSecretStore(
    ("api-key", "test-key"),
    ("db-password", "test-password")
);
```

### Mocking Secret Store

```csharp
[Fact]
public async Task MyService_WhenSecretMissing_ThrowsException()
{
    // Arrange
    var mockStore = new Mock<ISecretStore>();
    mockStore
        .Setup(s => s.GetSecretAsync(
            It.IsAny<SecretIdentifier>(),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(new SecretNotFoundException("missing-key"));

    var service = new MyService(mockStore.Object);

    // Act & Assert
    await Assert.ThrowsAsync<SecretNotFoundException>(
        () => service.GetSecretAsync("missing-key", CancellationToken.None));
}
```

### Reset Between Tests

```csharp
public class SecretStoreTests : IDisposable
{
    private readonly InMemorySecretStore _store;

    public SecretStoreTests()
    {
        _store = new InMemorySecretStore(
            NullLogger<InMemorySecretStore>.Instance);
    }

    [Fact]
    public async Task Test1()
    {
        _store.SetSecret("key1", "value1");
        // ... test
    }

    [Fact]
    public async Task Test2()
    {
        _store.SetSecret("key2", "value2");
        // ... test (isolated from Test1)
    }

    public void Dispose()
    {
        _store.Clear();
    }
}
```

---

## Summary

InMemory provider is ideal for testing:

| Feature | Supported |
|---------|-----------|
| Secrets | ✅ |
| Configuration | ✅ |
| Runtime updates | ✅ |
| Thread-safe | ✅ |
| Grid context | ✅ |
| Versioning | ❌ |
| Persistence | ❌ |
| Production use | ❌ |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
