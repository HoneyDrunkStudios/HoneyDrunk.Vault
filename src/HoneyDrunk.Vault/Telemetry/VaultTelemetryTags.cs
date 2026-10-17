namespace HoneyDrunk.Vault.Telemetry;

/// <summary>
/// Constants for vault telemetry tags.
/// </summary>
public static class VaultTelemetryTags
{
    /// <summary>
    /// The vault provider name tag.
    /// </summary>
    public const string Provider = "vault.provider";

    /// <summary>
    /// The vault operation key tag (not the secret value).
    /// </summary>
    public const string Key = "vault.key";

    /// <summary>
    /// The vault operation result status tag.
    /// </summary>
    public const string ResultStatus = "vault.result";

    /// <summary>
    /// The vault cache status tag.
    /// </summary>
    public const string CacheStatus = "vault.cache";

    /// <summary>
    /// The vault operation type tag.
    /// </summary>
    public const string OperationType = "vault.operation";

    /// <summary>
    /// The vault retry attempt count tag.
    /// </summary>
    public const string RetryAttempts = "vault.retry.attempts";

    /// <summary>
    /// The vault circuit breaker state tag.
    /// </summary>
    public const string CircuitBreakerState = "vault.circuit_breaker.state";

    /// <summary>
    /// Result status value: success.
    /// </summary>
    public const string ResultSuccess = "success";

    /// <summary>
    /// Result status value: error.
    /// </summary>
    public const string ResultError = "error";

    /// <summary>
    /// Result status value: not found.
    /// </summary>
    public const string ResultNotFound = "not_found";

    /// <summary>
    /// Result status value: circuit breaker open.
    /// </summary>
    public const string ResultCircuitOpen = "circuit_open";

    /// <summary>
    /// Cache status value: hit.
    /// </summary>
    public const string CacheHit = "hit";

    /// <summary>
    /// Cache status value: miss.
    /// </summary>
    public const string CacheMiss = "miss";

    /// <summary>
    /// Cache status value: bypass.
    /// </summary>
    public const string CacheBypass = "bypass";

    /// <summary>
    /// Circuit breaker state value: closed (normal operation).
    /// </summary>
    public const string CircuitClosed = "closed";

    /// <summary>
    /// Circuit breaker state value: open (blocking requests).
    /// </summary>
    public const string CircuitOpen = "open";

    /// <summary>
    /// Circuit breaker state value: half-open (testing recovery).
    /// </summary>
    public const string CircuitHalfOpen = "half_open";
}
