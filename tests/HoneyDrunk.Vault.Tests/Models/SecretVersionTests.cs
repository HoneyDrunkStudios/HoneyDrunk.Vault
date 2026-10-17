using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Vault.Tests.Models;

/// <summary>
/// Tests for <see cref="SecretVersion"/>.
/// </summary>
public sealed class SecretVersionTests
{
    /// <summary>
    /// Verifies that constructor sets properties correctly.
    /// </summary>
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange
        var createdOn = DateTimeOffset.UtcNow;

        // Act
        var version = new SecretVersion("v1", createdOn);

        // Assert
        Assert.Equal("v1", version.Version);
        Assert.Equal(createdOn, version.CreatedOn);
    }

    /// <summary>
    /// Verifies that constructor throws for null version.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNullVersion()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SecretVersion(null!, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Verifies that constructor throws for empty version.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForEmptyVersion()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SecretVersion(string.Empty, DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Verifies that constructor throws for whitespace version.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForWhitespaceVersion()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SecretVersion("   ", DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// Verifies that equality works correctly.
    /// </summary>
    [Fact]
    public void Equality_WorksCorrectly()
    {
        // Arrange
        var createdOn = DateTimeOffset.UtcNow;
        var version1 = new SecretVersion("v1", createdOn);
        var version2 = new SecretVersion("v1", createdOn);

        // Assert
        Assert.Equal(version1, version2);
    }

    /// <summary>
    /// Verifies that inequality works for different versions.
    /// </summary>
    [Fact]
    public void Inequality_WorksForDifferentVersions()
    {
        // Arrange
        var createdOn = DateTimeOffset.UtcNow;
        var version1 = new SecretVersion("v1", createdOn);
        var version2 = new SecretVersion("v2", createdOn);

        // Assert
        Assert.NotEqual(version1, version2);
    }

    /// <summary>
    /// Verifies that inequality works for different timestamps.
    /// </summary>
    [Fact]
    public void Inequality_WorksForDifferentTimestamps()
    {
        // Arrange
        var version1 = new SecretVersion("v1", DateTimeOffset.UtcNow);
        var version2 = new SecretVersion("v1", DateTimeOffset.UtcNow.AddSeconds(1));

        // Assert
        Assert.NotEqual(version1, version2);
    }

    /// <summary>
    /// Verifies that GetHashCode is consistent.
    /// </summary>
    [Fact]
    public void GetHashCode_IsConsistent()
    {
        // Arrange
        var createdOn = DateTimeOffset.UtcNow;
        var version1 = new SecretVersion("v1", createdOn);
        var version2 = new SecretVersion("v1", createdOn);

        // Assert
        Assert.Equal(version1.GetHashCode(), version2.GetHashCode());
    }
}
