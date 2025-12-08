using HoneyDrunk.Vault.Configuration;

namespace HoneyDrunk.Vault.Tests.Configuration;

/// <summary>
/// Tests for <see cref="VaultResilienceOptions"/>.
/// </summary>
public sealed class VaultResilienceOptionsTests
{
    /// <summary>
    /// Verifies that default values are set correctly.
    /// </summary>
    [Fact]
    public void DefaultValues_AreSetCorrectly()
    {
        // Act
        var options = new VaultResilienceOptions();

        // Assert
        Assert.True(options.RetryEnabled);
        Assert.Equal(3, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(200), options.RetryDelay);
        Assert.True(options.CircuitBreakerEnabled);
        Assert.Equal(5, options.FailureThreshold);
        Assert.Equal(TimeSpan.FromSeconds(30), options.CircuitBreakDuration);
        Assert.Equal(TimeSpan.FromSeconds(10), options.Timeout);
    }

    /// <summary>
    /// Verifies that properties can be set.
    /// </summary>
    [Fact]
    public void Properties_CanBeSet()
    {
        // Arrange
        var options = new VaultResilienceOptions
        {
            // Act
            RetryEnabled = false,
            MaxRetryAttempts = 5,
            RetryDelay = TimeSpan.FromMilliseconds(500),
            CircuitBreakerEnabled = false,
            FailureThreshold = 10,
            CircuitBreakDuration = TimeSpan.FromSeconds(60),
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Assert
        Assert.False(options.RetryEnabled);
        Assert.Equal(5, options.MaxRetryAttempts);
        Assert.Equal(TimeSpan.FromMilliseconds(500), options.RetryDelay);
        Assert.False(options.CircuitBreakerEnabled);
        Assert.Equal(10, options.FailureThreshold);
        Assert.Equal(TimeSpan.FromSeconds(60), options.CircuitBreakDuration);
        Assert.Equal(TimeSpan.FromSeconds(30), options.Timeout);
    }
}
