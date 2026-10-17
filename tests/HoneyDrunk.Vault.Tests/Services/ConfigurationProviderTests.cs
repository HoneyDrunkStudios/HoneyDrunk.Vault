using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.Configuration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for configuration-backed secret and configuration providers.
/// </summary>
public sealed class ConfigurationProviderTests
{
    /// <summary>
    /// Verifies configuration source returns configured string values.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationConfigSource_GetConfigValueAsync_ReturnsValue_WhenPresent()
    {
        // Arrange
        var source = CreateConfigSource(new Dictionary<string, string?> { ["Feature:Name"] = "vault" });

        // Act
        var value = await source.GetConfigValueAsync("Feature:Name");

        // Assert
        Assert.Equal("vault", value);
    }

    /// <summary>
    /// Verifies missing configuration source keys throw not-found exceptions.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationConfigSource_GetConfigValueAsync_Throws_WhenMissing()
    {
        // Arrange
        var source = CreateConfigSource([]);

        // Act & Assert
        await Assert.ThrowsAsync<ConfigurationNotFoundException>(() => source.GetConfigValueAsync("missing"));
    }

    /// <summary>
    /// Verifies try-get returns null for missing string keys.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationConfigSource_TryGetConfigValueAsync_ReturnsNull_WhenMissing()
    {
        // Arrange
        var source = CreateConfigSource([]);

        // Act
        var value = await source.TryGetConfigValueAsync("missing");

        // Assert
        Assert.Null(value);
    }

    /// <summary>
    /// Verifies typed configuration values are read through Microsoft configuration binding.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationConfigSource_GetConfigValueAsync_Typed_ReturnsValue()
    {
        // Arrange
        var source = CreateConfigSource(new Dictionary<string, string?> { ["Retries"] = "5" });

        // Act
        var value = await source.GetConfigValueAsync<int>("Retries");

        // Assert
        Assert.Equal(5, value);
    }

    /// <summary>
    /// Verifies configured value-type defaults are returned instead of being mistaken for "not found".
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationConfigSource_GetConfigValueAsync_Typed_ReturnsConfiguredZero()
    {
        // Arrange — 0 is a legitimate value, not "not found".
        var source = CreateConfigSource(new Dictionary<string, string?> { ["Retries"] = "0" });

        // Act
        var value = await source.GetConfigValueAsync<int>("Retries");

        // Assert
        Assert.Equal(0, value);
    }

    /// <summary>
    /// Verifies configured boolean `false` is returned instead of being mistaken for "not found".
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationConfigSource_GetConfigValueAsync_Typed_ReturnsConfiguredFalse()
    {
        // Arrange — false is a legitimate value, not "not found".
        var source = CreateConfigSource(new Dictionary<string, string?> { ["FeatureEnabled"] = "false" });

        // Act
        var value = await source.GetConfigValueAsync<bool>("FeatureEnabled");

        // Assert
        Assert.False(value);
    }

    /// <summary>
    /// Verifies typed try-get returns the default value when conversion fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationConfigSource_TryGetConfigValueAsync_Typed_ReturnsDefault_WhenConversionFails()
    {
        // Arrange
        var source = CreateConfigSource(new Dictionary<string, string?> { ["Retries"] = "not-an-int" });

        // Act
        var value = await source.TryGetConfigValueAsync("Retries", 3);

        // Assert
        Assert.Equal(3, value);
    }

    /// <summary>
    /// Verifies configuration secret store reads secrets under the Secrets section.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationSecretStore_GetSecretAsync_ReturnsSecret_WhenPresent()
    {
        // Arrange
        var store = CreateSecretStore(new Dictionary<string, string?> { ["Secrets:ApiKey"] = "secret-value" });

        // Act
        var secret = await store.GetSecretAsync(new SecretIdentifier("ApiKey", "v1"));

        // Assert
        Assert.Equal("ApiKey", secret.Identifier.Name);
        Assert.Equal("secret-value", secret.Value);
        Assert.Equal("v1", secret.Version);
    }

    /// <summary>
    /// Verifies missing configuration secrets throw not-found exceptions.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationSecretStore_GetSecretAsync_Throws_WhenMissing()
    {
        // Arrange
        var store = CreateSecretStore([]);

        // Act & Assert
        await Assert.ThrowsAsync<SecretNotFoundException>(() => store.GetSecretAsync(new SecretIdentifier("missing")));
    }

    /// <summary>
    /// Verifies try-get wraps missing secrets as failed vault results.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationSecretStore_TryGetSecretAsync_ReturnsFailure_WhenMissing()
    {
        // Arrange
        var store = CreateSecretStore([]);

        // Act
        var result = await ((ISecretStore)store).TryGetSecretAsync(new SecretIdentifier("missing"));

        // Assert
        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// Verifies version listing reports the single configuration-backed latest version.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ConfigurationSecretStore_ListSecretVersionsAsync_ReturnsLatest_WhenPresent()
    {
        // Arrange
        var store = CreateSecretStore(new Dictionary<string, string?> { ["Secrets:ApiKey"] = "secret-value" });

        // Act
        var versions = await store.ListSecretVersionsAsync("ApiKey");

        // Assert
        var version = Assert.Single(versions);
        Assert.Equal("latest", version.Version);
    }

    /// <summary>
    /// Verifies version listing validates the secret name.
    /// </summary>
    /// <param name="secretName">The invalid secret name to test.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ConfigurationSecretStore_ListSecretVersionsAsync_Throws_WhenNameInvalid(string? secretName)
    {
        // Arrange
        var store = CreateSecretStore([]);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => store.ListSecretVersionsAsync(secretName!));
    }

    private static ConfigurationConfigSource CreateConfigSource(Dictionary<string, string?> values)
    {
        return new ConfigurationConfigSource(CreateConfiguration(values), NullLogger<ConfigurationConfigSource>.Instance);
    }

    private static ConfigurationSecretStore CreateSecretStore(Dictionary<string, string?> values)
    {
        return new ConfigurationSecretStore(CreateConfiguration(values), NullLogger<ConfigurationSecretStore>.Instance);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
