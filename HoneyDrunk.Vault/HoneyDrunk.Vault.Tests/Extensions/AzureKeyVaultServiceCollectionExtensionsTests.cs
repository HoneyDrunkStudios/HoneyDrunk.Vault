using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Configuration;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Tests for <see cref="AzureKeyVaultServiceCollectionExtensions"/>.
/// </summary>
public sealed class AzureKeyVaultServiceCollectionExtensionsTests
{
    private static readonly Uri SampleVaultUri = new("https://kv.example.net/");

    /// <summary>
    /// Verifies that the configure overload registers ISecretStore and IConfigSource.
    /// </summary>
    [Fact]
    public void AddVaultWithAzureKeyVault_Configure_RegistersStoreAndConfigSource()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddVaultWithAzureKeyVault(options =>
        {
            options.VaultUri = SampleVaultUri;
            options.UseManagedIdentity = true;
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ISecretStore>());
        Assert.NotNull(provider.GetService<IConfigSource>());
    }

    /// <summary>
    /// Verifies that the options-overload registers when given an explicit options instance.
    /// </summary>
    [Fact]
    public void AddVaultWithAzureKeyVault_WithOptionsInstance_Registers()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddVaultWithAzureKeyVault(new AzureKeyVaultOptions { VaultUri = SampleVaultUri });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ISecretStore>());
    }

    /// <summary>
    /// Verifies that a missing VaultUri is rejected with ArgumentException.
    /// </summary>
    [Fact]
    public void AddVaultWithAzureKeyVault_ThrowsArgumentException_WhenVaultUriMissing()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            services.AddVaultWithAzureKeyVault(new AzureKeyVaultOptions()));
    }

    /// <summary>
    /// Verifies that null services/options/configure arguments are rejected.
    /// </summary>
    [Fact]
    public void AddVaultWithAzureKeyVault_ThrowsArgumentNullException_OnNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            AzureKeyVaultServiceCollectionExtensions.AddVaultWithAzureKeyVault(
                services: null!,
                configure: _ => { }));
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddVaultWithAzureKeyVault(configure: null!));
        Assert.Throws<ArgumentNullException>(() =>
            AzureKeyVaultServiceCollectionExtensions.AddVaultWithAzureKeyVault(
                services: null!,
                options: new AzureKeyVaultOptions { VaultUri = SampleVaultUri }));
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddVaultWithAzureKeyVault(options: null!));
    }
}
