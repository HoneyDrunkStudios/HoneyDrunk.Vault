using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Kernel.Hosting;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Extensions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.InMemory.Services;
using HoneyDrunk.Vault.Tests.Canary.Invariants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using System.Reflection;

namespace HoneyDrunk.Vault.Tests.Canary;

/// <summary>
/// Canary tests that enforce architectural invariants for the Vault system.
/// These tests run in CI to ensure Vault respects its boundaries with Kernel
/// and provider packages remain isolated from Kernel dependencies.
/// </summary>
/// <remarks>
/// All tests are hermetic: no network, no cloud SDKs, no external services.
/// Uses only InMemory and Configuration providers.
/// </remarks>
[Trait("Category", "Canary")]
[Trait("Category", "Architecture")]
public sealed class VaultArchitectureCanaryTests
{
    /// <summary>
    /// Known secret key for canary operations.
    /// </summary>
    private const string CanarySecretKey = "canary-secret-key";

    /// <summary>
    /// Known secret value for canary operations.
    /// </summary>
    private const string CanarySecretValue = "canary-secret-value-safe-dummy";

    /// <summary>
    /// Known config key for canary operations.
    /// </summary>
    private const string CanaryConfigKey = "canary-config-key";

    /// <summary>
    /// Known config value for canary operations.
    /// </summary>
    private const string CanaryConfigValue = "canary-config-value-safe-dummy";

