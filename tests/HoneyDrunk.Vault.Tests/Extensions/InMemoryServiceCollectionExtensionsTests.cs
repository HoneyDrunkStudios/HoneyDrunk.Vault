using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Providers.InMemory.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Tests for <see cref="InMemoryServiceCollectionExtensions"/>.
/// </summary>
public sealed class InMemoryServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that the no-arg overload wires both stores plus the resolvable composite ISecretStore.
    /// </summary>
    [Fact]
    public void AddVaultInMemory_NoArgs_RegistersStores()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddVaultInMemory();

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ISecretStore>());
        Assert.NotNull(provider.GetService<IConfigSource>());
    }

    /// <summary>
    /// Verifies that the configure overload runs and the extension does not throw
    /// when seed values are added.
    /// </summary>
    [Fact]
    public void AddVaultInMemory_WithConfigure_AcceptsSeedValues()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddVaultInMemory(options =>
        {
            options.AddSecret("api-key", "secret-value");
            options.AddConfigValue("App:Name", "Vault");
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ISecretStore>());
        Assert.NotNull(provider.GetService<IConfigSource>());
    }

    /// <summary>
    /// Verifies that null guards reject null services/configure.
    /// </summary>
    [Fact]
    public void AddVaultInMemory_ThrowsArgumentNullException_OnNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            InMemoryServiceCollectionExtensions.AddVaultInMemory(services: null!));
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddVaultInMemory(configure: null!));
    }
}
