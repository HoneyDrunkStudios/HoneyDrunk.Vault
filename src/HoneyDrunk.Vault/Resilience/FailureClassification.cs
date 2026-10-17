namespace HoneyDrunk.Vault.Resilience;

/// <summary>
/// Classifies the type of failure that occurred during a vault operation.
/// </summary>
public enum FailureClassification
{
    /// <summary>
    /// The requested item was not found. Not a failure, continue to next provider.
    /// </summary>
    NotFound,

    /// <summary>
    /// A transient failure (network, timeout, temporary unavailability).
    /// May be retried or fall back to next provider depending on provider configuration.
    /// </summary>
    Transient,

    /// <summary>
    /// A fatal configuration error (invalid credentials, missing required settings).
    /// Should fail fast and not retry.
    /// </summary>
    FatalConfiguration,
}
