namespace HoneyDrunk.Vault.Tests.Canary;

/// <summary>
/// Exception thrown when a canary invariant check fails.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CanaryInvariantException"/> class.
/// </remarks>
/// <param name="invariantName">The name of the violated invariant.</param>
/// <param name="details">Detailed information about the violation.</param>
public sealed class CanaryInvariantException(string invariantName, string details) : Exception($"Invariant '{invariantName}' violated: {details}")
{
    /// <summary>
    /// Gets the name of the violated invariant.
    /// </summary>
    public string InvariantName { get; } = invariantName;

    /// <summary>
    /// Gets detailed information about the violation.
    /// </summary>
    public string Details { get; } = details;
}
