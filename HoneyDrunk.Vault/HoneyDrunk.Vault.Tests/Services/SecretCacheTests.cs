using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="SecretCache"/>.
/// </summary>
public sealed class SecretCacheTests : IDisposable
{
    private readonly SecretCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretCacheTests"/> class.
    /// </summary>
    public SecretCacheTests()
    {
        var options = Options.Create(new VaultOptions
        {
            Cache = new VaultCacheOptions
            {
                Enabled = true,
                DefaultTtl = TimeSpan.FromMinutes(5),
                MaxSize = 100,
            },
        });
        _cache = new SecretCache(options, NullLogger<SecretCache>.Instance);
    }

    /// <summary>
    /// Verifies that TryGet returns false when key not found.
    /// </summary>
    [Fact]
    public void TryGet_ReturnsFalse_WhenKeyNotFound()
    {
        // Act
        var result = _cache.TryGet("non-existent-key", out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    /// <summary>
    /// Verifies that TryGet returns the value when key exists.
    /// </summary>
    [Fact]
    public void TryGet_ReturnsValue_WhenKeyExists()
    {
        // Arrange
        const string key = "test-key";
        var secretValue = new SecretValue(new SecretIdentifier(key), "test-value", "v1");
        _cache.Set(key, secretValue);

        // Act
        var result = _cache.TryGet(key, out var cachedValue);

        // Assert
        Assert.True(result);
        Assert.NotNull(cachedValue);
        Assert.Equal("test-value", cachedValue.Value);
    }

    /// <summary>
    /// Verifies that Set overwrites existing value.
    /// </summary>
    [Fact]
    public void Set_OverwritesExistingValue()
    {
        // Arrange
        const string key = "test-key";
        var originalValue = new SecretValue(new SecretIdentifier(key), "original-value", "v1");
        var updatedValue = new SecretValue(new SecretIdentifier(key), "updated-value", "v2");
        _cache.Set(key, originalValue);

        // Act
        _cache.Set(key, updatedValue);

        // Assert
        var result = _cache.TryGet(key, out var cachedValue);
        Assert.True(result);
        Assert.Equal("updated-value", cachedValue!.Value);
    }

    /// <summary>
    /// Verifies that GetOrCreateAsync returns existing value.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetOrCreateAsync_ReturnsExistingValue()
    {
        // Arrange
        const string key = "test-key";
        var existingValue = new SecretValue(new SecretIdentifier(key), "existing-value", "v1");
        _cache.Set(key, existingValue);

        var factoryWasCalled = false;

        // Act
        var result = await _cache.GetOrCreateAsync(key, ct =>
        {
            factoryWasCalled = true;
            return Task.FromResult(new SecretValue(new SecretIdentifier(key), "factory-value", "v1"));
        });

        // Assert
        Assert.Equal("existing-value", result.Value);
        Assert.False(factoryWasCalled);
    }

    /// <summary>
    /// Verifies that GetOrCreateAsync calls factory when value not cached.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetOrCreateAsync_CallsFactory_WhenValueNotCached()
    {
        // Arrange
        const string key = "new-key";
        var factoryValue = new SecretValue(new SecretIdentifier(key), "factory-value", "v1");

        var factoryWasCalled = false;

        // Act
        var result = await _cache.GetOrCreateAsync(key, ct =>
        {
            factoryWasCalled = true;
            return Task.FromResult(factoryValue);
        });

        // Assert
        Assert.Equal("factory-value", result.Value);
        Assert.True(factoryWasCalled);

        // Verify value was cached
        var found = _cache.TryGet(key, out var cached);
        Assert.True(found);
        Assert.Equal("factory-value", cached!.Value);
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    public void Dispose()
    {
        _cache.Dispose();
    }
}
