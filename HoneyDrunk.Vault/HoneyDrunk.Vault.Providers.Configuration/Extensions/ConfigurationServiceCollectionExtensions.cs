using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Extensions;
using HoneyDrunk.Vault.Providers.Configuration.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Vault.Providers.Configuration.Extensions;

/// <summary>
/// Extension methods for configuring configuration-based vault provider.
/// </summary>
public static class ConfigurationServiceCollectionExtensions
{
    /// <summary>
    /// Adds configuration-based vault provider with the specified configuration.
    /// This automatically registers the vault core services and configuration-based implementations.
    /// This is primarily intended for local development and simple applications.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultWithConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Register core vault services
        services.AddVaultCore();

        // Register configuration instance if not already registered
        services.TryAddSingleton(configuration);

        // Register configuration-based provider implementations
        services.TryAddSingleton<ISecretStore, ConfigurationSecretStore>();
        services.TryAddSingleton<IConfigSource, ConfigurationConfigSource>();

        return services;
    }
}
