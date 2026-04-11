using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Vault.Providers.AppConfiguration.Extensions;

public static class BootstrapConfigurationResolver
{
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

    public static bool IsDevelopment(IConfiguration configuration, string aspNetCoreEnvironmentSetting, string dotNetEnvironmentSetting)
    {
        var environment = configuration[aspNetCoreEnvironmentSetting] ??
            configuration[dotNetEnvironmentSetting] ??
            Environment.GetEnvironmentVariable(aspNetCoreEnvironmentSetting) ??
            Environment.GetEnvironmentVariable(dotNetEnvironmentSetting);

        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryGetEndpoint(IConfiguration configuration, string settingName, out Uri? endpoint)
    {
        var value = configuration[settingName];
        var isValid = Uri.TryCreate(value, UriKind.Absolute, out var parsed);
        endpoint = parsed;
        return isValid;
    }
}
