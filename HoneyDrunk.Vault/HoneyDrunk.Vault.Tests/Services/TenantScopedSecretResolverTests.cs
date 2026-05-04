using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.InMemory.Services;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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

    /// <summary>
    /// Verifies tenant-scoped provider failures are not masked by shared-secret fallback.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveAsync_PropagatesTenantScopedProviderFailure()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        var store = new Mock<ISecretStore>();
        store.Setup(s => s.GetSecretAsync(
                It.Is<SecretIdentifier>(id => id.Name.StartsWith("tenant-", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider offline"));

        var resolver = new TenantScopedSecretResolver(store.Object);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveAsync(tenantId, "resend-api-key"));
        store.Verify(s => s.GetSecretAsync(It.Is<SecretIdentifier>(id => id.Name == "resend-api-key"), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Verifies tenant-scoped not-found failures still fall back to the shared secret.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ResolveAsync_FallsBackOnlyWhenTenantScopedSecretNotFound()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        var shared = new SecretValue(new SecretIdentifier("resend-api-key"), "shared-value", "v1");
        var store = new Mock<ISecretStore>();
        store.Setup(s => s.GetSecretAsync(
                It.Is<SecretIdentifier>(id => id.Name.StartsWith("tenant-", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SecretNotFoundException("tenant-secret"));
        store.Setup(s => s.GetSecretAsync(
                It.Is<SecretIdentifier>(id => id.Name == "resend-api-key"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(shared);

        var resolver = new TenantScopedSecretResolver(store.Object);

        // Act
        var result = await resolver.ResolveAsync(tenantId, "resend-api-key");

        // Assert
        Assert.Equal(shared, result);
    }

    /// <summary>
    /// Verifies TryResolveAsync returns tenant-scoped provider errors instead of falling back to shared secrets.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task TryResolveAsync_ReturnsFailure_WhenTenantScopedProviderFails()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        var store = new Mock<ISecretStore>();
        store.Setup(s => s.GetSecretAsync(
                It.Is<SecretIdentifier>(id => id.Name.StartsWith("tenant-", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("provider offline"));

        var resolver = new TenantScopedSecretResolver(store.Object);

        // Act
        var result = await resolver.TryResolveAsync(tenantId, "resend-api-key");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("provider offline", result.ErrorMessage, StringComparison.Ordinal);
        store.Verify(s => s.TryGetSecretAsync(It.Is<SecretIdentifier>(id => id.Name == "resend-api-key"), It.IsAny<CancellationToken>()), Times.Never);
    }
}
