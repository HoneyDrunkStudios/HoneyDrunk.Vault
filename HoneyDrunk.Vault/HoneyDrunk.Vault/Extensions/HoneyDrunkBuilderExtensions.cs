using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Health;
using HoneyDrunk.Vault.Lifecycle;
using HoneyDrunk.Vault.Services;
using HoneyDrunk.Vault.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Vault.Extensions;

/// <summary>
/// Extension methods for integrating Vault with HoneyDrunk.Kernel.
/// </summary>
public static class HoneyDrunkBuilderExtensions
{
    /// <summary>
    /// Adds Vault services to the HoneyDrunk node.
    /// This registers ISecretStore, IConfigProvider, health/readiness contributors,
    /// and telemetry integration with the Kernel.
    /// </summary>
    /// <param name="builder">The HoneyDrunk builder.</param>
    /// <param name="configure">The configuration action for vault options.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddVault(
        this IHoneyDrunkBuilder builder,
        Action<VaultOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        var services = builder.Services;

        // Configure options
        var options = new VaultOptions();
        configure(options);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        // Register secret cache (always registered, respects Enabled flag internally)
        services.TryAddSingleton<SecretCache>();

        // Register telemetry
        if (options.EnableTelemetry)
        {
            services.TryAddSingleton<VaultTelemetry>();
        }

        // Register core vault services (composite, caching, resilience)
        services.AddVaultCore();

        // Register Kernel lifecycle integration
        services.AddSingleton<IHealthContributor, VaultHealthContributor>();
        services.AddSingleton<IReadinessContributor, VaultReadinessContributor>();
        services.AddSingleton<IStartupHook, VaultStartupHook>();

        return builder;
    }

    /// <summary>
    /// Adds Vault services with default configuration.
    /// </summary>
    /// <param name="builder">The HoneyDrunk builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddVault(this IHoneyDrunkBuilder builder)
    {
        return builder.AddVault(_ => { });
    }
}
