using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Extensions;
using HoneyDrunk.Vault.Providers.File.Configuration;
using HoneyDrunk.Vault.Providers.File.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Vault.Providers.File.Extensions;

/// <summary>
/// Extension methods for configuring file-based vault provider.
/// </summary>
public static class FileVaultServiceCollectionExtensions
{
    /// <summary>
    /// Adds file-based vault provider for development and local testing.
    /// This automatically registers the vault core services and file-based implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultWithFile(this IServiceCollection services)
    {
        return services.AddVaultWithFile(_ => { });
    }

    /// <summary>
    /// Adds file-based vault provider with the specified options.
    /// This automatically registers the vault core services and file-based implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action for file vault options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultWithFile(
        this IServiceCollection services,
        Action<FileVaultOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new FileVaultOptions();
        configure(options);

        // Register options
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        // Register core vault services
        services.AddVaultCore();

        // Register file-based provider implementations
        services.TryAddSingleton<ISecretStore, FileSecretStore>();
        services.TryAddSingleton<ISecretProvider, FileSecretStore>();
        services.TryAddSingleton<IConfigSource, FileConfigSource>();
        services.TryAddSingleton<IConfigProvider, FileConfigSource>();

        return services;
    }
}
