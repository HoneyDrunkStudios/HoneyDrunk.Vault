using HoneyDrunk.Vault.Providers.AppConfiguration.Extensions;
using Microsoft.Extensions.Configuration;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Unit tests for App Configuration bootstrap resolution.
/// </summary>
public sealed class AppConfigurationBootstrapConfigurationResolverTests
{
    /// <summary>
    /// Verifies that TryGetEndpoint returns true when a valid endpoint is present.
    /// </summary>
    [Fact]
    public void TryGetEndpoint_ReturnsTrue_WhenEndpointPresent()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AZURE_APPCONFIG_ENDPOINT"] = "https://appcs-hd-shared-prod.azconfig.io",
            })
            .Build();

        var result = BootstrapConfigurationResolver.TryGetEndpoint(configuration, "AZURE_APPCONFIG_ENDPOINT", out var endpoint);

        Assert.True(result);
        Assert.NotNull(endpoint);
    }

    /// <summary>
    /// Verifies that IsDevelopment returns true for a Development environment.
    /// </summary>
    [Fact]
    public void IsDevelopment_ReturnsTrue_WhenEnvironmentIsDevelopment()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DOTNET_ENVIRONMENT"] = "Development",
            })
            .Build();

        var isDevelopment = BootstrapConfigurationResolver.IsDevelopment(configuration, "ASPNETCORE_ENVIRONMENT", "DOTNET_ENVIRONMENT");

        Assert.True(isDevelopment);
    }

    /// <summary>
    /// Verifies that TryGetEndpoint returns false when the endpoint is missing.
    /// </summary>
    [Fact]
    public void TryGetEndpoint_ReturnsFalse_WhenEndpointMissing()
    {
        var configuration = new ConfigurationBuilder().Build();

        var result = BootstrapConfigurationResolver.TryGetEndpoint(configuration, "AZURE_APPCONFIG_ENDPOINT", out var endpoint);

        Assert.False(result);
        Assert.Null(endpoint);
    }

    /// <summary>
    /// Verifies that TryGetEndpoint falls back to reading an environment variable.
    /// </summary>
    [Fact]
    public void TryGetEndpoint_ReadsEnvironmentVariable_WhenMissingFromConfiguration()
    {
        const string setting = "AZURE_APPCONFIG_ENDPOINT";
        var original = Environment.GetEnvironmentVariable(setting);

        try
        {
            Environment.SetEnvironmentVariable(setting, "https://appcs-env.azconfig.io");
            var configuration = new ConfigurationBuilder().Build();

            var result = BootstrapConfigurationResolver.TryGetEndpoint(configuration, setting, out var endpoint);

            Assert.True(result);
            Assert.NotNull(endpoint);
        }
        finally
        {
            Environment.SetEnvironmentVariable(setting, original);
        }
    }
}
