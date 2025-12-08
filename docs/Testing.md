# 🧪 Testing - Test Patterns and Strategies

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Unit Testing](#unit-testing)
- [Integration Testing](#integration-testing)
- [Mocking Strategies](#mocking-strategies)
- [Test Fixtures](#test-fixtures)
- [Best Practices](#best-practices)

---

## Overview

Testing strategies for applications using HoneyDrunk.Vault. This guide covers unit testing, integration testing, and mocking patterns.

**Location:** `HoneyDrunk.Vault.Tests/`

---

## Unit Testing

### Using InMemory Provider

The InMemory provider is the recommended approach for unit tests:

```csharp
using HoneyDrunk.Vault.Providers.InMemory.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class PaymentServiceTests
{
    [Fact]
    public async Task ProcessPayment_WithValidCredentials_Succeeds()
    {
        // Arrange
        var secretStore = new InMemorySecretStore(
            NullLogger<InMemorySecretStore>.Instance);
        secretStore.SetSecret("payment-api-key", "test-key");
        secretStore.SetSecret("payment-api-secret", "test-secret");

        var service = new PaymentService(secretStore);

        // Act
        var result = await service.ProcessPaymentAsync(
            new Payment { Amount = 100 },
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ProcessPayment_WithMissingCredentials_ThrowsException()
    {
        // Arrange
        var secretStore = new InMemorySecretStore(
            NullLogger<InMemorySecretStore>.Instance);
        // No secrets configured

        var service = new PaymentService(secretStore);

        // Act & Assert
        await Assert.ThrowsAsync<SecretNotFoundException>(
            () => service.ProcessPaymentAsync(
                new Payment { Amount = 100 },
                CancellationToken.None));
    }
}
```

### Testing Configuration Access

```csharp
public class FeatureServiceTests
{
    [Fact]
    public async Task IsFeatureEnabled_WhenConfigured_ReturnsTrue()
    {
        // Arrange
        var configSource = new InMemoryConfigSource(
            NullLogger<InMemoryConfigSource>.Instance);
        configSource.SetConfig("feature:new-ui", "true");

        var service = new FeatureService(configSource);

        // Act
        var isEnabled = await service.IsFeatureEnabledAsync(
            "new-ui",
            CancellationToken.None);

        // Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public async Task GetTimeout_WhenNotConfigured_ReturnsDefault()
    {
        // Arrange
        var configSource = new InMemoryConfigSource(
            NullLogger<InMemoryConfigSource>.Instance);
        // No config set

        var service = new FeatureService(configSource);

        // Act
        var timeout = await service.GetTimeoutAsync(CancellationToken.None);

        // Assert
        Assert.Equal(30, timeout); // Default value
    }
}
```

---

## Integration Testing

### WebApplicationFactory Setup

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove production vault registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ISecretStore));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add test vault
                services.AddVaultWithInMemory(options =>
                {
                    options.SetSecret("api-key", "test-api-key");
                    options.SetSecret("db-connection", "Server=test;...");
                    options.SetConfig("timeout", "5");
                });
            });
        });
    }

    [Fact]
    public async Task GetResource_WithValidAuth_ReturnsOk()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/resource");

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }
}
```

### Custom WebApplicationFactory

```csharp
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    public InMemorySecretStore SecretStore { get; private set; } = null!;
    public InMemoryConfigSource ConfigSource { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Create shared instances for test manipulation
            SecretStore = new InMemorySecretStore(
                NullLogger<InMemorySecretStore>.Instance);
            ConfigSource = new InMemoryConfigSource(
                NullLogger<InMemoryConfigSource>.Instance);

            // Register as singletons
            services.AddSingleton<ISecretStore>(SecretStore);
            services.AddSingleton<IConfigSource>(ConfigSource);
        });
    }

    public void SetupDefaultSecrets()
    {
        SecretStore.SetSecret("api-key", "test-key");
        SecretStore.SetSecret("db-connection", "Server=test;...");
    }
}

// Usage
public class MyTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public MyTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.SetupDefaultSecrets();
    }

    [Fact]
    public async Task Test_CanModifySecretsAtRuntime()
    {
        // Modify secrets during test
        _factory.SecretStore.SetSecret("special-key", "special-value");

        var client = _factory.CreateClient();
        // ... test with modified secrets
    }
}
```

---

## Mocking Strategies

### Using Moq

```csharp
using Moq;
using Xunit;