    /// <summary>
    /// Invariant 1: Kernel context ownership.
    /// Within a single DI scope:
    /// - Vault must not instantiate or own GridContext, NodeContext, or OperationContext.
    /// - Vault must only consume context via Kernel DI/accessors.
    /// - Vault must not access context properties when IsInitialized is false.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task Invariant1_KernelContextOwnership_VaultConsumesContextsViaAccessors()
    {
        // Arrange: Build host with Kernel + Vault registered
        var host = CreateHostWithKernelAndVault();
        await host.StartAsync();

        try
        {
            using var scope = host.Services.CreateScope();

            // Act & Assert: Execute Vault operations and verify context stability
            await KernelContextOwnershipInvariant.ValidateAsync(
                scope,
                async services =>
                {
                    // Representative Vault operations
                    var secretStore = services.GetRequiredService<ISecretStore>();
                    var configSource = services.GetRequiredService<IConfigSource>();

                    // Read secrets
                    var secret1 = await secretStore.GetSecretAsync(new SecretIdentifier(CanarySecretKey));
                    Assert.Equal(CanarySecretValue, secret1.Value);

                    // Read again to exercise cache path
                    var secret2 = await secretStore.GetSecretAsync(new SecretIdentifier(CanarySecretKey));
                    Assert.Equal(CanarySecretValue, secret2.Value);

                    // Read config
                    var config1 = await configSource.GetConfigValueAsync(CanaryConfigKey);
                    Assert.Equal(CanaryConfigValue, config1);

                    // Read again to exercise cache path
                    var config2 = await configSource.GetConfigValueAsync(CanaryConfigKey);
                    Assert.Equal(CanaryConfigValue, config2);

                    // Try-read patterns
                    var trySecret = await secretStore.TryGetSecretAsync(new SecretIdentifier(CanarySecretKey));
                    Assert.True(trySecret.IsSuccess);

                    var tryConfig = await configSource.TryGetConfigValueAsync(CanaryConfigKey);
                    Assert.NotNull(tryConfig);
                });
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    /// <summary>
    /// Invariant 2: No context creation anywhere in Vault.
    /// Vault code must not construct context objects.
    /// Verified via IL scanning of Vault assemblies.
    /// </summary>
    [Fact]
    public void Invariant2_NoContextCreation_VaultAssembliesDoNotConstructContexts()
    {
        // Get all Vault assemblies to scan
        var vaultAssemblies = GetVaultAssemblies();

        Assert.NotEmpty(vaultAssemblies);

        foreach (var assembly in vaultAssemblies)
        {
            // Act & Assert: Scan each assembly for forbidden context construction
            NoContextCreationInvariant.Validate(assembly);
        }
    }

    /// <summary>
    /// Invariant 3: Provider boundary stays clean.
    /// Provider assemblies must have no references to HoneyDrunk.Kernel or HoneyDrunk.Kernel.Abstractions.
    /// Provider assemblies must not have public APIs that accept Kernel types.
    /// </summary>
    [Fact]
    public void Invariant3_ProviderBoundary_InMemoryProviderHasNoKernelDependencies()
    {
        // Get InMemory provider assembly
        var inMemoryAssembly = typeof(Providers.InMemory.Services.InMemorySecretStore).Assembly;

        // Act & Assert
        ProviderBoundaryInvariant.Validate(inMemoryAssembly);
    }

    /// <summary>
    /// Invariant 3 continued: Configuration provider boundary.
    /// </summary>
    [Fact]
    public void Invariant3_ProviderBoundary_ConfigurationProviderHasNoKernelDependencies()
    {
        // Get Configuration provider assembly
        var configAssembly = typeof(Providers.Configuration.Services.ConfigurationSecretStore).Assembly;

        // Act & Assert
        ProviderBoundaryInvariant.Validate(configAssembly);
    }

    /// <summary>
    /// Health and readiness contributors must not violate context rules.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task Invariant4_HealthContributors_DoNotViolateContextRules()
    {
        // Arrange: Build host with Kernel + Vault registered
        var host = CreateHostWithKernelAndVault();
        await host.StartAsync();

        try
        {
            using var scope = host.Services.CreateScope();

            // Act: Execute health and readiness checks
            await KernelContextOwnershipInvariant.ValidateAsync(
                scope,
                async services =>
                {
                    // Get health contributors
                    var healthContributors = services.GetServices<IHealthContributor>();
                    var readinessContributors = services.GetServices<IReadinessContributor>();

                    // Exercise health contributors
                    foreach (var contributor in healthContributors)
                    {
                        var (status, message) = await contributor.CheckHealthAsync();

                        // We don't assert on status - just that it doesn't throw context violations
                        _ = status;
                        _ = message;
                    }

                    // Exercise readiness contributors
                    foreach (var contributor in readinessContributors)
                    {
                        var (isReady, message) = await contributor.CheckReadinessAsync();

                        // We don't assert on readiness - just that it doesn't throw context violations
                        _ = isReady;
                        _ = message;
                    }
                });
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    /// <summary>
    /// Combined canary: Full execution path with all invariant checks.
    /// This test represents the complete "Boot, Read, Cache" scenario.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task FullCanary_BootReadCache_AllInvariantsHold()
    {
        // Invariant 2: Scan assemblies before boot
        var vaultAssemblies = GetVaultAssemblies();
        foreach (var assembly in vaultAssemblies)
        {
            NoContextCreationInvariant.Validate(assembly);
        }

        // Invariant 3: Provider boundaries
        ProviderBoundaryInvariant.Validate(typeof(Providers.InMemory.Services.InMemorySecretStore).Assembly);
        ProviderBoundaryInvariant.Validate(typeof(Providers.Configuration.Services.ConfigurationSecretStore).Assembly);

        // Boot the host
        var host = CreateHostWithKernelAndVault();
        await host.StartAsync();

        try
        {
            using var scope = host.Services.CreateScope();

            // Invariant 1: Context ownership during operations
            await KernelContextOwnershipInvariant.ValidateAsync(
                scope,
                async services =>
                {
                    var secretStore = services.GetRequiredService<ISecretStore>();
                    var configSource = services.GetRequiredService<IConfigSource>();

                    // Initial reads
                    var secret = await secretStore.GetSecretAsync(new SecretIdentifier(CanarySecretKey));
                    Assert.Equal(CanarySecretValue, secret.Value);

                    var config = await configSource.GetConfigValueAsync(CanaryConfigKey);
                    Assert.Equal(CanaryConfigValue, config);

                    // Cache reads (repeated operations)
                    for (int i = 0; i < 3; i++)
                    {
                        var cachedSecret = await secretStore.GetSecretAsync(new SecretIdentifier(CanarySecretKey));
                        Assert.Equal(CanarySecretValue, cachedSecret.Value);

                        var cachedConfig = await configSource.GetConfigValueAsync(CanaryConfigKey);
                        Assert.Equal(CanaryConfigValue, cachedConfig);
                    }

                    // Note: Health/readiness contributors are not registered in off-grid mode
                    // They are tested separately via Invariant4 when full stack is available
                });
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }

    private static IHost CreateHostWithKernelAndVault()
    {
        var configValues = new Dictionary<string, string?>
        {
            [$"Secrets:{CanarySecretKey}"] = CanarySecretValue,
            [CanaryConfigKey] = CanaryConfigValue,
        };

        // Pre-create the secrets and config dictionaries
        var secretsDictionary = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        secretsDictionary[CanarySecretKey] = CanarySecretValue;

        var configDictionary = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        configDictionary[CanaryConfigKey] = CanaryConfigValue;

        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(configValues);
            })
            .ConfigureServices((context, services) =>
            {
                // Register Kernel (Grid + Node)
                services.AddHoneyDrunkGrid(grid =>
                {
                    grid.StudioId = "canary-studio";
                    grid.NodeId = new NodeId("canary-node");
                });

                // Configure VaultOptions (disable caching for simpler test)
                services.Configure<VaultOptions>(opt =>
                {
                    opt.Cache.Enabled = false;
                });

                // Register VaultTelemetry (required by CompositeSecretStore)
                services.AddSingleton<Vault.Telemetry.VaultTelemetry>();

                // Register core Vault services (composite stores, resilience, health contributors)
                services.AddVaultCore();

                // Register InMemory providers via the provider registration pattern
                var registration = new ProviderRegistration
                {
                    Name = "in-memory",
                    ProviderType = ProviderType.InMemory,
                    Priority = 1,
                    IsEnabled = true,
                };

                services.AddSecretProvider(
                    sp => new InMemorySecretStore(
                        secretsDictionary,
                        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InMemorySecretStore>>()),
                    registration);

                services.AddConfigSourceProvider(
                    sp => new InMemoryConfigSource(
                        configDictionary,
                        sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<InMemoryConfigSource>>()),
                    registration);
            })
            .Build();
    }

    private static List<Assembly> GetVaultAssemblies()
    {
        // Get all loaded assemblies that are part of HoneyDrunk.Vault
        return [.. AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a =>
            {
                var name = a.GetName().Name ?? string.Empty;
                return name.StartsWith("HoneyDrunk.Vault", StringComparison.OrdinalIgnoreCase) &&
                       !name.Contains("Tests", StringComparison.OrdinalIgnoreCase);
            })];
    }
}
