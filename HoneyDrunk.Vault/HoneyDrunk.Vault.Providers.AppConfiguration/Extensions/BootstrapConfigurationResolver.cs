using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Vault.Providers.AppConfiguration.Extensions;

/// <summary>
/// Resolves bootstrap configuration from the service collection or environment.
/// </summary>
public static class BootstrapConfigurationResolver
{
    /// <summary>
    /// Resolves an <see cref="IConfiguration"/> instance from the service collection or builds a fallback from environment variables.
    /// </summary>
    /// <param name="services">The service collection to inspect.</param>
    /// <returns>The resolved configuration instance.</returns>
    public static IConfiguration Resolve(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var descriptor = services.LastOrDefault(static d => d.ServiceType == typeof(IConfiguration));
        if (descriptor?.ImplementationInstance is IConfiguration configuration)
        {
            return configuration;
        }

        return new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
    }

    /// <summary>
    /// Determines whether the current environment is Development.
    /// </summary>
    /// <param name="configuration">The configuration to check.</param>
    /// <param name="aspNetCoreEnvironmentSetting">The ASP.NET Core environment variable name.</param>
    /// <param name="dotNetEnvironmentSetting">The .NET environment variable name.</param>
    /// <returns><see langword="true"/> if the environment is Development; otherwise, <see langword="false"/>.</returns>
    public static bool IsDevelopment(IConfiguration configuration, string aspNetCoreEnvironmentSetting, string dotNetEnvironmentSetting)
    {
        var environment = configuration[aspNetCoreEnvironmentSetting] ??
            configuration[dotNetEnvironmentSetting] ??
            Environment.GetEnvironmentVariable(aspNetCoreEnvironmentSetting) ??
            Environment.GetEnvironmentVariable(dotNetEnvironmentSetting);

        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to read an App Configuration endpoint URI from configuration or environment variables.
    /// </summary>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="settingName">The setting key name.</param>
    /// <param name="endpoint">When this method returns, contains the parsed endpoint URI, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a valid URI was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetEndpoint(IConfiguration configuration, string settingName, out Uri? endpoint)
    {
        var value = configuration[settingName];
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Environment.GetEnvironmentVariable(settingName);
        }

        var isValid = Uri.TryCreate(value, UriKind.Absolute, out var parsed);
        endpoint = parsed;
        return isValid;
    }
}
