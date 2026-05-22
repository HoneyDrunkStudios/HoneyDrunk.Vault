using Azure.Core;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.Providers.AppConfiguration.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Tests for <see cref="AppConfigurationBootstrapExtensions"/>.
/// </summary>
public sealed class AppConfigurationBootstrapExtensionsTests
{
    /// <summary>
    /// Verifies that Azure App Configuration source is registered when an endpoint is present.
    /// </summary>
    [Fact]
    public void AddAppConfiguration_RegistersAzureSource_WhenEndpointPresent()
    {
        var services = new ServiceCollection();
        using var configuration = new ConfigurationManager();
        configuration["AZURE_APPCONFIG_ENDPOINT"] = "https://appcs-test.azconfig.io";
        configuration["HONEYDRUNK_NODE_ID"] = "orders";
        services.AddSingleton<IConfiguration>(configuration);

        var builder = CreateBuilder(services);

        var result = builder.AddAppConfiguration(o =>
        {
            o.Optional = true;
            o.Credential = new StaticTokenCredential();
            o.StartupTimeout = TimeSpan.FromMilliseconds(1);
        });

        Assert.Same(builder, result);
        Assert.Contains(configuration.Sources, s => s.GetType().Name.Contains("AzureAppConfiguration", StringComparison.Ordinal));
        Assert.Contains(services, d => d.ServiceType.FullName == "Microsoft.FeatureManagement.IFeatureManager");
    }

    /// <summary>
    /// Verifies that a Development JSON fallback is added when in Development without an endpoint.
    /// </summary>
    [Fact]
    public void AddAppConfiguration_AddsDevelopmentJson_WhenDevelopmentAndEndpointMissing()
    {
        var services = new ServiceCollection();
        using var configuration = new ConfigurationManager();
        configuration["DOTNET_ENVIRONMENT"] = "Development";
        services.AddSingleton<IConfiguration>(configuration);

        var builder = CreateBuilder(services);

        builder.AddAppConfiguration();

        Assert.Contains(configuration.Sources, s => s is JsonConfigurationSource json && json.Path == "appsettings.Development.json");
    }

    /// <summary>
    /// Verifies that an exception is thrown when not in Development and no endpoint is configured.
    /// </summary>
    [Fact]
    public void AddAppConfiguration_Throws_WhenNonDevelopmentAndEndpointMissing()
    {
        var services = new ServiceCollection();
        using var configuration = new ConfigurationManager();
        configuration["DOTNET_ENVIRONMENT"] = "Production";
        services.AddSingleton<IConfiguration>(configuration);

        var builder = CreateBuilder(services);

        Assert.Throws<InvalidOperationException>(() => builder.AddAppConfiguration());
    }

    private static IHoneyDrunkBuilder CreateBuilder(IServiceCollection services)
    {
        var builder = Substitute.For<IHoneyDrunkBuilder>();
        builder.Services.Returns(services);
        return builder;
    }

    private sealed class StaticTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return CreateToken();
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(CreateToken());
        }

        private static AccessToken CreateToken()
        {
            return new AccessToken("fake-token", DateTimeOffset.MaxValue);
        }
    }
}
