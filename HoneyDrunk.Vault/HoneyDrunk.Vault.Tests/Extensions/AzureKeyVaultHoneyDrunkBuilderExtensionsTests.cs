using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.Services;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;
using HoneyDrunk.Vault.Providers.File.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace HoneyDrunk.Vault.Tests.Extensions;

public sealed class AzureKeyVaultHoneyDrunkBuilderExtensionsTests
{
    [Fact]
    public void AddVaultWithAzureKeyVaultBootstrap_RegistersAzureProviders_WhenUriPresent()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();
        configuration["AZURE_KEYVAULT_URI"] = "https://kv-test.vault.azure.net/";
        services.AddSingleton<IConfiguration>(configuration);

        var builder = CreateBuilder(services);

        var result = builder.AddVaultWithAzureKeyVaultBootstrap();

        Assert.Same(builder, result);
        Assert.Contains(services, d => d.ServiceType == typeof(RegisteredSecretProvider));
        Assert.Contains(services, d => d.ServiceType == typeof(RegisteredConfigSourceProvider));
    }

    [Fact]
    public void AddVaultWithAzureKeyVaultBootstrap_RegistersFileFallback_WhenDevelopmentAndUriMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();
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

    [Fact]
    public void AddVaultWithAzureKeyVaultBootstrap_Throws_WhenNonDevelopmentAndUriMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();
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
