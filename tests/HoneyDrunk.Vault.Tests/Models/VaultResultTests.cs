using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Vault.Tests.Models;

/// <summary>
/// Tests for <see cref="VaultResult"/> and <see cref="VaultResult{T}"/>.
/// </summary>
public sealed class VaultResultTests
{
    /// <summary>
    /// Verifies that Success creates a successful result.
    /// </summary>
    [Fact]
    public void Success_CreatesSuccessfulResult()
    {
        // Act
        var result = VaultResult.Success("test-value");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("test-value", result.Value);
        Assert.Null(result.ErrorMessage);
    }

    /// <summary>
    /// Verifies that Failure creates a failed result.
    /// </summary>
    [Fact]
    public void Failure_CreatesFailedResult()
    {
        // Act
        var result = VaultResult.Failure<string>("Error message");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("Error message", result.ErrorMessage);
    }

    /// <summary>
    /// Verifies that Success works with complex types.
    /// </summary>
    [Fact]
    public void Success_WorksWithComplexTypes()
    {
        // Arrange
        var secretValue = new SecretValue(new SecretIdentifier("key"), "value", "v1");

        // Act
        var result = VaultResult.Success(secretValue);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(secretValue, result.Value);
    }

    /// <summary>
    /// Verifies that Failure works with complex types.
    /// </summary>
    [Fact]
    public void Failure_WorksWithComplexTypes()
    {
        // Act
        var result = VaultResult.Failure<SecretValue>("Not found");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Equal("Not found", result.ErrorMessage);
    }
}
