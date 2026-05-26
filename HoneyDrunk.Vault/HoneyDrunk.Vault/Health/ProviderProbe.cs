namespace HoneyDrunk.Vault.Health;

/// <summary>
/// Shared probe helper used by <see cref="VaultHealthContributor"/> and <see cref="VaultReadinessContributor"/>
/// so the try/catch shape lives in one place and each contributor focuses on its own bucket bookkeeping.
/// </summary>
internal static class ProviderProbe
{
    /// <summary>
    /// Runs a single provider probe and captures the outcome without throwing.
    /// </summary>
    /// <param name="probe">The probe delegate (typically <c>provider.CheckHealthAsync</c>).</param>
    /// <param name="cancellationToken">Cancellation token forwarded to the probe.</param>
    /// <returns>A <see cref="ProbeOutcome"/> describing the result.</returns>
    public static async Task<ProbeOutcome> RunAsync(Func<CancellationToken, Task<bool>> probe, CancellationToken cancellationToken)
    {
        try
        {
            var isHealthy = await probe(cancellationToken).ConfigureAwait(false);
            return new ProbeOutcome(isHealthy, null);
        }
        catch (Exception ex)
        {
            return new ProbeOutcome(false, ex);
        }
    }
}
