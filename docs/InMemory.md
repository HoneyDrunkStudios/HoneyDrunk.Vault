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
- Testing provider fallback behavior

**Provider Slot Architecture:** InMemory provider implements `ISecretProvider` and `IConfigSource` (internal contracts). `VaultCore` implements `ISecretStore` and `IConfigProvider` (exported contracts). Applications inject `ISecretStore`/`IConfigProvider`, never the provider directly.

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

builder.Services.AddVaultInMemory(options =>
{
    options.AddSecret("database-connection", "Server=localhost;Database=test;");
    options.AddSecret("api-key", "test-api-key-12345");
    options.AddConfigValue("logging:level", "Debug");
    options.AddConfigValue("cache:enabled", "true");
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
    public Dictionary<string, string> ConfigurationValues { get; }

    /// <summary>
    /// Adds a secret value.
    /// </summary>
    public InMemoryVaultOptions AddSecret(string name, string value);

    /// <summary>
    /// Adds a configuration value.
    /// </summary>
    public InMemoryVaultOptions AddConfigValue(string key, string value);
}
```

---

## InMemorySecretStore.cs

In-memory implementation of `ISecretProvider` (internal contract).

**Provider Contract:** This class implements `ISecretProvider`, not `ISecretStore`. `ISecretStore` is the exported contract implemented by `VaultClient`. InMemory provider participates in provider resolution and caching orchestration.

```csharp
public sealed class InMemorySecretStore : ISecretProvider
{
    public string ProviderName => "in-memory";
    public bool IsAvailable => true;

    public InMemorySecretStore(ILogger<InMemorySecretStore> logger);

    public InMemorySecretStore(
        ConcurrentDictionary<string, string> secrets,
        ILogger<InMemorySecretStore> logger);

    // ISecretProvider implementation
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
- **Deterministic**: InMemory provider never throws `VaultOperationException`. Failures come only from missing keys (`SecretNotFoundException`) or explicit test errors.
- **Provider availability**: InMemory provider always reports availability and is always selected when enabled, unless a higher-priority provider is configured in `VaultOptions`.
- **Versioning**: InMemory provider always returns a single implicit version placeholder when `ListSecretVersionsAsync` is called. This satisfies the interface contract but does not represent real version history.
- **GridContext propagation**: InMemory provider forwards `GridContext` into `VaultTelemetry` so test environments maintain correlation IDs and activity traces.

### Usage Example

**Proper DI Layering:** Applications should resolve `ISecretStore` through DI, not instantiate providers directly. Direct instantiation is only appropriate when testing provider implementations themselves.

```csharp
// ✅ Correct: Use DI to resolve ISecretStore
var services = new ServiceCollection();
services.AddVaultCore();
services.AddVaultInMemory(options =>
{
    options.AddSecret("api-key", "test-key");
    options.AddSecret("db-password", "test-password");
});

var provider = services.BuildServiceProvider();
var secretStore = provider.GetRequiredService<ISecretStore>();

// Retrieve secret
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key"),
    ct);

// ❌ Avoid: Direct provider instantiation (bypasses VaultCore orchestration)
// var store = new InMemorySecretStore(secrets, logger);
```

**Runtime Updates:**

```csharp
// Runtime updates (testing scenarios only)
var inMemoryProvider = provider.GetRequiredService<InMemorySecretStore>();
inMemoryProvider.SetSecret("new-key", "new-value");

// Note: Runtime updates do not invalidate the Vault cache.
// Cached values remain until TTL expires.
```

---

## InMemoryConfigSource.cs

In-memory implementation of `IConfigSourceProvider` (internal contract).

**Architecture:** `InMemoryConfigSource` implements `IConfigSourceProvider` which extends `IConfigSource`. When registered via `AddConfigSourceProvider()`, it is wrapped by `CompositeConfigSource` which implements both `IConfigSource` and `IConfigProvider`. The composite handles priority-based provider selection and exposes typed APIs to consumers.

**Typed APIs:** Both `IConfigSource` and `IConfigProvider` expose typed methods (`GetConfigValueAsync<T>`/`GetValueAsync<T>`). Type conversion is performed by `CompositeConfigSource` when serving `IConfigProvider` requests. Individual providers like `InMemoryConfigSource` implement only the string-based `IConfigSource` methods; typed overloads throw `NotSupportedException`.

**Provider Interface:** As of v0.2.0, `InMemoryConfigSource` implements `IConfigSourceProvider` (which extends `IConfigSource`), enabling registration via `AddConfigSourceProvider()` for use with composite stores and health contributors.

```csharp
public sealed class InMemoryConfigSource : IConfigSource, IConfigSourceProvider
{
    // IConfigSourceProvider members
    public string ProviderName => "in-memory";
    public bool IsAvailable => true;
    public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);

    public InMemoryConfigSource(ILogger<InMemoryConfigSource> logger);

    public InMemoryConfigSource(
        ConcurrentDictionary<string, string> config,
        ILogger<InMemoryConfigSource> logger);

    // IConfigSource implementation (raw string-based)
    public Task<string> GetConfigValueAsync(
        string key,
        CancellationToken cancellationToken = default);

    public Task<string?> TryGetConfigValueAsync(
        string key,
        CancellationToken cancellationToken = default);

    // Runtime modification
    public void SetConfigValue(string key, string value);
    public bool RemoveConfigValue(string key);
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
    // Arrange - Use DI to resolve ISecretStore
    var services = new ServiceCollection();
    services.AddVaultCore();
    services.AddVaultInMemory(options =>
    {
        options.AddSecret("db-connection", "Server=test;Database=testdb;");
    });

    var provider = services.BuildServiceProvider();
    var secretStore = provider.GetRequiredService<ISecretStore>();
    
    var service = new MyService(secretStore);

    // Act
    var connection = await service.GetDatabaseConnectionAsync();

    // Assert
    Assert.Equal("Server=test;Database=testdb;", connection);
}
```

### Integration Test Setup

**Provider Precedence:** When multiple providers are registered, provider resolution follows priority order (see `VaultOptions.Providers`). In integration tests, replace all other provider registrations to avoid provider fallback behavior interfering with results.

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
                // Remove existing providers to avoid precedence issues
                services.RemoveAll<ISecretProvider>();
                services.RemoveAll<IConfigSource>();

                // Replace with in-memory for testing
                services.AddVaultInMemory(options =>
                {
                    options.AddSecret("api-key", "test-key");
                    options.AddConfigValue("timeout", "5");
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
    public static ISecretStore CreateSecretStore(
        params (string key, string value)[] secrets)
    {
        var services = new ServiceCollection();
        services.AddVaultCore();
        services.AddVaultInMemory(options =>
        {
            foreach (var (key, value) in secrets)
            {
                options.AddSecret(key, value);
            }
        });

        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ISecretStore>();
    }

    public static IServiceCollection AddTestVault(
        this IServiceCollection services,
        params (string key, string value)[] secrets)
    {
        services.AddVaultInMemory(options =>
        {
            foreach (var (key, value) in secrets)
            {
                options.AddSecret(key, value);
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

### Simulating Provider Failure

InMemory provider is ideal for testing provider fallback behavior:

```csharp
[Fact]
public async Task VaultClient_WhenPrimaryProviderUnavailable_FallsBackToSecondary()
{
    // Arrange - Configure multiple providers
    var services = new ServiceCollection();
    services.AddVaultCore();
    services.AddVaultInMemory(options =>
    {
        options.AddSecret("api-key", "primary-value");
    });
    services.AddVaultInMemory(options => // Secondary provider
    {
        options.AddSecret("api-key", "fallback-value");
    });

    var provider = services.BuildServiceProvider();
    var inMemoryProvider = provider.GetServices<ISecretProvider>()
        .OfType<InMemorySecretStore>()
        .First();

    // Simulate primary provider failure
    inMemoryProvider.IsAvailable = false;

    var secretStore = provider.GetRequiredService<ISecretStore>();

    // Act - Vault should fall back to secondary provider
    var secret = await secretStore.GetSecretAsync(
        new SecretIdentifier("api-key"),
        CancellationToken.None);

    // Assert
    Assert.Equal("fallback-value", secret.Value);
}
```

### Reset Between Tests

```csharp
public class SecretStoreTests : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly ISecretStore _store;
    private readonly InMemorySecretStore _inMemoryProvider;

    public SecretStoreTests()
    {
        var services = new ServiceCollection();
        services.AddVaultCore();
        services.AddVaultInMemory();

        _provider = services.BuildServiceProvider();
        _store = _provider.GetRequiredService<ISecretStore>();
        _inMemoryProvider = _provider.GetRequiredService<InMemorySecretStore>();
    }

    [Fact]
    public async Task Test1()
    {
        _inMemoryProvider.SetSecret("key1", "value1");
        // ... test
    }

    [Fact]
    public async Task Test2()
    {
        _inMemoryProvider.SetSecret("key2", "value2");
        // ... test (isolated from Test1)
    }

    public void Dispose()
    {
        _inMemoryProvider.Clear();
        (_provider as IDisposable)?.Dispose();
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
