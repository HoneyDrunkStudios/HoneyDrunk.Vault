using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Vault.Tests.Models;

/// <summary>
/// Tests for <see cref="VaultScope"/>.
/// </summary>
public sealed class VaultScopeTests
{
    /// <summary>
    /// Verifies that the single-arg constructor leaves tenant/node null.
    /// </summary>
    [Fact]
    public void EnvironmentOnlyConstructor_DefaultsTenantAndNodeToNull()
    {
        var scope = new VaultScope("production");

        Assert.Equal("production", scope.Environment);
        Assert.Null(scope.Tenant);
        Assert.Null(scope.Node);
    }

    /// <summary>
    /// Verifies that the full constructor stores all three components.
    /// </summary>
    [Fact]
    public void FullConstructor_StoresAllComponents()
    {
        var scope = new VaultScope("production", "tenant-42", "node-7");

        Assert.Equal("production", scope.Environment);
        Assert.Equal("tenant-42", scope.Tenant);
        Assert.Equal("node-7", scope.Node);
    }

    /// <summary>
    /// Verifies that null/whitespace environment values are rejected.
    /// </summary>
    /// <param name="environment">The invalid environment name to test.</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ThrowsArgumentException_ForInvalidEnvironment(string? environment)
    {
        Assert.Throws<ArgumentException>(() => new VaultScope(environment!));
        Assert.Throws<ArgumentException>(() => new VaultScope(environment!, "tenant", "node"));
    }

    /// <summary>
    /// Verifies that VaultScope is value-equal as a record.
    /// </summary>
    [Fact]
    public void RecordEquality_ReturnsTrue_ForMatchingValues()
    {
        var a = new VaultScope("production", "tenant", "node");
        var b = new VaultScope("production", "tenant", "node");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
