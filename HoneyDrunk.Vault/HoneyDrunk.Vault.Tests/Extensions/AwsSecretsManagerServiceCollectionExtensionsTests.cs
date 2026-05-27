using Amazon.SecretsManager;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Providers.Aws.Extensions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Tests for <see cref="AwsSecretsManagerServiceCollectionExtensions"/>.
/// </summary>
public sealed class AwsSecretsManagerServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that the no-arg overload registers ISecretStore + ISecretProvider.
    /// </summary>
    [Fact]
    public void AddVaultWithAwsSecretsManager_NoArgs_RegistersStores()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAmazonSecretsManager>());

        services.AddVaultWithAwsSecretsManager();

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ISecretStore>());
        Assert.NotNull(provider.GetService<ISecretProvider>());
    }

    /// <summary>
    /// Verifies that the configure overload threads options through.
    /// </summary>
    [Fact]
    public void AddVaultWithAwsSecretsManager_Configure_RegistersStores()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<IAmazonSecretsManager>());

        services.AddVaultWithAwsSecretsManager(options =>
        {
            options.Region = "us-east-1";
            options.SecretPrefix = "test/";
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ISecretStore>());
    }

    /// <summary>
    /// Verifies that the client-factory overload registers a custom client.
    /// </summary>
    [Fact]
    public void AddVaultWithAwsSecretsManager_ClientFactory_RegistersStores()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var client = Substitute.For<IAmazonSecretsManager>();

        services.AddVaultWithAwsSecretsManager(_ => client, options =>
        {
            options.Region = "us-east-1";
        });

        using var provider = services.BuildServiceProvider();
        Assert.Same(client, provider.GetRequiredService<IAmazonSecretsManager>());
        Assert.NotNull(provider.GetService<ISecretStore>());
    }

    /// <summary>
    /// Verifies that null arguments are rejected.
    /// </summary>
    [Fact]
    public void AddVaultWithAwsSecretsManager_ThrowsArgumentNullException_OnNullArguments()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            AwsSecretsManagerServiceCollectionExtensions.AddVaultWithAwsSecretsManager(
                services: null!));
        Assert.Throws<ArgumentNullException>(() =>
            services.AddVaultWithAwsSecretsManager(configure: null!));
        Assert.Throws<ArgumentNullException>(() =>
            AwsSecretsManagerServiceCollectionExtensions.AddVaultWithAwsSecretsManager(
                services: null!,
                clientFactory: _ => Substitute.For<IAmazonSecretsManager>()));
        Assert.Throws<ArgumentNullException>(() =>
            services.AddVaultWithAwsSecretsManager(clientFactory: null!));
    }
}
