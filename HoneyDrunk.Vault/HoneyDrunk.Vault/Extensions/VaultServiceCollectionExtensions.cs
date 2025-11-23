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
    /// Provider-specific extension methods should be used to register ISecretStore and IConfigSource implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IVaultClient, VaultClient>();

        return services;
    }
}
