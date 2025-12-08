using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Vault.Tests.Models;

/// <summary>
/// Tests for <see cref="SecretValue"/>.
/// </summary>
public sealed class SecretValueTests
{
    /// <summary>
    /// Verifies that constructor sets all properties correctly.
    /// </summary>
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var identifier = new SecretIdentifier("test-secret", "v1");

        // Act
        var value = new SecretValue(identifier, "secret-value", "v1");

        // Assert
        Assert.Equal(identifier, value.Identifier);
        Assert.Equal("secret-value", value.Value);
        Assert.Equal("v1", value.Version);
    }

    /// <summary>
    /// Verifies that constructor throws for null identifier.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNullIdentifier()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SecretValue(null!, "value", "v1"));
    }

    /// <summary>
    /// Verifies that constructor throws for null value.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNullValue()
    {
        // Arrange
        var identifier = new SecretIdentifier("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SecretValue(identifier, null!, "v1"));
    }

    /// <summary>
    /// Verifies that constructor allows null version.
    /// </summary>
    [Fact]
    public void Constructor_AllowsNullVersion()
    {
        // Arrange
        var identifier = new SecretIdentifier("test");

        // Act - version is nullable, so null is allowed
        var value = new SecretValue(identifier, "value", null);

        // Assert
        Assert.Null(value.Version);
    }

    /// <summary>
    /// Verifies that equality works correctly.
    /// </summary>
    [Fact]
    public void Equality_WorksCorrectly()
    {
        // Arrange
        var id1 = new SecretIdentifier("secret", "v1");
        var id2 = new SecretIdentifier("secret", "v1");
        var value1 = new SecretValue(id1, "value", "v1");
        var value2 = new SecretValue(id2, "value", "v1");

        // Assert
        Assert.Equal(value1, value2);
    }
}
