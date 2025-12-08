using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Vault.Extensions;

/// <summary>
/// Extension methods for configuring vault services.
/// </summary>
public static class VaultServiceCollectionExtensions
{
    /// <summary>
    /// Adds core vault services to the service collection.
    /// This method registers the VaultClient orchestrator but does not register any provider implementations.
    /// Provider-specific extension methods should be used to register ISecretStore and IConfigSource/IConfigProvider implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IVaultClient, VaultClient>();

        // Register IConfigProvider as a wrapper around IConfigSource for new code
        services.TryAddSingleton<IConfigProvider>(sp =>
        {
            var configSource = sp.GetService<IConfigSource>();
            if (configSource is IConfigProvider provider)
            {
                return provider;
            }

            // Wrap IConfigSource in an adapter if it doesn't implement IConfigProvider
            return configSource != null
                ? new ConfigSourceAdapter(configSource)
                : throw new InvalidOperationException("No IConfigSource or IConfigProvider registered");
        });

        return services;
    }
}
