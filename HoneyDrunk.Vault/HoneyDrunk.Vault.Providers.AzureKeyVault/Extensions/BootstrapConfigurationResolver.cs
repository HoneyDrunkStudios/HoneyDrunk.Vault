using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using SharedBootstrapConfigurationResolver = HoneyDrunk.Vault.Configuration.BootstrapConfigurationResolver;

namespace HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;

/// <summary>
/// Resolves Azure Key Vault bootstrap settings.
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
        return SharedBootstrapConfigurationResolver.Resolve(services);
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
        return SharedBootstrapConfigurationResolver.IsDevelopment(configuration, aspNetCoreEnvironmentSetting, dotNetEnvironmentSetting);
    }

    /// <summary>
    /// Attempts to read a Key Vault URI from configuration or environment variables.
    /// </summary>
    /// <param name="configuration">The configuration to read from.</param>
    /// <param name="settingName">The setting key name.</param>
    /// <param name="vaultUri">When this method returns, contains the parsed vault URI, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a valid URI was found; otherwise, <see langword="false"/>.</returns>
    public static bool TryGetKeyVaultUri(IConfiguration configuration, string settingName, [NotNullWhen(true)] out Uri? vaultUri)
    {
        return SharedBootstrapConfigurationResolver.TryGetUri(configuration, settingName, out vaultUri);
    }
}
