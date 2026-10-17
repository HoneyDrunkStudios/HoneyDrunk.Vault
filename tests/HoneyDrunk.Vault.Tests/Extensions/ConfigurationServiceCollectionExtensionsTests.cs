using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Providers.Configuration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Tests for <see cref="ConfigurationServiceCollectionExtensions"/>.
/// </summary>
public sealed class ConfigurationServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that the extension registers ISecretStore + IConfigSource and persists
    /// the supplied IConfiguration in the container.
    /// </summary>
    [Fact]
    public void AddVaultWithConfiguration_RegistersStoreAndConfigSource()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Secrets:ApiKey"] = "secret-value",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddVaultWithConfiguration(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ISecretStore>());
        Assert.NotNull(provider.GetService<IConfigSource>());
        Assert.Same(configuration, provider.GetRequiredService<IConfiguration>());
    }

    /// <summary>
    /// Verifies that null services/configuration arguments are rejected.
    /// </summary>
    [Fact]
    public void AddVaultWithConfiguration_ThrowsArgumentNullException_OnNullArguments()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<ArgumentNullException>(() =>
            ConfigurationServiceCollectionExtensions.AddVaultWithConfiguration(services: null!, configuration));
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddVaultWithConfiguration(configuration: null!));
    }
}
