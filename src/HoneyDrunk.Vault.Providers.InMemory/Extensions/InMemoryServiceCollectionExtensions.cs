using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Extensions;
using HoneyDrunk.Vault.Providers.InMemory.Configuration;
using HoneyDrunk.Vault.Providers.InMemory.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Concurrent;

namespace HoneyDrunk.Vault.Providers.InMemory.Extensions;

/// <summary>
/// Extension methods for configuring in-memory vault provider.
/// </summary>
public static class InMemoryServiceCollectionExtensions
{
    /// <summary>
    /// Adds in-memory vault provider for testing and development purposes.
    /// This automatically registers the vault core services and in-memory implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultInMemory(this IServiceCollection services)
    {
        return services.AddVaultInMemory(_ => { });
    }

    /// <summary>
    /// Adds in-memory vault provider with initial values for testing and development purposes.
    /// This automatically registers the vault core services and in-memory implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action to prepopulate secrets and configuration values.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultInMemory(
        this IServiceCollection services,
        Action<InMemoryVaultOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new InMemoryVaultOptions();
        configure(options);

        // Register core vault services
        services.AddVaultCore();

        // Create shared storage dictionaries
        var secretsDictionary = new ConcurrentDictionary<string, string>(
            options.Secrets,
            StringComparer.OrdinalIgnoreCase);

        var configDictionary = new ConcurrentDictionary<string, string>(
            options.ConfigurationValues,
            StringComparer.OrdinalIgnoreCase);

        // Register in-memory provider implementations with shared dictionaries
        services.TryAddSingleton<ISecretStore>(sp =>
            new InMemorySecretStore(
                secretsDictionary,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InMemorySecretStore>>()));

        services.TryAddSingleton<IConfigSource>(sp =>
            new InMemoryConfigSource(
                configDictionary,
                sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InMemoryConfigSource>>()));

        return services;
    }
}
