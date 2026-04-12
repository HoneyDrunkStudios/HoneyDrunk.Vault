namespace HoneyDrunk.Vault.Abstractions;

/// <summary>
/// Invalidates cached secret entries so rotated secrets can propagate within seconds per ADR-0006 Tier 3.
/// This contract exists to uphold invariant 21 by keeping callers pinned to latest-version secret resolution.
/// </summary>
public interface ISecretCacheInvalidator
{
    /// <summary>
    /// Invalidates the cached entry for the specified secret name.
    /// </summary>
    /// <param name="secretName">The secret name to invalidate.</param>
    void Invalidate(string secretName);

    /// <summary>
    /// Invalidates all cached secret entries.
    /// </summary>
    void InvalidateAll();
}
