using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Resilience;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Additional tests for <see cref="CompositeConfigSource"/> fallback and provider behavior.
/// </summary>
public sealed class CompositeConfigSourceAdditionalTests
{
    /// <summary>
    /// Verifies constructor filters disabled and unavailable providers then orders by priority.
    /// </summary>
    [Fact]
    public void Constructor_FiltersAndOrdersProviders()
    {
        // Arrange
        var low = new TestConfigProvider("low", "low-value");
        var high = new TestConfigProvider("high", "high-value");
        var disabled = new TestConfigProvider("disabled", "disabled-value");
        var unavailable = new TestConfigProvider("unavailable", "unavailable-value", isAvailable: false);

        // Act
        var source = CreateSource([
            Register(low, priority: 100),
            Register(high, priority: 1),
            Register(disabled, enabled: false),
            Register(unavailable)]);

        // Assert
        Assert.Equal(["high", "low"], source.Providers.Select(p => p.Provider.ProviderName));
    }

    /// <summary>
    /// Verifies string lookups return the first value found by priority order.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetConfigValueAsync_ReturnsHighestPriorityProviderValue()
    {
        // Arrange
        var source = CreateSource([
            Register(new TestConfigProvider("low", "low-value"), priority: 100),
            Register(new TestConfigProvider("high", "high-value"), priority: 1)]);

        // Act
        var value = await source.GetConfigValueAsync("feature:name");

        // Assert
        Assert.Equal("high-value", value);
    }

    /// <summary>
    /// Verifies optional not-found and transient failures fall through to lower-priority providers.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_FallsBack_WhenOptionalProvidersMissOrFailTransiently()
    {
        // Arrange
        var missing = new TestConfigProvider("missing", exception: new ConfigurationNotFoundException("setting"));
        var transient = new TestConfigProvider("transient", exception: new TimeoutException("timeout"));
        var fallback = new TestConfigProvider("fallback", "fallback-value");
        var source = CreateSource([
            Register(missing, priority: 1),
            Register(transient, priority: 2),
            Register(fallback, priority: 3)]);

        // Act
        var value = await source.TryGetConfigValueAsync("setting");

        // Assert
        Assert.Equal("fallback-value", value);
        Assert.Equal(1, missing.Calls);
        Assert.Equal(1, transient.Calls);
        Assert.Equal(1, fallback.Calls);
    }

    /// <summary>
    /// Verifies required transient failures fail fast instead of falling back.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetConfigValueAsync_ThrowsVaultOperationException_WhenRequiredProviderFailsTransiently()
    {
        // Arrange
        var required = new TestConfigProvider("required", exception: new TimeoutException("timeout"));
        var fallback = new TestConfigProvider("fallback", "fallback-value");
        var source = CreateSource([
            Register(required, priority: 1, required: true),
            Register(fallback, priority: 2)]);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<VaultOperationException>(() => source.GetConfigValueAsync("setting"));
        Assert.Contains("Failed to retrieve configuration key", ex.Message, StringComparison.Ordinal);
        Assert.Equal(1, required.Calls);
        Assert.Equal(0, fallback.Calls);
    }

    /// <summary>
    /// Verifies fatal provider configuration failures fail fast.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetConfigValueAsync_ThrowsVaultOperationException_WhenProviderHasFatalConfigurationError()
    {
        // Arrange
        var fatal = new TestConfigProvider("fatal", exception: new InvalidOperationException("missing required configuration"));
        var fallback = new TestConfigProvider("fallback", "fallback-value");
        var source = CreateSource([
            Register(fatal, priority: 1),
            Register(fallback, priority: 2)]);

        // Act & Assert
        await Assert.ThrowsAsync<VaultOperationException>(() => source.GetConfigValueAsync("setting"));
        Assert.Equal(1, fatal.Calls);
        Assert.Equal(0, fallback.Calls);
    }

    /// <summary>
    /// Verifies try-get returns null when all providers miss.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_ReturnsNull_WhenAllProvidersMiss()
    {
        // Arrange
        var source = CreateSource([
            Register(new TestConfigProvider("missing", value: null), priority: 1)]);

        // Act
        var value = await source.TryGetConfigValueAsync("setting");

        // Assert
        Assert.Null(value);
    }

    /// <summary>
    /// Verifies typed and <see cref="IConfigProvider"/> members delegate through composite lookups.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TypedAndIConfigProviderMembers_DelegateToCompositeLookups()
    {
        // Arrange
        var source = CreateSource([Register(new TestConfigProvider("provider", "42"))]);
        var provider = (IConfigProvider)source;

        // Act & Assert
        Assert.Equal(42, await source.GetConfigValueAsync<int>("answer"));
        Assert.Equal(42, await source.TryGetConfigValueAsync("answer", 0));
        Assert.Equal("42", await provider.GetValueAsync("answer"));
        Assert.Equal("42", await provider.TryGetValueAsync("answer"));
        Assert.Equal(42, await provider.GetValueAsync<int>("answer"));
        Assert.Equal(42, await provider.GetValueAsync("answer", 0));
    }

    private static CompositeConfigSource CreateSource(IEnumerable<RegisteredConfigSourceProvider> providers)
    {
        var resilience = new ResiliencePipelineFactory(
            new VaultResilienceOptions { RetryEnabled = false, CircuitBreakerEnabled = false },
            NullLogger<ResiliencePipelineFactory>.Instance);

        return new CompositeConfigSource(providers, resilience, NullLogger<CompositeConfigSource>.Instance);
    }

    private static RegisteredConfigSourceProvider Register(
        TestConfigProvider provider,
        int priority = 0,
        bool enabled = true,
        bool required = false)
    {
        return new RegisteredConfigSourceProvider(
            provider,
            new ProviderRegistration
            {
                Name = provider.ProviderName,
                IsEnabled = enabled,
                IsRequired = required,
                Priority = priority,
            });
    }

    private sealed class TestConfigProvider(
        string providerName,
        string? value = null,
        bool isAvailable = true,
        Exception? exception = null) : IConfigSourceProvider
    {
        public string ProviderName { get; } = providerName;

        public bool IsAvailable { get; } = isAvailable;

        public int Calls { get; private set; }

        public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsAvailable);
        }

        public Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<T> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<string?> TryGetConfigValueAsync(string key, CancellationToken cancellationToken = default)
        {
            Calls++;
            return exception is null ? Task.FromResult(value) : Task.FromException<string?>(exception);
        }

        public Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
