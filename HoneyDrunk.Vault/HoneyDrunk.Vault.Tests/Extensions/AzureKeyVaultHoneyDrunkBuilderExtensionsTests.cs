using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;
using HoneyDrunk.Vault.Providers.File.Configuration;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Tests for <see cref="AzureKeyVaultHoneyDrunkBuilderExtensions"/>.
/// </summary>
public sealed class AzureKeyVaultHoneyDrunkBuilderExtensionsTests
{
    /// <summary>
    /// Verifies that Azure providers are registered when the Key Vault URI is present.
    /// </summary>
    [Fact]
    public void AddVaultWithAzureKeyVaultBootstrap_RegistersAzureProviders_WhenUriPresent()
    {
        var services = new ServiceCollection();
        using var configuration = new ConfigurationManager();
        configuration["AZURE_KEYVAULT_URI"] = "https://kv-test.vault.azure.net/";
        services.AddSingleton<IConfiguration>(configuration);

        var builder = CreateBuilder(services);

        var result = builder.AddVaultWithAzureKeyVaultBootstrap();

        Assert.Same(builder, result);
        Assert.Contains(services, d => d.ServiceType == typeof(RegisteredSecretProvider));
        Assert.Contains(services, d => d.ServiceType == typeof(RegisteredConfigSourceProvider));
    }

    /// <summary>
    /// Verifies that file-based fallback providers are registered in Development when no URI is set.
    /// </summary>
    [Fact]
    public void AddVaultWithAzureKeyVaultBootstrap_RegistersFileFallback_WhenDevelopmentAndUriMissing()
    {
        var services = new ServiceCollection();
        using var configuration = new ConfigurationManager();
        configuration["ASPNETCORE_ENVIRONMENT"] = "Development";
        services.AddSingleton<IConfiguration>(configuration);

        var builder = CreateBuilder(services);

        builder.AddVaultWithAzureKeyVaultBootstrap(options =>
        {
            options.DevelopmentSecretsFilePath = "secrets/dev-secrets.json";
        });

        Assert.Contains(services, d => d.ServiceType == typeof(IOptions<FileVaultOptions>));
        Assert.Contains(services, d => d.ServiceType == typeof(RegisteredSecretProvider));
        Assert.Contains(services, d => d.ServiceType == typeof(RegisteredConfigSourceProvider));
    }

    /// <summary>
    /// Verifies that an exception is thrown when not in Development and no URI is configured.
    /// </summary>
    [Fact]
    public void AddVaultWithAzureKeyVaultBootstrap_Throws_WhenNonDevelopmentAndUriMissing()
    {
        var services = new ServiceCollection();
        using var configuration = new ConfigurationManager();
        configuration["ASPNETCORE_ENVIRONMENT"] = "Production";
        services.AddSingleton<IConfiguration>(configuration);

        var builder = CreateBuilder(services);

        Assert.Throws<InvalidOperationException>(() => builder.AddVaultWithAzureKeyVaultBootstrap());
    }

    private static IHoneyDrunkBuilder CreateBuilder(IServiceCollection services)
    {
        var builderMock = new Mock<IHoneyDrunkBuilder>();
        builderMock.SetupGet(static b => b.Services).Returns(services);
        return builderMock.Object;
    }
}
