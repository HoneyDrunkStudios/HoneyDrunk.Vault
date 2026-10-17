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
    /// Adds Azure App Configuration using env-var-driven bootstrap discovery, resolving the mutable
    /// <see cref="IConfigurationManager"/> from the service collection.
    /// Reads <c>AZURE_APPCONFIG_ENDPOINT</c> and <c>HONEYDRUNK_NODE_ID</c> via <see cref="IConfiguration"/>,
    /// applies per-Node label partitioning, enables unlabeled shared fallback keys, resolves Key Vault references,
    /// and registers feature management services.
    /// </summary>
    /// <remarks>
    /// This overload requires the host builder to have registered its <see cref="IConfigurationManager"/> as an
    /// <see cref="IConfiguration"/> instance on the service collection — which <c>WebApplication.CreateBuilder</c>
    /// does, but Azure Functions' <c>FunctionsApplication.CreateBuilder</c> and the generic
    /// <c>Host.CreateApplicationBuilder</c> do <b>not</b>. For those hosts call the
    /// <see cref="AddAppConfiguration(IHoneyDrunkBuilder, IConfigurationManager, Action{AppConfigurationOptions})"/>
    /// overload and pass <c>builder.Configuration</c> directly.
    /// </remarks>
    /// <param name="builder">The HoneyDrunk builder.</param>
    /// <param name="configure">Optional bootstrap configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddAppConfiguration(
        this IHoneyDrunkBuilder builder,
        Action<AppConfigurationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configuration = BootstrapConfigurationResolver.Resolve(builder.Services);
        if (configuration is not IConfigurationManager manager)
        {
            throw new InvalidOperationException(
                "App Configuration bootstrap requires a mutable IConfigurationManager, but the host builder did " +
                "not register one as an IConfiguration instance on the service collection. Azure Functions' " +
                "FunctionsApplication.CreateBuilder and the generic Host.CreateApplicationBuilder do not register " +
                "it; call the AddAppConfiguration(IConfigurationManager) overload and pass builder.Configuration.");
        }

        return builder.AddAppConfiguration(manager, configure);
    }

    /// <summary>
    /// Adds Azure App Configuration using env-var-driven bootstrap discovery against an explicitly supplied
    /// <see cref="IConfigurationManager"/> (typically the host builder's <c>Configuration</c>).
    /// Reads <c>AZURE_APPCONFIG_ENDPOINT</c> and <c>HONEYDRUNK_NODE_ID</c>, applies per-Node label partitioning,
    /// enables unlabeled shared fallback keys, resolves Key Vault references, and registers feature management.
    /// </summary>
    /// <remarks>
    /// Use this overload for hosts that do not register the manager on the service collection — Azure Functions
    /// (<c>FunctionsApplication.CreateBuilder</c>) and the generic <c>Host.CreateApplicationBuilder</c>. Passing
    /// the manager directly avoids the service-collection lookup the parameterless overload depends on, which on
    /// those hosts falls back to an immutable env-vars-only configuration and cannot append the App Configuration
    /// source (the worker then aborts at startup).
    /// </remarks>
    /// <param name="builder">The HoneyDrunk builder.</param>
    /// <param name="configurationManager">
    /// The mutable configuration manager to read bootstrap settings from and append the App Configuration source
    /// to — typically the host builder's <c>Configuration</c>.
    /// </param>
    /// <param name="configure">Optional bootstrap configuration.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddAppConfiguration(
        this IHoneyDrunkBuilder builder,
        IConfigurationManager configurationManager,
        Action<AppConfigurationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configurationManager);

        var options = new AppConfigurationOptions();
        configure?.Invoke(options);

        if (BootstrapConfigurationResolver.TryGetEndpoint(configurationManager, AppConfigurationEndpointSetting, out var endpointUri))
        {
            var nodeId = configurationManager[NodeIdSetting]
                ?? Environment.GetEnvironmentVariable(NodeIdSetting);
            if (string.IsNullOrWhiteSpace(nodeId))
            {
                throw new InvalidOperationException(
                    $"Missing required bootstrap setting '{NodeIdSetting}'. " +
                    "HONEYDRUNK_NODE_ID is required for App Configuration label partitioning.");
            }

            var credential = options.Credential ?? new DefaultAzureCredential();
            configurationManager.AddAzureAppConfiguration(
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

        if (BootstrapConfigurationResolver.IsDevelopment(configurationManager, AspNetCoreEnvironmentSetting, DotNetEnvironmentSetting))
        {
            configurationManager.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);

            builder.Services.AddFeatureManagement();
            return builder;
        }

        throw new InvalidOperationException(
            $"Missing required bootstrap setting '{AppConfigurationEndpointSetting}'. " +
            "Deployable services must receive AZURE_APPCONFIG_ENDPOINT via environment configuration.");
    }
}
