using HoneyDrunk.Vault.Configuration;

namespace HoneyDrunk.Vault.Tests.Configuration;

/// <summary>
/// Tests for <see cref="VaultCacheOptions"/>.
/// </summary>
public sealed class VaultCacheOptionsTests
{
    /// <summary>
    /// Verifies that default values are set correctly.
    /// </summary>
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var options = new VaultCacheOptions();

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(5), options.DefaultTtl);
        Assert.Equal(1000, options.MaxSize);
        Assert.Null(options.SlidingExpiration);
    }

    /// <summary>
    /// Verifies that properties can be set.
    /// </summary>
    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var options = new VaultCacheOptions
        {
            // Act
            Enabled = false,
            DefaultTtl = TimeSpan.FromMinutes(10),
            MaxSize = 500,
            SlidingExpiration = TimeSpan.FromMinutes(2)
        };

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal(TimeSpan.FromMinutes(10), options.DefaultTtl);
        Assert.Equal(500, options.MaxSize);
        Assert.Equal(TimeSpan.FromMinutes(2), options.SlidingExpiration);
    }
}
