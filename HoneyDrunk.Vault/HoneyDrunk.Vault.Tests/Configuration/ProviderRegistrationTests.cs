using HoneyDrunk.Vault.Configuration;

namespace HoneyDrunk.Vault.Tests.Configuration;

/// <summary>
/// Tests for <see cref="ProviderRegistration"/>.
/// </summary>
public sealed class ProviderRegistrationTests
{
    /// <summary>
    /// Verifies that default values are set correctly.
    /// </summary>
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var registration = new ProviderRegistration();

        // Assert
        Assert.Equal(string.Empty, registration.Name);
        Assert.Equal(ProviderType.File, registration.ProviderType);
        Assert.Empty(registration.Settings);
        Assert.True(registration.IsEnabled);
        Assert.Equal(0, registration.Priority);
    }

    /// <summary>
    /// Verifies that properties can be set.
    /// </summary>
    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var registration = new ProviderRegistration
        {
            // Act
            Name = "azure-kv",
            ProviderType = ProviderType.AzureKeyVault,
            IsEnabled = false,
            Priority = 10
        };

        // Assert
        Assert.Equal("azure-kv", registration.Name);
        Assert.Equal(ProviderType.AzureKeyVault, registration.ProviderType);
        Assert.False(registration.IsEnabled);
        Assert.Equal(10, registration.Priority);
    }

    /// <summary>
    /// Verifies that settings dictionary works correctly.
    /// </summary>
    [Fact]
    public void Settings_WorksCorrectly()
    {
        // Arrange
        var registration = new ProviderRegistration();

        // Act
        registration.Settings["vaultUri"] = "https://my-vault.vault.azure.net";
        registration.Settings["tenantId"] = "my-tenant-id";

        // Assert
        Assert.Equal(2, registration.Settings.Count);
        Assert.Equal("https://my-vault.vault.azure.net", registration.Settings["vaultUri"]);
        Assert.Equal("my-tenant-id", registration.Settings["tenantId"]);
    }

    /// <summary>
    /// Verifies that settings dictionary is case-insensitive.
    /// </summary>
    [Fact]
    public void Settings_AreCaseInsensitive()
    {
        // Arrange
        var registration = new ProviderRegistration();

        // Act
        registration.Settings["VaultUri"] = "https://my-vault.vault.azure.net";

        // Assert
        Assert.Equal("https://my-vault.vault.azure.net", registration.Settings["vaulturi"]);
        Assert.Equal("https://my-vault.vault.azure.net", registration.Settings["VAULTURI"]);
    }
}
