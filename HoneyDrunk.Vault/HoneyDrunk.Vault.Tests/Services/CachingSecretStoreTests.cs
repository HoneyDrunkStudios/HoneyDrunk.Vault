using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="CachingSecretStore"/>.
/// </summary>
public sealed class CachingSecretStoreTests : IDisposable
{
    private readonly SecretCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingSecretStoreTests"/> class.
    /// </summary>
    public CachingSecretStoreTests()
    {
        _cache = new SecretCache(
            Options.Create(new VaultOptions
            {
                Cache = new VaultCacheOptions
                {
                    Enabled = true,
                    DefaultTtl = TimeSpan.FromMinutes(5),
                    MaxSize = 100,
                },
            }),
            NullLogger<SecretCache>.Instance);
    }

    /// <summary>
    /// Verifies that invalidating the cache forces the next read to refetch from the provider.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetSecretAsync_RefetchesAfterInvalidation()
    {
        // Arrange
        var spyStore = new SpySecretStore();
        var store = new CachingSecretStore(spyStore, _cache, NullLogger<CachingSecretStore>.Instance);
        var identifier = new SecretIdentifier("db-password");

        // Act
        var first = await store.GetSecretAsync(identifier);
        var second = await store.GetSecretAsync(identifier);
        _cache.Invalidate(identifier.Name);
        var third = await store.GetSecretAsync(identifier);

        // Assert
        Assert.Equal(2, spyStore.GetSecretAsyncCallCount);
        Assert.Equal(first.Value, second.Value);
        Assert.NotEqual(second.Value, third.Value);
    }

    /// <summary>
    /// Disposes shared resources.
    /// </summary>
    public void Dispose()
    {
        _cache.Dispose();
    }

    private sealed class SpySecretStore : ISecretStore
    {
        public int GetSecretAsyncCallCount { get; private set; }

        public Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        {
            GetSecretAsyncCallCount++;
            return Task.FromResult(new SecretValue(identifier, $"value-{GetSecretAsyncCallCount}", $"v{GetSecretAsyncCallCount}"));
        }

        public Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(VaultResult.Success(new SecretValue(identifier, "value", "v1")));
        }

        public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SecretVersion>>([]);
        }
    }
}
