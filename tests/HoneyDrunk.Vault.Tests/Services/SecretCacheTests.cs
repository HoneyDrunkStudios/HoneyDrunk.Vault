using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Extensions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics.Metrics;

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
    /// Verifies the host meter reports real cache lookups only when caching and telemetry are enabled.
    /// </summary>
    /// <param name="cacheEnabled">Whether the secret cache is enabled.</param>
    /// <param name="telemetryEnabled">Whether Vault telemetry is enabled.</param>
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void CacheMetrics_ReportOnlyAggregateLookups_WhenEnabled(bool cacheEnabled, bool telemetryEnabled)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();
        services.AddVaultCore();
        services.Configure<VaultOptions>(options =>
        {
            options.Cache.Enabled = cacheEnabled;
            options.EnableTelemetry = telemetryEnabled;
        });
        using var provider = services.BuildServiceProvider();
        var meterFactory = provider.GetRequiredService<IMeterFactory>();
        using var listener = new MeterListener();
        var measurements = new List<(long count, KeyValuePair<string, object?>[] tags)>();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (ReferenceEquals(instrument.Meter.Scope, meterFactory) &&
                instrument.Meter.Name == "Microsoft.Extensions.Caching.Memory.MemoryCache" &&
                instrument.Name == "dotnet.cache.requests")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, count, tags, _) => measurements.Add((count, tags.ToArray())));
        listener.Start();

        var cache = provider.GetRequiredService<SecretCache>();
        const string key = "test-secret-key";
        Assert.False(cache.TryGet(key, out _));
        cache.Set(key, new SecretValue(new SecretIdentifier(key), "test-secret-value", "v1"));
        Assert.Equal(cacheEnabled, cache.TryGet(key, out _));
        listener.RecordObservableInstruments();

        if (!cacheEnabled || !telemetryEnabled)
        {
            Assert.Empty(measurements);
            return;
        }

        Assert.Equal(2, measurements.Count);
        foreach (var (count, tags) in measurements)
        {
            Assert.Equal(1L, count);
            Assert.Equal(2, tags.Length);
            Assert.Contains(new KeyValuePair<string, object?>("dotnet.cache.name", "HoneyDrunk.Vault.SecretCache"), tags);
        }

        Assert.Contains(measurements, item => item.tags.Contains(new("dotnet.cache.request.result", "hit")));
        Assert.Contains(measurements, item => item.tags.Contains(new("dotnet.cache.request.result", "miss")));
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
    /// Verifies that invalidating one secret leaves other entries intact.
    /// </summary>
    [Fact]
    public void Invalidate_RemovesOnlyRequestedEntry()
    {
        // Arrange
        var first = new SecretValue(new SecretIdentifier("first-secret"), "first-value", "v1");
        var second = new SecretValue(new SecretIdentifier("second-secret"), "second-value", "v1");
        _cache.Set("first-secret", first);
        _cache.Set("second-secret", second);

        // Act
        _cache.Invalidate("first-secret");

        // Assert
        Assert.False(_cache.TryGet("first-secret", out _));
        Assert.True(_cache.TryGet("second-secret", out var remaining));
        Assert.Equal("second-value", remaining!.Value);
    }

    /// <summary>
    /// Verifies that invalidating all secrets clears the cache.
    /// </summary>
    [Fact]
    public void InvalidateAll_ClearsCache()
    {
        // Arrange
        _cache.Set("first-secret", new SecretValue(new SecretIdentifier("first-secret"), "first-value", "v1"));
        _cache.Set("second-secret", new SecretValue(new SecretIdentifier("second-secret"), "second-value", "v1"));

        // Act
        _cache.InvalidateAll();

        // Assert
        Assert.False(_cache.TryGet("first-secret", out _));
        Assert.False(_cache.TryGet("second-secret", out _));
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    public void Dispose()
    {
        _cache.Dispose();
    }
}