public class ServiceWithMockedVaultTests
{
    [Fact]
    public async Task Service_WhenSecretExists_UsesSecret()
    {
        // Arrange
        var mockStore = new Mock<ISecretStore>();
        mockStore
            .Setup(s => s.GetSecretAsync(
                It.Is<SecretIdentifier>(id => id.Name == "api-key"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SecretValue(
                new SecretIdentifier("api-key"),
                "mocked-value",
                null));

        var service = new MyService(mockStore.Object);

        // Act
        var result = await service.DoWorkAsync(CancellationToken.None);

        // Assert
        Assert.Equal("mocked-value", result);
        mockStore.Verify(
            s => s.GetSecretAsync(
                It.IsAny<SecretIdentifier>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Service_WhenSecretMissing_HandlesGracefully()
    {
        // Arrange
        var mockStore = new Mock<ISecretStore>();
        mockStore
            .Setup(s => s.TryGetSecretAsync(
                It.IsAny<SecretIdentifier>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VaultResult.Failure<SecretValue>("Not found"));

        var service = new MyService(mockStore.Object);

        // Act
        var result = await service.TryDoWorkAsync(CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Service_WhenVaultFails_ThrowsException()
    {
        // Arrange
        var mockStore = new Mock<ISecretStore>();
        mockStore
            .Setup(s => s.GetSecretAsync(
                It.IsAny<SecretIdentifier>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new VaultOperationException("Connection failed"));

        var service = new MyService(mockStore.Object);

        // Act & Assert
        await Assert.ThrowsAsync<VaultOperationException>(
            () => service.DoWorkAsync(CancellationToken.None));
    }
}
```

### Mock Factory Helper

```csharp
public static class VaultMockFactory
{
    public static Mock<ISecretStore> CreateSecretStore(
        params (string key, string value)[] secrets)
    {
        var mock = new Mock<ISecretStore>();

        foreach (var (key, value) in secrets)
        {
            mock.Setup(s => s.GetSecretAsync(
                    It.Is<SecretIdentifier>(id => id.Name == key),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SecretValue(
                    new SecretIdentifier(key),
                    value,
                    null));

            mock.Setup(s => s.TryGetSecretAsync(
                    It.Is<SecretIdentifier>(id => id.Name == key),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(VaultResult.Success(new SecretValue(
                    new SecretIdentifier(key),
                    value,
                    null)));
        }

        // Default for missing secrets
        mock.Setup(s => s.GetSecretAsync(
                It.IsAny<SecretIdentifier>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SecretNotFoundException("unknown"));

        return mock;
    }
}

// Usage
[Fact]
public async Task Test()
{
    var mockStore = VaultMockFactory.CreateSecretStore(
        ("api-key", "test-key"),
        ("db-password", "test-password")
    );

    var service = new MyService(mockStore.Object);
    // ...
}
```

---

## Test Fixtures

### Shared Vault Fixture

```csharp
public class VaultFixture : IDisposable
{
    public InMemorySecretStore SecretStore { get; }
    public InMemoryConfigSource ConfigSource { get; }

    public VaultFixture()
    {
        SecretStore = new InMemorySecretStore(
            NullLogger<InMemorySecretStore>.Instance);
        ConfigSource = new InMemoryConfigSource(
            NullLogger<InMemoryConfigSource>.Instance);

        // Setup common test data
        SecretStore.SetSecret("api-key", "test-api-key");
        SecretStore.SetSecret("db-connection", "Server=localhost;");
        ConfigSource.SetConfig("timeout", "30");
    }

    public void Reset()
    {
        SecretStore.Clear();
        ConfigSource.Clear();
    }

    public void Dispose()
    {
        Reset();
    }
}

// Usage with xUnit collection
[CollectionDefinition("Vault")]
public class VaultCollection : ICollectionFixture<VaultFixture> { }

[Collection("Vault")]
public class MyServiceTests
{
    private readonly VaultFixture _fixture;

    public MyServiceTests(VaultFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset(); // Clean state for each test
    }

    [Fact]
    public async Task Test()
    {
        var service = new MyService(_fixture.SecretStore);
        // ...
    }
}
```

---

## Best Practices

### 1. Prefer InMemory Over Mocks

```csharp
// ✅ Good - Real implementation, predictable behavior
var store = new InMemorySecretStore(logger);
store.SetSecret("key", "value");

// ⚠️ Use mocks when you need to verify interactions
var mock = new Mock<ISecretStore>();
mock.Verify(s => s.GetSecretAsync(...), Times.Once);
```

### 2. Isolate Tests

```csharp
// ✅ Good - Each test gets fresh state
public class MyTests : IDisposable
{
    private readonly InMemorySecretStore _store;

    public MyTests()
    {
        _store = new InMemorySecretStore(logger);
    }

    public void Dispose() => _store.Clear();
}
```

### 3. Test Error Scenarios

```csharp
[Fact]
public async Task Service_HandlesSecretNotFound()
{
    var store = new InMemorySecretStore(logger);
    // No secrets configured
    
    var service = new MyService(store);
    
    await Assert.ThrowsAsync<SecretNotFoundException>(
        () => service.GetRequiredSecretAsync(ct));
}

[Fact]
public async Task Service_HandlesTryGetGracefully()
{
    var store = new InMemorySecretStore(logger);
    var service = new MyService(store);
    
    var result = await service.TryGetOptionalSecretAsync(ct);
    
    Assert.Null(result);
}
```

### 4. Don't Test Framework Code

```csharp
// ❌ Bad - Testing the framework, not your code
[Fact]
public async Task InMemoryStore_SetSecret_CanBeRetrieved()
{
    var store = new InMemorySecretStore(logger);
    store.SetSecret("key", "value");
    
    var secret = await store.GetSecretAsync(new SecretIdentifier("key"), ct);
    
    Assert.Equal("value", secret.Value);
}

// ✅ Good - Testing your code's behavior
[Fact]
public async Task MyService_UsesSecretCorrectly()
{
    var store = new InMemorySecretStore(logger);
    store.SetSecret("key", "value");
    
    var service = new MyService(store);
    var result = await service.ProcessAsync(ct);
    
    Assert.Contains("processed-value", result);
}
```

---

## Summary

Testing strategies for vault-dependent code:

| Approach | Use Case | Complexity |
|----------|----------|------------|
| InMemory Provider | Most tests | Low |
| Moq/Mock | Interaction verification | Medium |
| WebApplicationFactory | Integration tests | High |
| Test Fixtures | Shared setup | Medium |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
