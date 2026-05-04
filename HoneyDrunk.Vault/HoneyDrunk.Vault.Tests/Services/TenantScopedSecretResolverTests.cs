using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Vault.Providers.InMemory.Services;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="TenantScopedSecretResolver"/>.
/// </summary>
public sealed class TenantScopedSecretResolverTests
{
    private readonly ConcurrentDictionary<string, string> _secrets = new(StringComparer.OrdinalIgnoreCase);
    private readonly TenantScopedSecretResolver _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantScopedSecretResolverTests"/> class.
    /// </summary>
    public TenantScopedSecretResolverTests()
    {
        var store = new InMemorySecretStore(_secrets, NullLogger<InMemorySecretStore>.Instance);
        _resolver = new TenantScopedSecretResolver(store);
    }

    /// <summary>
    /// Verifies tenant-scoped secrets are resolved before shared secrets.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveAsync_ReturnsTenantScopedSecret_WhenPresent()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        const string secretName = "resend-api-key";
        var tenantScopedName = TenantScopedSecretResolver.FormatTenantScopedName(tenantId, secretName);
        _secrets[secretName] = "shared-value";
        _secrets[tenantScopedName] = "tenant-value";

        // Act
        var result = await _resolver.ResolveAsync(tenantId, secretName);

        // Assert
        Assert.Equal(tenantScopedName, result.Identifier.Name);
        Assert.Equal("tenant-value", result.Value);
    }

    /// <summary>
    /// Verifies tenant-scoped resolution falls back to the shared node path when tenant secret is absent.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveAsync_FallsBackToSharedSecret_WhenTenantScopedSecretMissing()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        const string secretName = "resend-api-key";
        _secrets[secretName] = "shared-value";

        // Act
        var result = await _resolver.ResolveAsync(tenantId, secretName);

        // Assert
        Assert.Equal(secretName, result.Identifier.Name);
        Assert.Equal("shared-value", result.Value);
    }

    /// <summary>
    /// Verifies internal tenant resolution short-circuits to the shared node path.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveAsync_UsesSharedSecret_WhenTenantIsInternal()
    {
        // Arrange
        var tenantId = new TenantId("00000000000000000000000000");
        const string secretName = "resend-api-key";
        _secrets[secretName] = "shared-value";
        _secrets[TenantScopedSecretResolver.FormatTenantScopedName(tenantId, secretName)] = "tenant-value";

        // Act
        var result = await _resolver.ResolveAsync(tenantId, secretName);

        // Assert
        Assert.Equal(secretName, result.Identifier.Name);
        Assert.Equal("shared-value", result.Value);
    }

    /// <summary>
    /// Verifies TryResolveAsync returns failure when neither tenant-scoped nor shared secret exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task TryResolveAsync_ReturnsFailure_WhenNoSecretExists()
    {
        // Arrange
        var tenantId = TenantId.NewId();

        // Act
        var result = await _resolver.TryResolveAsync(tenantId, "missing-secret");

        // Assert
        Assert.False(result.IsSuccess);
    }
}
