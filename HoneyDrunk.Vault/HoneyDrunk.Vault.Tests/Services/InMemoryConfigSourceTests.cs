using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Providers.InMemory.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="InMemoryConfigSource"/>.
/// </summary>
public sealed class InMemoryConfigSourceTests
{
    private readonly InMemoryConfigSource _source;
    private readonly ConcurrentDictionary<string, string> _configValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryConfigSourceTests"/> class.
    /// </summary>
    public InMemoryConfigSourceTests()
    {
        _configValues = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _source = new InMemoryConfigSource(_configValues, NullLogger<InMemoryConfigSource>.Instance);
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync returns value when it exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetConfigValueAsync_ReturnsValue_WhenExists()
    {
        // Arrange
        const string key = "test-key";
        const string value = "test-value";
        _configValues[key] = value;

        // Act
        var result = await _source.GetConfigValueAsync(key);

        // Assert
        Assert.Equal(value, result);
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync throws ConfigurationNotFoundException when key doesn't exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetConfigValueAsync_ThrowsConfigurationNotFoundException_WhenNotExists()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ConfigurationNotFoundException>(
            () => _source.GetConfigValueAsync("non-existent"));
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync throws for null or whitespace key.
    /// </summary>
    /// <param name="key">The invalid key to test.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConfigValueAsync_ThrowsArgumentException_WhenKeyIsNullOrWhitespace(string? key)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _source.GetConfigValueAsync(key!));
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync returns value when it exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_ReturnsValue_WhenExists()
    {
        // Arrange
        const string key = "existing-key";
        const string value = "existing-value";
        _configValues[key] = value;

        // Act
        var result = await _source.TryGetConfigValueAsync(key);

        // Assert
        Assert.Equal(value, result);
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync returns null when key doesn't exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _source.TryGetConfigValueAsync("missing-key");

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync with type returns converted value.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetConfigValueAsync_Typed_ReturnsConvertedValue()
    {
        // Arrange
        _configValues["int-key"] = "42";
        _configValues["bool-key"] = "true";
        _configValues["double-key"] = "3.14";

        // Act & Assert
        Assert.Equal(42, await _source.GetConfigValueAsync<int>("int-key"));
        Assert.True(await _source.GetConfigValueAsync<bool>("bool-key"));
        Assert.Equal(3.14, await _source.GetConfigValueAsync<double>("double-key"));
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync with type returns default when key doesn't exist.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_Typed_ReturnsDefault_WhenNotExists()
    {
        // Act
        var result = await _source.TryGetConfigValueAsync("missing", 100);

        // Assert
        Assert.Equal(100, result);
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync with type returns converted value when exists.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_Typed_ReturnsConvertedValue_WhenExists()
    {
        // Arrange
        _configValues["timeout"] = "30";

        // Act
        var result = await _source.TryGetConfigValueAsync("timeout", 10);

        // Assert
        Assert.Equal(30, result);
    }

    /// <summary>
    /// Verifies that SetConfigValue adds a new value.
    /// </summary>
    [Fact]
    public void SetConfigValue_AddsNewValue()
    {
        // Act
        _source.SetConfigValue("new-key", "new-value");

        // Assert
        Assert.True(_configValues.ContainsKey("new-key"));
        Assert.Equal("new-value", _configValues["new-key"]);
    }

    /// <summary>
    /// Verifies that SetConfigValue updates an existing value.
    /// </summary>
    [Fact]
    public void SetConfigValue_UpdatesExistingValue()
    {
        // Arrange
        _configValues["updateable"] = "old-value";

        // Act
        _source.SetConfigValue("updateable", "new-value");

        // Assert
        Assert.Equal("new-value", _configValues["updateable"]);
    }

    /// <summary>
    /// Verifies that SetConfigValue throws for null or whitespace key.
    /// </summary>
    /// <param name="key">The invalid key to test.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetConfigValue_ThrowsArgumentException_WhenKeyIsNullOrWhitespace(string? key)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _source.SetConfigValue(key!, "value"));
    }

    /// <summary>
    /// Verifies that SetConfigValue throws for null value.
    /// </summary>
    [Fact]
    public void SetConfigValue_ThrowsArgumentNullException_WhenValueIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _source.SetConfigValue("key", null!));
    }

    /// <summary>
    /// Verifies that RemoveConfigValue returns true when value existed.
    /// </summary>
    [Fact]
    public void RemoveConfigValue_ReturnsTrue_WhenValueExisted()
    {
        // Arrange
        _configValues["removable"] = "value";

        // Act
        var result = _source.RemoveConfigValue("removable");

        // Assert
        Assert.True(result);
        Assert.False(_configValues.ContainsKey("removable"));
    }

    /// <summary>
    /// Verifies that RemoveConfigValue returns false when value doesn't exist.
    /// </summary>
    [Fact]
    public void RemoveConfigValue_ReturnsFalse_WhenValueNotExist()
    {
        // Act
        var result = _source.RemoveConfigValue("non-existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that Clear removes all values.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllValues()
    {
        // Arrange
        _configValues["key1"] = "value1";
        _configValues["key2"] = "value2";

        // Act
        _source.Clear();

        // Assert
        Assert.Empty(_configValues);
    }

    /// <summary>
    /// Verifies that key lookup is case-insensitive.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetConfigValueAsync_IsCaseInsensitive()
    {
        // Arrange
        _configValues["MyKey"] = "value";

        // Act & Assert
        Assert.Equal("value", await _source.GetConfigValueAsync("mykey"));
        Assert.Equal("value", await _source.GetConfigValueAsync("MYKEY"));
        Assert.Equal("value", await _source.GetConfigValueAsync("MyKey"));
    }
}
