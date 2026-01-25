using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Resilience;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="CompositeSecretStore"/> covering priority ordering,
/// required-provider fail-fast, transient vs not-found classification, and error propagation.
/// </summary>
public sealed class CompositeSecretStoreTests
{
    private readonly VaultResilienceOptions _resilienceOptions;
    private readonly ResiliencePipelineFactory _resilienceFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeSecretStoreTests"/> class.
    /// </summary>
    public CompositeSecretStoreTests()
    {
        _resilienceOptions = new VaultResilienceOptions
        {
            RetryEnabled = false, // Disable retries for deterministic testing
            CircuitBreakerEnabled = false,
        };
        _resilienceFactory = new ResiliencePipelineFactory(
            _resilienceOptions,
            NullLogger<ResiliencePipelineFactory>.Instance);
    }

    /// <summary>
    /// Verifies that the composite store returns the secret from the highest-priority provider when the secret exists in multiple providers.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ReturnsFromHighestPriorityProvider_WhenSecretExistsInMultiple()
    {
        // Arrange
        var highPriorityProvider = CreateMockProvider("high-priority", "secret-value-high");
        var lowPriorityProvider = CreateMockProvider("low-priority", "secret-value-low");

        var providers = new[]
        {
            new RegisteredSecretProvider(lowPriorityProvider.Object, CreateRegistration("low", priority: 100)),
            new RegisteredSecretProvider(highPriorityProvider.Object, CreateRegistration("high", priority: 1)),
        };

        var store = CreateCompositeStore(providers);

        // Act
        var result = await store.TryGetSecretAsync(new SecretIdentifier("test-secret"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("secret-value-high", result.Value!.Value);
        highPriorityProvider.Verify(p => p.FetchSecretAsync("test-secret", null, It.IsAny<CancellationToken>()), Times.Once);
        lowPriorityProvider.Verify(p => p.FetchSecretAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that the composite store falls back to a lower-priority provider when a higher-priority provider returns not found.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetSecretAsync_FallsBackToLowerPriorityProvider_WhenHigherPriorityReturnsNotFound()
    {
        // Arrange
        var highPriorityProvider = CreateMockProvider("high-priority", throwsNotFound: true);
        var lowPriorityProvider = CreateMockProvider("low-priority", "secret-value-low");

        var providers = new[]
        {
            new RegisteredSecretProvider(highPriorityProvider.Object, CreateRegistration("high", priority: 1)),
            new RegisteredSecretProvider(lowPriorityProvider.Object, CreateRegistration("low", priority: 100)),
        };

        var store = CreateCompositeStore(providers);

        // Act
        var result = await store.TryGetSecretAsync(new SecretIdentifier("test-secret"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("secret-value-low", result.Value!.Value);
        highPriorityProvider.Verify(p => p.FetchSecretAsync("test-secret", null, It.IsAny<CancellationToken>()), Times.Once);
        lowPriorityProvider.Verify(p => p.FetchSecretAsync("test-secret", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that the constructor orders providers by priority (ascending).
    /// </summary>
    [Fact]
    public void Constructor_OrdersProvidersByPriority()
    {
        // Arrange
        var provider1 = CreateMockProvider("provider-1").Object;
        var provider2 = CreateMockProvider("provider-2").Object;
        var provider3 = CreateMockProvider("provider-3").Object;

        var providers = new[]
        {
            new RegisteredSecretProvider(provider2, CreateRegistration("p2", priority: 50)),
            new RegisteredSecretProvider(provider3, CreateRegistration("p3", priority: 100)),
            new RegisteredSecretProvider(provider1, CreateRegistration("p1", priority: 1)),
        };

        // Act
        var store = CreateCompositeStore(providers);

        // Assert - Providers should be ordered by priority (ascending)
        Assert.Equal(3, store.Providers.Count);
        Assert.Equal("provider-1", store.Providers[0].Provider.ProviderName);
        Assert.Equal("provider-2", store.Providers[1].Provider.ProviderName);
        Assert.Equal("provider-3", store.Providers[2].Provider.ProviderName);
    }

    /// <summary>
    /// Verifies that the composite store fails fast when a required provider throws a transient error.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetSecretAsync_FailsFast_WhenRequiredProviderThrowsTransientError()
    {
        // Arrange
        var requiredProvider = CreateMockProvider("required", throwsTransient: true);
        var fallbackProvider = CreateMockProvider("fallback", "fallback-value");

        var providers = new[]
        {
            new RegisteredSecretProvider(requiredProvider.Object, CreateRegistration("required", priority: 1, isRequired: true)),
            new RegisteredSecretProvider(fallbackProvider.Object, CreateRegistration("fallback", priority: 100)),
        };

        var store = CreateCompositeStore(providers);

        // Act
        var result = await store.TryGetSecretAsync(new SecretIdentifier("test-secret"));

        // Assert - Should fail fast, not fall back
        Assert.False(result.IsSuccess);
        Assert.Contains("Required provider", result.ErrorMessage);
        fallbackProvider.Verify(p => p.FetchSecretAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that the composite store continues to fallback when an optional provider throws a transient error.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ContinuesToFallback_WhenOptionalProviderThrowsTransientError()
    {
        // Arrange
        var optionalProvider = CreateMockProvider("optional", throwsTransient: true);
        var fallbackProvider = CreateMockProvider("fallback", "fallback-value");

        var providers = new[]
        {
            new RegisteredSecretProvider(optionalProvider.Object, CreateRegistration("optional", priority: 1, isRequired: false)),
            new RegisteredSecretProvider(fallbackProvider.Object, CreateRegistration("fallback", priority: 100)),
        };

        var store = CreateCompositeStore(providers);

        // Act
        var result = await store.TryGetSecretAsync(new SecretIdentifier("test-secret"));

        // Assert - Should fall back to next provider
        Assert.True(result.IsSuccess);
        Assert.Equal("fallback-value", result.Value!.Value);
    }

    /// <summary>
    /// Verifies that the composite store continues to fallback when a required provider returns not found.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ContinuesToFallback_WhenRequiredProviderReturnsNotFound()
    {
        // Arrange - NotFound should always continue to next provider, even for required providers
        var requiredProvider = CreateMockProvider("required", throwsNotFound: true);
        var fallbackProvider = CreateMockProvider("fallback", "fallback-value");

        var providers = new[]
        {
            new RegisteredSecretProvider(requiredProvider.Object, CreateRegistration("required", priority: 1, isRequired: true)),
            new RegisteredSecretProvider(fallbackProvider.Object, CreateRegistration("fallback", priority: 100)),
        };

        var store = CreateCompositeStore(providers);

        // Act
        var result = await store.TryGetSecretAsync(new SecretIdentifier("test-secret"));

        // Assert - NotFound should still fall back even for required providers
        Assert.True(result.IsSuccess);
        Assert.Equal("fallback-value", result.Value!.Value);
    }

    /// <summary>
    /// Verifies that the composite store fails fast when a fatal configuration error occurs.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetSecretAsync_FailsFast_WhenFatalConfigurationError()
    {
        // Arrange
        var fatalProvider = CreateMockProvider("fatal", throwsFatalConfig: true);
        var fallbackProvider = CreateMockProvider("fallback", "fallback-value");

        var providers = new[]
        {
            new RegisteredSecretProvider(fatalProvider.Object, CreateRegistration("fatal", priority: 1)),
            new RegisteredSecretProvider(fallbackProvider.Object, CreateRegistration("fallback", priority: 100)),
        };

        var store = CreateCompositeStore(providers);

        // Act
        var result = await store.TryGetSecretAsync(new SecretIdentifier("test-secret"));

        // Assert - Fatal config errors should fail fast
        Assert.False(result.IsSuccess);
        Assert.Contains("Fatal configuration error", result.ErrorMessage);
        fallbackProvider.Verify(p => p.FetchSecretAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies that the composite store returns not found when all providers return not found.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ReturnsNotFound_WhenAllProvidersReturnNotFound()
    {
        // Arrange
        var provider1 = CreateMockProvider("provider-1", throwsNotFound: true);
        var provider2 = CreateMockProvider("provider-2", throwsNotFound: true);

        var providers = new[]
        {
            new RegisteredSecretProvider(provider1.Object, CreateRegistration("p1", priority: 1)),
            new RegisteredSecretProvider(provider2.Object, CreateRegistration("p2", priority: 2)),
        };

        var store = CreateCompositeStore(providers);

        // Act
        var result = await store.TryGetSecretAsync(new SecretIdentifier("missing-secret"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that GetSecretAsync throws SecretNotFoundException when the secret is not found.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetSecretAsync_ThrowsSecretNotFoundException_WhenNotFound()
    {
        // Arrange
        var provider = CreateMockProvider("provider", throwsNotFound: true);
        var providers = new[]
        {
            new RegisteredSecretProvider(provider.Object, CreateRegistration("p", priority: 1)),
        };

        var store = CreateCompositeStore(providers);

        // Act & Assert
        await Assert.ThrowsAsync<SecretNotFoundException>(
            () => store.GetSecretAsync(new SecretIdentifier("missing")));
    }

    /// <summary>
    /// Verifies that GetSecretAsync throws VaultOperationException when a provider fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetSecretAsync_ThrowsVaultOperationException_WhenProviderFails()
    {
        // Arrange
        var provider = CreateMockProvider("provider", throwsFatalConfig: true);
        var providers = new[]
        {
            new RegisteredSecretProvider(provider.Object, CreateRegistration("p", priority: 1)),
        };

        var store = CreateCompositeStore(providers);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<VaultOperationException>(
            () => store.GetSecretAsync(new SecretIdentifier("test")));
        Assert.Contains("Fatal configuration error", ex.Message);
    }

    /// <summary>
    /// Verifies that GetSecretAsync throws VaultOperationException when a required provider fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetSecretAsync_ThrowsVaultOperationException_WhenRequiredProviderFails()
    {
        // Arrange
        var provider = CreateMockProvider("provider", throwsTransient: true);
        var providers = new[]
        {
            new RegisteredSecretProvider(provider.Object, CreateRegistration("p", priority: 1, isRequired: true)),
        };

        var store = CreateCompositeStore(providers);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<VaultOperationException>(
            () => store.GetSecretAsync(new SecretIdentifier("test")));
        Assert.Contains("Required provider", ex.Message);
    }

    /// <summary>
    /// Verifies that TryGetSecretAsync returns failure when no providers are registered.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ReturnsFailure_WhenNoProvidersRegistered()
    {
        // Arrange
        var store = CreateCompositeStore([]);

        // Act
        var result = await store.TryGetSecretAsync(new SecretIdentifier("test"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("No providers available", result.ErrorMessage);
    }

    /// <summary>
    /// Verifies that the constructor filters out disabled providers.
    /// </summary>
    [Fact]
    public void Constructor_FiltersOutDisabledProviders()
    {
        // Arrange
        var enabledProvider = CreateMockProvider("enabled").Object;
        var disabledProvider = CreateMockProvider("disabled").Object;

        var providers = new[]
        {
            new RegisteredSecretProvider(enabledProvider, CreateRegistration("enabled", priority: 1, isEnabled: true)),
            new RegisteredSecretProvider(disabledProvider, CreateRegistration("disabled", priority: 2, isEnabled: false)),
        };

        // Act
        var store = CreateCompositeStore(providers);

        // Assert
        Assert.Single(store.Providers);
        Assert.Equal("enabled", store.Providers[0].Provider.ProviderName);
    }

    /// <summary>
    /// Verifies that the constructor filters out unavailable providers.
    /// </summary>
    [Fact]
    public void Constructor_FiltersOutUnavailableProviders()
    {
        // Arrange
        var availableProvider = CreateMockProvider("available", isAvailable: true).Object;
        var unavailableProvider = CreateMockProvider("unavailable", isAvailable: false).Object;

        var providers = new[]
        {
            new RegisteredSecretProvider(availableProvider, CreateRegistration("available", priority: 1)),
            new RegisteredSecretProvider(unavailableProvider, CreateRegistration("unavailable", priority: 2)),
        };

        // Act
        var store = CreateCompositeStore(providers);

        // Assert
        Assert.Single(store.Providers);
        Assert.Equal("available", store.Providers[0].Provider.ProviderName);
    }

    private static ProviderRegistration CreateRegistration(
        string name,
        int priority = 1,
        bool isRequired = false,
        bool isEnabled = true)
    {
        return new ProviderRegistration
        {
            Name = name,
            Priority = priority,
            IsRequired = isRequired,
            IsEnabled = isEnabled,
        };
    }

    private static Mock<ISecretProvider> CreateMockProvider(
        string name,
        string? returnValue = null,
        bool throwsNotFound = false,
        bool throwsTransient = false,
        bool throwsFatalConfig = false,
        bool isAvailable = true)
    {
        var mock = new Mock<ISecretProvider>();
        mock.Setup(p => p.ProviderName).Returns(name);
        mock.Setup(p => p.IsAvailable).Returns(isAvailable);

        if (throwsNotFound)
        {
            mock.Setup(p => p.FetchSecretAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new SecretNotFoundException("test-secret"));
        }
        else if (throwsTransient)
        {
            mock.Setup(p => p.FetchSecretAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new HttpRequestException("Transient network error"));
        }
        else if (throwsFatalConfig)
        {
            mock.Setup(p => p.FetchSecretAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Missing required configuration"));
        }
        else if (returnValue != null)
        {
            mock.Setup(p => p.FetchSecretAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SecretValue(new SecretIdentifier("test-secret"), returnValue, "v1"));
        }

        return mock;
    }

    private CompositeSecretStore CreateCompositeStore(IEnumerable<RegisteredSecretProvider> providers)
    {
        return new CompositeSecretStore(
            providers,
            _resilienceFactory,
            null, // telemetry
            NullLogger<CompositeSecretStore>.Instance);
    }
}
