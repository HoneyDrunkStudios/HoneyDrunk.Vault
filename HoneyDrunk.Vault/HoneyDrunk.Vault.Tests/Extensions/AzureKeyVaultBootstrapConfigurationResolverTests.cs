using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;
using Microsoft.Extensions.Configuration;

namespace HoneyDrunk.Vault.Tests.Extensions;

public sealed class AzureKeyVaultBootstrapConfigurationResolverTests
{
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

    [Fact]
    public void TryGetKeyVaultUri_ReturnsFalse_WhenUriMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = BootstrapConfigurationResolver.TryGetKeyVaultUri(configuration, "AZURE_KEYVAULT_URI", out var vaultUri);

        Assert.False(result);
        Assert.Null(vaultUri);
    }
}
