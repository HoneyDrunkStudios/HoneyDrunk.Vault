using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.EventGrid.Extensions;
using HoneyDrunk.Vault.EventGrid.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Tests for <see cref="VaultEventGridServiceCollectionExtensions"/>.
/// </summary>
public sealed class VaultEventGridServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that the extension registers both invalidation handlers and that they
    /// resolve from the container with their secret-store/invalidator dependencies.
    /// </summary>
    [Fact]
    public void AddVaultEventGridInvalidation_RegistersWebhookAndFunctionHandlers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ISecretStore>());
        services.AddSingleton(Substitute.For<ISecretCacheInvalidator>());

        services.AddVaultEventGridInvalidation();

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<VaultInvalidationWebhookHandler>());
        Assert.NotNull(provider.GetService<VaultInvalidationFunctionHandler>());
    }

    /// <summary>
    /// Verifies that null services arguments are rejected.
    /// </summary>
    [Fact]
    public void AddVaultEventGridInvalidation_ThrowsArgumentNullException_OnNullServices()
    {
        Assert.Throws<ArgumentNullException>(() =>
            VaultEventGridServiceCollectionExtensions.AddVaultEventGridInvalidation(services: null!));
    }
}
