namespace HoneyDrunk.Vault.Health;

/// <summary>
/// Result of probing a single provider's health endpoint.
/// </summary>
/// <param name="IsHealthy">True when the probe returned without exception and signalled healthy.</param>
/// <param name="Exception">The exception captured when the probe threw; null when no exception occurred.</param>
internal readonly record struct ProbeOutcome(bool IsHealthy, Exception? Exception);
