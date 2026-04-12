using Azure.Identity;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.Providers.AppConfiguration.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace HoneyDrunk.Vault.Providers.AppConfiguration.Extensions;

/// <summary>
/// Bootstrap extensions for Azure App Configuration.
/// </summary>
public static class AppConfigurationBootstrapExtensions
{
    private const string AppConfigurationEndpointSetting = "AZURE_APPCONFIG_ENDPOINT";
    private const string NodeIdSetting = "HONEYDRUNK_NODE_ID";
    private const string AspNetCoreEnvironmentSetting = "ASPNETCORE_ENVIRONMENT";
    private const string DotNetEnvironmentSetting = "DOTNET_ENVIRONMENT";

    /// <summary>
    /// Adds Azure App Configuration using env-var-driven bootstrap discovery.
    /// Reads <c>AZURE_APPCONFIG_ENDPOINT</c> and <c>HONEYDRUNK_NODE_ID</c> via <see cref="IConfiguration"/>,
    /// applies per-Node label partitioning, enables unlabeled shared fallback keys, resolves Key Vault references,
    /// and registers feature management services.
    /// </summary>
    /// <param name="builder">The HoneyDrunk builder.</param>
    /// <param name="configure">Optional bootstrap configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddAppConfiguration(
        this IHoneyDrunkBuilder builder,
        Action<AppConfigurationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new AppConfigurationOptions();
        configure?.Invoke(options);

        var configuration = BootstrapConfigurationResolver.Resolve(builder.Services);
        if (BootstrapConfigurationResolver.TryGetEndpoint(configuration, AppConfigurationEndpointSetting, out var endpointUri))
        {
            var nodeId = configuration[NodeIdSetting];
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new InvalidOperationException(
                    $"Missing required bootstrap setting '{NodeIdSetting}'. " +
                    "HONEYDRUNK_NODE_ID is required for App Configuration label partitioning.");
            }

            if (configuration is not IConfigurationManager manager)
            {
                throw new InvalidOperationException(
                    "App Configuration bootstrap requires a mutable IConfigurationManager instance on the service collection.");
            }

            var credential = options.Credential ?? new DefaultAzureCredential();
            manager.AddAzureAppConfiguration(
                appConfigOptions =>
                {
                    appConfigOptions.Connect(endpointUri, credential);
                    appConfigOptions.Select(KeyFilter.Any, nodeId);

                    if (options.IncludeUnlabeledKeys)
                    {
                        appConfigOptions.Select(KeyFilter.Any, LabelFilter.Null);
                    }

                    appConfigOptions.ConfigureKeyVault(keyVault => keyVault.SetCredential(credential));

                    if (options.StartupTimeout.HasValue)
                    {
                        appConfigOptions.ConfigureStartupOptions(startup => startup.Timeout = options.StartupTimeout.Value);
                    }
                },
                optional: options.Optional);

            builder.Services.AddAzureAppConfiguration();
            builder.Services.AddFeatureManagement();
            return builder;
        }

        if (BootstrapConfigurationResolver.IsDevelopment(configuration, AspNetCoreEnvironmentSetting, DotNetEnvironmentSetting))
        {
            if (configuration is not IConfigurationManager manager)
            {
                throw new InvalidOperationException(
                    "Development bootstrap requires a mutable IConfigurationManager to apply appsettings.Development.json fallback.");
            }

            manager.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

            builder.Services.AddFeatureManagement();
            return builder;
        }

        throw new InvalidOperationException(
            $"Missing required bootstrap setting '{AppConfigurationEndpointSetting}'. " +
            "Deployable services must receive AZURE_APPCONFIG_ENDPOINT via environment configuration.");
    }
}
