using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Vault.Tests.Models;

/// <summary>
/// Tests for <see cref="SecretIdentifier"/>.
/// </summary>
public sealed class SecretIdentifierTests
{
    /// <summary>
    /// Verifies that constructor sets Name correctly.
    /// </summary>
    [Fact]
    public void Constructor_SetsName()
    {
        // Arrange & Act
        var identifier = new SecretIdentifier("test-secret");

        // Assert
        Assert.Equal("test-secret", identifier.Name);
        Assert.Null(identifier.Version);
    }

    /// <summary>
    /// Verifies that constructor sets Name and Version correctly.
    /// </summary>
    [Fact]
    public void Constructor_SetsNameAndVersion()
    {
        // Arrange & Act
        var identifier = new SecretIdentifier("test-secret", "v1");

        // Assert
        Assert.Equal("test-secret", identifier.Name);
        Assert.Equal("v1", identifier.Version);
    }

    /// <summary>
    /// Verifies that constructor throws for null name.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNullName()
    {
        // Act & Assert - SecretIdentifier throws ArgumentException for null/empty/whitespace
        Assert.Throws<ArgumentException>(() => new SecretIdentifier(null!));
    }

    /// <summary>
    /// Verifies that constructor throws for empty name.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForEmptyName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SecretIdentifier(string.Empty));
    }

    /// <summary>
    /// Verifies that constructor throws for whitespace name.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForWhitespaceName()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SecretIdentifier("   "));
    }

    /// <summary>
    /// Verifies that equality works correctly for same values.
    /// </summary>
    [Fact]
    public void Equality_WorksCorrectly_SameValues()
    {
        // Arrange
        var id1 = new SecretIdentifier("secret", "v1");
        var id2 = new SecretIdentifier("secret", "v1");

        // Assert
        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    /// <summary>
    /// Verifies that equality works correctly for different versions.
    /// </summary>
    [Fact]
    public void Equality_WorksCorrectly_DifferentVersions()
    {
        // Arrange
        var id1 = new SecretIdentifier("secret", "v1");
        var id2 = new SecretIdentifier("secret", "v2");

        // Assert
        Assert.NotEqual(id1, id2);
        Assert.True(id1 != id2);
    }

    /// <summary>
    /// Verifies that equality works correctly for null version.
    /// </summary>
    [Fact]
    public void Equality_WorksCorrectly_NullVersion()
    {
        // Arrange
        var id1 = new SecretIdentifier("secret");
        var id2 = new SecretIdentifier("secret");

        // Assert
        Assert.Equal(id1, id2);
    }

    /// <summary>
    /// Verifies that ToString returns meaningful representation.
    /// </summary>
    [Fact]
    public void ToString_ReturnsMeaningfulRepresentation()
    {
        // Arrange
        var withVersion = new SecretIdentifier("secret", "v1");
        var withoutVersion = new SecretIdentifier("secret");

        // Assert
        Assert.Contains("secret", withVersion.ToString());
        Assert.Contains("v1", withVersion.ToString());
        Assert.Contains("secret", withoutVersion.ToString());
    }
}
