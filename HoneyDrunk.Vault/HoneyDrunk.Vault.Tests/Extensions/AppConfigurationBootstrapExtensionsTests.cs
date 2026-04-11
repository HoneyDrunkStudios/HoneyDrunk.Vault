using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.Providers.AppConfiguration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HoneyDrunk.Vault.Tests.Extensions;

public sealed class AppConfigurationBootstrapExtensionsTests
{
    [Fact]
    public void AddAppConfiguration_RegistersAzureSource_WhenEndpointPresent()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();
        configuration["AZURE_APPCONFIG_ENDPOINT"] = "https://appcs-test.azconfig.io";
        configuration["HONEYDRUNK_NODE_ID"] = "orders";
        services.AddSingleton<IConfiguration>(configuration);

        var builder = CreateBuilder(services);

        var result = builder.AddAppConfiguration();

        Assert.Same(builder, result);
        Assert.Contains(configuration.Sources, s => s.GetType().Name.Contains("AzureAppConfiguration", StringComparison.Ordinal));
        Assert.Contains(services, d => d.ServiceType.FullName == "Microsoft.FeatureManagement.IFeatureManager");
    }

    [Fact]
    public void AddAppConfiguration_AddsDevelopmentJson_WhenDevelopmentAndEndpointMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();
        configuration["DOTNET_ENVIRONMENT"] = "Development";
        services.AddSingleton<IConfiguration>(configuration);

        var builder = CreateBuilder(services);

        builder.AddAppConfiguration();

        Assert.Contains(configuration.Sources, s => s is JsonConfigurationSource json && json.Path == "appsettings.Development.json");
    }

    [Fact]
    public void AddAppConfiguration_Throws_WhenNonDevelopmentAndEndpointMissing()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();
        configuration["DOTNET_ENVIRONMENT"] = "Production";
        services.AddSingleton<IConfiguration>(configuration);

        var builder = CreateBuilder(services);

        Assert.Throws<InvalidOperationException>(() => builder.AddAppConfiguration());
    }

    private static IHoneyDrunkBuilder CreateBuilder(IServiceCollection services)
    {
        var builderMock = new Mock<IHoneyDrunkBuilder>();
        builderMock.SetupGet(static b => b.Services).Returns(services);
        return builderMock.Object;
    }
}
