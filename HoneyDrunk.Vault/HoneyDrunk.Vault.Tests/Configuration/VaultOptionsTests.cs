using HoneyDrunk.Vault.Configuration;

namespace HoneyDrunk.Vault.Tests.Configuration;

/// <summary>
/// Tests for <see cref="VaultOptions"/>.
/// </summary>
public sealed class VaultOptionsTests
{
    /// <summary>
    /// Verifies that AddProvider registers a provider correctly.
    /// </summary>
    [Fact]
    public void AddProvider_RegistersProvider()
    {
        // Arrange
        var options = new VaultOptions();

        // Act
        options.AddProvider("test-provider", reg =>
        {
            reg.Priority = 10;
            reg.IsEnabled = true;
        });

        // Assert
        Assert.True(options.Providers.ContainsKey("test-provider"));
        Assert.Equal(10, options.Providers["test-provider"].Priority);
    }

    /// <summary>
    /// Verifies that AddFileProvider registers with file provider type.
    /// </summary>
    [Fact]
    public void AddFileProvider_RegistersWithFileProviderType()
    {
        // Arrange
        var options = new VaultOptions();

        // Act
        options.AddFileProvider(opt =>
        {
            opt.FilePath = "secrets/dev.json";
        });

        // Assert
        Assert.True(options.Providers.ContainsKey("file"));
        Assert.Equal(ProviderType.File, options.Providers["file"].ProviderType);
    }

    /// <summary>
    /// Verifies that AddAzureKeyVaultProvider registers correctly.
    /// </summary>
    [Fact]
    public void AddAzureKeyVaultProvider_RegistersCorrectly()
    {
        // Arrange
        var options = new VaultOptions();
        const string vaultUri = "https://my-vault.vault.azure.net/";

        // Act
        options.AddAzureKeyVaultProvider(opt =>
        {
            opt.VaultUri = new Uri(vaultUri);
        });

        // Assert
        Assert.True(options.Providers.ContainsKey("azure-keyvault"));
    }

    /// <summary>
    /// Verifies that AddAwsSecretsManagerProvider registers correctly.
    /// </summary>
    [Fact]
    public void AddAwsSecretsManagerProvider_RegistersCorrectly()
    {
        // Arrange
        var options = new VaultOptions();

        // Act
        options.AddAwsSecretsManagerProvider(opt =>
        {
            opt.Region = "us-east-1";
        });

        // Assert
        Assert.True(options.Providers.ContainsKey("aws-secretsmanager"));
    }

    /// <summary>
    /// Verifies that AddInMemoryProvider registers correctly.
    /// </summary>
    [Fact]
    public void AddInMemoryProvider_RegistersCorrectly()
    {
        // Arrange
        var options = new VaultOptions();

        // Act
        options.AddInMemoryProvider(opt =>
        {
            opt.Secrets["key1"] = "value1";
            opt.Secrets["key2"] = "value2";
        });

        // Assert
        Assert.True(options.Providers.ContainsKey("in-memory"));
    }

    /// <summary>
    /// Verifies that DefaultProvider can be set.
    /// </summary>
    [Fact]
    public void DefaultProvider_CanBeSet()
    {
        // Arrange
        var options = new VaultOptions
        {
            DefaultProvider = "azure-keyvault",
        };

        // Assert
        Assert.Equal("azure-keyvault", options.DefaultProvider);
    }

    /// <summary>
    /// Verifies that CacheOptions has sensible defaults.
    /// </summary>
    [Fact]
    public void CacheOptions_HasSensibleDefaults()
    {
        // Arrange
        var options = new VaultOptions();

        // Assert
        Assert.True(options.Cache.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Cache.DefaultTtl);
        Assert.Equal(1000, options.Cache.MaxSize);
    }

    /// <summary>
    /// Verifies that ResilienceOptions has sensible defaults.
    /// </summary>
    [Fact]
    public void ResilienceOptions_HasSensibleDefaults()
    {
        // Arrange
        var options = new VaultOptions();

        // Assert
        Assert.True(options.Resilience.RetryEnabled);
        Assert.Equal(3, options.Resilience.MaxRetryAttempts);
        Assert.True(options.Resilience.CircuitBreakerEnabled);
        Assert.Equal(5, options.Resilience.FailureThreshold);
    }

    /// <summary>
    /// Verifies that WarmupKeys can be populated.
    /// </summary>
    [Fact]
    public void WarmupKeys_CanBePopulated()
    {
        // Arrange
        var options = new VaultOptions();

        // Act
        options.WarmupKeys.Add("secret1");
        options.WarmupKeys.Add("secret2");

        // Assert
        Assert.Contains("secret1", options.WarmupKeys);
        Assert.Contains("secret2", options.WarmupKeys);
    }
}
