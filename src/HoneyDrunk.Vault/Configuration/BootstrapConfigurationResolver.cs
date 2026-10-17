using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// Resolves bootstrap configuration values shared by Vault provider packages.
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
        ArgumentNullException.ThrowIfNull(configuration);

        var environment = configuration[aspNetCoreEnvironmentSetting] ??
            configuration[dotNetEnvironmentSetting] ??
            Environment.GetEnvironmentVariable(aspNetCoreEnvironmentSetting) ??
            Environment.GetEnvironmentVariable(dotNetEnvironmentSetting);

        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attempts to read an absolute URI from configuration or environment variables.
    /// </summary>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="settingName">The setting key name.</param>
    /// <param name="uri">When this method returns, contains the parsed URI, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a valid absolute URI was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetUri(IConfiguration configuration, string settingName, [NotNullWhen(true)] out Uri? uri)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var value = configuration[settingName];
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Environment.GetEnvironmentVariable(settingName);
        }

        var isValid = Uri.TryCreate(value, UriKind.Absolute, out var parsed);
        uri = parsed;
        return isValid;
    }
}
