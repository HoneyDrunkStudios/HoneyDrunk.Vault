using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;
using Microsoft.Extensions.Configuration;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Tests for Azure Key Vault bootstrap configuration resolution.
/// </summary>
public sealed class AzureKeyVaultBootstrapConfigurationResolverTests
{
    /// <summary>
    /// Verifies that TryGetKeyVaultUri returns true when a valid URI is present.
    /// </summary>
    [Fact]
    public void TryGetKeyVaultUri_ReturnsTrue_WhenUriPresent()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AZURE_KEYVAULT_URI"] = "https://kv-hd-orders-prod.vault.azure.net/",
            })
            .Build();

        var result = BootstrapConfigurationResolver.TryGetKeyVaultUri(configuration, "AZURE_KEYVAULT_URI", out var vaultUri);

        Assert.True(result);
        Assert.NotNull(vaultUri);
    }

    /// <summary>
    /// Verifies that IsDevelopment returns true for a Development environment.
    /// </summary>
    [Fact]
    public void IsDevelopment_ReturnsTrue_WhenEnvironmentIsDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
            })
            .Build();

        var isDevelopment = BootstrapConfigurationResolver.IsDevelopment(configuration, "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT");

        Assert.True(isDevelopment);
    }

    /// <summary>
    /// Verifies that TryGetKeyVaultUri returns false when the URI is missing.
    /// </summary>
    [Fact]
    public void TryGetKeyVaultUri_ReturnsFalse_WhenUriMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = BootstrapConfigurationResolver.TryGetKeyVaultUri(configuration, "AZURE_KEYVAULT_URI", out var vaultUri);

        Assert.False(result);
        Assert.Null(vaultUri);
    }

    /// <summary>
    /// Verifies that TryGetKeyVaultUri falls back to reading an environment variable.
    /// </summary>
    [Fact]
    public void TryGetKeyVaultUri_ReadsEnvironmentVariable_WhenMissingFromConfiguration()
    {
        const string setting = "AZURE_KEYVAULT_URI";
        var original = Environment.GetEnvironmentVariable(setting);

        try
        {
            Environment.SetEnvironmentVariable(setting, "https://kv-env.vault.azure.net/");
            var configuration = new ConfigurationBuilder().Build();

            var result = BootstrapConfigurationResolver.TryGetKeyVaultUri(configuration, setting, out var vaultUri);

            Assert.True(result);
            Assert.NotNull(vaultUri);
        }
        finally
        {
            Environment.SetEnvironmentVariable(setting, original);
        }
    }
}
