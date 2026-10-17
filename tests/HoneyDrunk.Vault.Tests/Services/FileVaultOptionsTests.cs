using HoneyDrunk.Vault.Providers.File.Configuration;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="FileVaultOptions"/>.
/// </summary>
public sealed class FileVaultOptionsTests
{
    /// <summary>
    /// Verifies that default values are set correctly.
    /// </summary>
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var options = new FileVaultOptions();

        // Assert
        Assert.Equal("secrets.json", options.SecretsFilePath);
        Assert.Equal("config.json", options.ConfigFilePath);
        Assert.Null(options.EncryptionKeyEnvironmentVariable);
        Assert.Null(options.EncryptionKeyFilePath);
        Assert.True(options.WatchForChanges);
        Assert.True(options.CreateIfNotExists);
    }

    /// <summary>
    /// Verifies that properties can be set.
    /// </summary>
    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var options = new FileVaultOptions
        {
            // Act
            SecretsFilePath = "/custom/secrets.json",
            ConfigFilePath = "/custom/config.json",
            EncryptionKeyEnvironmentVariable = "MY_KEY",
            EncryptionKeyFilePath = "/path/to/key",
            WatchForChanges = false,
            CreateIfNotExists = false
        };

        // Assert
        Assert.Equal("/custom/secrets.json", options.SecretsFilePath);
        Assert.Equal("/custom/config.json", options.ConfigFilePath);
        Assert.Equal("MY_KEY", options.EncryptionKeyEnvironmentVariable);
        Assert.Equal("/path/to/key", options.EncryptionKeyFilePath);
        Assert.False(options.WatchForChanges);
        Assert.False(options.CreateIfNotExists);
    }
}
