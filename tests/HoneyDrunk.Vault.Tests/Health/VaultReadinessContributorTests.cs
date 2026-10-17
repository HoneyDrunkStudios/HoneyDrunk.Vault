using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Health;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Vault.Tests.Health;

/// <summary>
/// Tests for <see cref="VaultReadinessContributor"/> required-provider readiness semantics.
/// </summary>
public sealed class VaultReadinessContributorTests
{
    /// <summary>
    /// Verifies contributor metadata remains stable for lifecycle ordering.
    /// </summary>
    [Fact]
    public void Metadata_ReturnsExpectedValues()
    {
        // Arrange
        var contributor = CreateContributor([], []);

        // Assert
        Assert.Equal("HoneyDrunk.Vault", contributor.Name);
        Assert.Equal(100, contributor.Priority);
        Assert.True(contributor.IsRequired);
    }

    /// <summary>
    /// Verifies no configured providers is treated as ready but reported explicitly.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckReadinessAsync_ReturnsReady_WhenNoProvidersConfigured()
    {
        // Arrange
        var contributor = CreateContributor([], []);

        // Act
        var (ready, message) = await contributor.CheckReadinessAsync();

        // Assert
        Assert.True(ready);
        Assert.Equal("Vault ready (no providers configured)", message);
    }

    /// <summary>
    /// Verifies any required unhealthy provider fails readiness.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckReadinessAsync_ReturnsNotReady_WhenRequiredProviderIsUnhealthy()
    {
        // Arrange
        var contributor = CreateContributor(
            [RegisterSecret(new TestSecretProvider("required-down", isHealthy: false), required: true)],
            [RegisterConfig(new TestConfigProvider("optional-up", isHealthy: true))]);

        // Act
        var (ready, message) = await contributor.CheckReadinessAsync();

        // Assert
        Assert.False(ready);
        Assert.Equal("Required providers not ready: required-down", message);
    }

    /// <summary>
    /// Verifies required provider exceptions fail readiness and keep checking other enabled providers.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckReadinessAsync_ReturnsNotReady_WhenRequiredProviderThrows()
    {
        // Arrange
        var optional = new TestConfigProvider("optional-up", isHealthy: true);
        var contributor = CreateContributor(
            [RegisterSecret(new TestSecretProvider("required-throwing", exception: new InvalidOperationException("boom")), required: true)],
            [RegisterConfig(optional)]);

        // Act
        var (ready, message) = await contributor.CheckReadinessAsync();

        // Assert
        Assert.False(ready);
        Assert.Equal("Required providers not ready: required-throwing", message);
        Assert.Equal(1, optional.HealthChecks);
    }

    /// <summary>
    /// Verifies optional outages are reported while readiness succeeds when another provider is ready.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckReadinessAsync_ReturnsReadyWithUnavailableProviders_WhenOnlyOptionalProvidersAreDown()
    {
        // Arrange
        var contributor = CreateContributor(
            [RegisterSecret(new TestSecretProvider("ready", isHealthy: true))],
            [RegisterConfig(new TestConfigProvider("optional-down", isHealthy: false))]);

        // Act
        var (ready, message) = await contributor.CheckReadinessAsync();

        // Assert
        Assert.True(ready);
        Assert.Equal("Ready: ready; Unavailable: optional-down", message);
    }

    /// <summary>
    /// Verifies duplicated provider names are de-duplicated across secret and config providers.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckReadinessAsync_DeDuplicatesProviderNamesAcrossProviderKinds()
    {
        // Arrange
        var contributor = CreateContributor(
            [RegisterSecret(new TestSecretProvider("shared", isHealthy: true))],
            [RegisterConfig(new TestConfigProvider("shared", isHealthy: false), required: true)]);

        // Act
        var (ready, message) = await contributor.CheckReadinessAsync();

        // Assert
        Assert.False(ready);
        Assert.Equal("Required providers not ready: shared", message);
    }

    /// <summary>
    /// Verifies disabled providers are not probed for readiness.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckReadinessAsync_IgnoresDisabledProviders()
    {
        // Arrange
        var disabled = new TestSecretProvider("disabled", isHealthy: false);
        var contributor = CreateContributor(
            [RegisterSecret(disabled, enabled: false, required: true)],
            []);

        // Act
        var (ready, message) = await contributor.CheckReadinessAsync();

        // Assert
        Assert.True(ready);
        Assert.Equal("Vault ready (no providers configured)", message);
        Assert.Equal(0, disabled.HealthChecks);
    }

    /// <summary>
    /// Verifies no providers ready reports not ready.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckReadinessAsync_ReturnsNotReady_WhenNoEnabledProviderIsReady()
    {
        // Arrange
        var contributor = CreateContributor(
            [RegisterSecret(new TestSecretProvider("optional-down", isHealthy: false))],
            []);

        // Act
        var (ready, message) = await contributor.CheckReadinessAsync();

        // Assert
        Assert.False(ready);
        Assert.Equal("No providers available", message);
    }

    private static VaultReadinessContributor CreateContributor(
        IEnumerable<RegisteredSecretProvider> secretProviders,
        IEnumerable<RegisteredConfigSourceProvider> configProviders)
    {
        return new VaultReadinessContributor(
            secretProviders,
            configProviders,
            NullLogger<VaultReadinessContributor>.Instance);
    }

    private static RegisteredSecretProvider RegisterSecret(TestSecretProvider provider, bool enabled = true, bool required = false)
    {
        return new RegisteredSecretProvider(provider, new ProviderRegistration { Name = provider.ProviderName, IsEnabled = enabled, IsRequired = required });
    }

    private static RegisteredConfigSourceProvider RegisterConfig(TestConfigProvider provider, bool enabled = true, bool required = false)
    {
        return new RegisteredConfigSourceProvider(provider, new ProviderRegistration { Name = provider.ProviderName, IsEnabled = enabled, IsRequired = required });
    }

    private sealed class TestSecretProvider(string providerName, bool isHealthy = true, Exception? exception = null) : ISecretProvider
    {
        public string ProviderName { get; } = providerName;

        public bool IsAvailable => isHealthy;

        public int HealthChecks { get; private set; }

        public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            HealthChecks++;
            return exception is null ? Task.FromResult(isHealthy) : Task.FromException<bool>(exception);
        }

        public Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class TestConfigProvider(string providerName, bool isHealthy = true, Exception? exception = null) : IConfigSourceProvider
    {
        public string ProviderName { get; } = providerName;

        public bool IsAvailable => isHealthy;

        public int HealthChecks { get; private set; }

        public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            HealthChecks++;
            return exception is null ? Task.FromResult(isHealthy) : Task.FromException<bool>(exception);
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
            throw new NotSupportedException();
        }

        public Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
