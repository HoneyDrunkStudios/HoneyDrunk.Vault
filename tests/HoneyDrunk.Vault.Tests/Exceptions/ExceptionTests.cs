using HoneyDrunk.Vault.Exceptions;

namespace HoneyDrunk.Vault.Tests.Exceptions;

/// <summary>
/// Tests for vault exceptions.
/// </summary>
public sealed class ExceptionTests
{
    /// <summary>
    /// Verifies that SecretNotFoundException stores secret name.
    /// </summary>
    [Fact]
    public void SecretNotFoundException_StoresSecretName()
    {
        // Arrange & Act
        var exception = new SecretNotFoundException("my-secret");

        // Assert
        Assert.Equal("my-secret", exception.SecretName);
        Assert.Contains("my-secret", exception.Message);
    }

    /// <summary>
    /// Verifies that SecretNotFoundException with inner exception works correctly.
    /// </summary>
    [Fact]
    public void SecretNotFoundException_WithInnerException_WorksCorrectly()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new SecretNotFoundException("my-secret", innerException);

        // Assert
        Assert.Equal("my-secret", exception.SecretName);
        Assert.Contains("my-secret", exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    /// <summary>
    /// Verifies that ConfigurationNotFoundException stores key.
    /// </summary>
    [Fact]
    public void ConfigurationNotFoundException_StoresKey()
    {
        // Arrange & Act
        var exception = new ConfigurationNotFoundException("my-key");

        // Assert
        Assert.Equal("my-key", exception.Key);
        Assert.Contains("my-key", exception.Message);
    }

    /// <summary>
    /// Verifies that ConfigurationNotFoundException with inner exception works correctly.
    /// </summary>
    [Fact]
    public void ConfigurationNotFoundException_WithInnerException_WorksCorrectly()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new ConfigurationNotFoundException("my-key", innerException);

        // Assert
        Assert.Equal("my-key", exception.Key);
        Assert.Contains("my-key", exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    /// <summary>
    /// Verifies that VaultOperationException with message works correctly.
    /// </summary>
    [Fact]
    public void VaultOperationException_WithMessage_WorksCorrectly()
    {
        // Arrange & Act
        var exception = new VaultOperationException("Operation failed");

        // Assert
        Assert.Equal("Operation failed", exception.Message);
        Assert.Null(exception.InnerException);
    }

    /// <summary>
    /// Verifies that VaultOperationException with inner exception works correctly.
    /// </summary>
    [Fact]
    public void VaultOperationException_WithInnerException_WorksCorrectly()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new VaultOperationException("Operation failed", innerException);

        // Assert
        Assert.Equal("Operation failed", exception.Message);
        Assert.Same(innerException, exception.InnerException);
    }

    /// <summary>
    /// Verifies that VaultOperationException is derived from Exception.
    /// </summary>
    [Fact]
    public void VaultOperationException_IsDerivedFromException()
    {
        // Arrange & Act
        var exception = new VaultOperationException("Test");

        // Assert
        Assert.IsType<Exception>(exception, exactMatch: false);
    }
}
