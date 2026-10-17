namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// Resilience options for the vault (circuit breaker, retry policies).
/// </summary>
public sealed class VaultResilienceOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether retry is enabled.
    /// </summary>
    public bool RetryEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial retry delay.
    /// </summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Gets or sets a value indicating whether circuit breaker is enabled.
    /// </summary>
    public bool CircuitBreakerEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the failure threshold before circuit opens.
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration the circuit stays open.
    /// </summary>
    public TimeSpan CircuitBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the operation timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
