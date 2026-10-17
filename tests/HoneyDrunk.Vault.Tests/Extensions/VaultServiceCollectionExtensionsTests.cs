using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Extensions;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Tests for <see cref="VaultServiceCollectionExtensions"/>.
/// </summary>
public sealed class VaultServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that AddVaultCore registers the cache invalidator alongside the cache.
    /// </summary>
    [Fact]
    public void AddVaultCore_RegistersCacheInvalidatorAsSecretCache()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<VaultOptions>>(Options.Create(new VaultOptions
        {
            Cache = new VaultCacheOptions
            {
                Enabled = true,
                DefaultTtl = TimeSpan.FromMinutes(5),
                MaxSize = 100,
            },
        }));

        // Act
        services.AddVaultCore();
        using var provider = services.BuildServiceProvider();

        // Assert
        var cache = provider.GetRequiredService<SecretCache>();
        var invalidator = provider.GetRequiredService<ISecretCacheInvalidator>();
        Assert.Same(cache, invalidator);
    }
}
