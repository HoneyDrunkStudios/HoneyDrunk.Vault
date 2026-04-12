using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Resilience;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Extensions;

/// <summary>
/// Extension methods for configuring vault services.
/// </summary>
public static class VaultServiceCollectionExtensions
{
    /// <summary>
    /// Adds core vault services to the service collection.
    /// This method registers the VaultClient orchestrator, resilience pipeline, and composite stores.
    /// Provider-specific extension methods should be used to register provider implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register resilience pipeline factory
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<VaultOptions>>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<ResiliencePipelineFactory>>();
            return new ResiliencePipelineFactory(options.Value.Resilience, logger);
        });

        // Register cache + invalidation contract so webhook hosts can resolve explicit invalidation without cache internals.
        services.TryAddSingleton<SecretCache>();
        services.TryAddSingleton<ISecretCacheInvalidator>(sp => sp.GetRequiredService<SecretCache>());

        // Register composite secret store (wraps all registered providers)
        services.TryAddSingleton<CompositeSecretStore>();

        // Register composite config source (wraps all registered providers)
        services.TryAddSingleton<CompositeConfigSource>();

        // Register caching secret store (wraps composite)
        services.TryAddSingleton<CachingSecretStore>(sp =>
        {
            var composite = sp.GetRequiredService<CompositeSecretStore>();
            var cache = sp.GetRequiredService<SecretCache>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachingSecretStore>>();
            return new CachingSecretStore(composite, cache, logger);
        });

        // Register ISecretStore as the caching store (cache wraps composite, composite wraps providers)
        services.TryAddSingleton<ISecretStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<VaultOptions>>();

            if (options.Value.Cache.Enabled)
            {
                return sp.GetRequiredService<CachingSecretStore>();
            }

            return sp.GetRequiredService<CompositeSecretStore>();
        });

        // Register IConfigSource and IConfigProvider as the composite
        services.TryAddSingleton<IConfigSource>(sp => sp.GetRequiredService<CompositeConfigSource>());
        services.TryAddSingleton<IConfigProvider>(sp => sp.GetRequiredService<CompositeConfigSource>());

        // Register VaultClient orchestrator
        services.TryAddSingleton<IVaultClient, VaultClient>();

        return services;
    }

    /// <summary>
    /// Registers a secret provider with its registration metadata.
    /// </summary>
    /// <typeparam name="TProvider">The provider type implementing ISecretProvider.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="registration">The provider registration metadata.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSecretProvider<TProvider>(
        this IServiceCollection services,
        ProviderRegistration registration)
        where TProvider : class, ISecretProvider
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(registration);

        services.AddSingleton<TProvider>();
        services.AddSingleton(sp =>
        {
            var provider = sp.GetRequiredService<TProvider>();
            return new RegisteredSecretProvider(provider, registration);
        });

        return services;
    }

    /// <summary>
    /// Registers a secret provider with its registration metadata using a factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The provider factory.</param>
    /// <param name="registration">The provider registration metadata.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSecretProvider(
        this IServiceCollection services,
        Func<IServiceProvider, ISecretProvider> factory,
        ProviderRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(registration);

        services.AddSingleton(sp =>
        {
            var provider = factory(sp);
            return new RegisteredSecretProvider(provider, registration);
        });

        return services;
    }

    /// <summary>
    /// Registers a configuration source provider with its registration metadata.
    /// </summary>
    /// <typeparam name="TProvider">The provider type implementing IConfigSourceProvider.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="registration">The provider registration metadata.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfigSourceProvider<TProvider>(
        this IServiceCollection services,
        ProviderRegistration registration)
        where TProvider : class, IConfigSourceProvider
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(registration);

        services.AddSingleton<TProvider>();
        services.AddSingleton(sp =>
        {
            var provider = sp.GetRequiredService<TProvider>();
            return new RegisteredConfigSourceProvider(provider, registration);
        });

        return services;
    }

    /// <summary>
    /// Registers a configuration source provider with its registration metadata using a factory.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">The provider factory.</param>
    /// <param name="registration">The provider registration metadata.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfigSourceProvider(
        this IServiceCollection services,
        Func<IServiceProvider, IConfigSourceProvider> factory,
        ProviderRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(registration);

        services.AddSingleton(sp =>
        {
            var provider = factory(sp);
            return new RegisteredConfigSourceProvider(provider, registration);
        });

        return services;
    }
}
