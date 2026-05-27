using HoneyDrunk.Kernel.Abstractions.Health;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Health;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Vault.Tests.Health;

/// <summary>
/// Tests for <see cref="VaultHealthContributor"/> provider aggregation behavior.
/// </summary>
public sealed class VaultHealthContributorTests
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
        Assert.True(contributor.IsCritical);
    }

    /// <summary>
    /// Verifies no configured providers report a degraded health state.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenNoProvidersConfigured()
    {
        // Arrange
        var contributor = CreateContributor([], []);

        // Act
        var (status, message) = await contributor.CheckHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Degraded, status);
        Assert.Equal("No vault providers configured", message);
    }

    /// <summary>
    /// Verifies all healthy providers produce a healthy result with de-duplicated provider names.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenAllEnabledProvidersAreHealthy()
    {
        // Arrange
        var secret = new TestSecretProvider("shared", isHealthy: true);
        var config = new TestConfigProvider("shared", isHealthy: true);
        var contributor = CreateContributor(
            [RegisterSecret(secret)],
            [RegisterConfig(config)]);

        // Act
        var (status, message) = await contributor.CheckHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Healthy, status);
        Assert.Equal("All providers healthy: shared", message);
        Assert.Equal(1, secret.HealthChecks);
        Assert.Equal(1, config.HealthChecks);
    }

    /// <summary>
    /// Verifies mixed provider health reports degraded rather than unhealthy.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenSomeProvidersAreUnhealthy()
    {
        // Arrange
        var contributor = CreateContributor(
            [RegisterSecret(new TestSecretProvider("healthy", isHealthy: true))],
            [RegisterConfig(new TestConfigProvider("unhealthy", isHealthy: false))]);

        // Act
        var (status, message) = await contributor.CheckHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Degraded, status);
        Assert.Equal("Healthy: healthy; Unhealthy: unhealthy", message);
    }

    /// <summary>
    /// Verifies all unhealthy providers report an unhealthy state.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenNoEnabledProviderIsHealthy()
    {
        // Arrange
        var contributor = CreateContributor(
            [RegisterSecret(new TestSecretProvider("down", isHealthy: false))],
            [RegisterConfig(new TestConfigProvider("throwing", exception: new InvalidOperationException("boom")))]);

        // Act
        var (status, message) = await contributor.CheckHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, status);
        Assert.Equal("All providers unhealthy: down, throwing", message);
    }

    /// <summary>
    /// Verifies disabled providers are not probed during health checks.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckHealthAsync_IgnoresDisabledProviders()
    {
        // Arrange
        var disabled = new TestSecretProvider("disabled", isHealthy: false);
        var enabled = new TestConfigProvider("enabled", isHealthy: true);
        var contributor = CreateContributor(
            [RegisterSecret(disabled, enabled: false)],
            [RegisterConfig(enabled)]);

        // Act
        var (status, message) = await contributor.CheckHealthAsync();

        // Assert
        Assert.Equal(HealthStatus.Healthy, status);
        Assert.Equal("All providers healthy: enabled", message);
        Assert.Equal(0, disabled.HealthChecks);
        Assert.Equal(1, enabled.HealthChecks);
    }

    private static VaultHealthContributor CreateContributor(
        IEnumerable<RegisteredSecretProvider> secretProviders,
        IEnumerable<RegisteredConfigSourceProvider> configProviders)
    {
        return new VaultHealthContributor(
            secretProviders,
            configProviders,
            NullLogger<VaultHealthContributor>.Instance);
    }

    private static RegisteredSecretProvider RegisterSecret(TestSecretProvider provider, bool enabled = true)
    {
        return new RegisteredSecretProvider(provider, new ProviderRegistration { Name = provider.ProviderName, IsEnabled = enabled });
    }

    private static RegisteredConfigSourceProvider RegisterConfig(TestConfigProvider provider, bool enabled = true)
    {
        return new RegisteredConfigSourceProvider(provider, new ProviderRegistration { Name = provider.ProviderName, IsEnabled = enabled });
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
