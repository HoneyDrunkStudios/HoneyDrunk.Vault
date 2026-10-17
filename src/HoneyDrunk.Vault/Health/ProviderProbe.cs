using HoneyDrunk.Vault.Services;

namespace HoneyDrunk.Vault.Health;

/// <summary>
/// Shared probe helper used by <see cref="VaultHealthContributor"/> and <see cref="VaultReadinessContributor"/>
/// so the per-provider iteration + try/catch shape lives in one place and each contributor focuses on its
/// own bucket bookkeeping via the supplied classifier callback.
/// </summary>
internal static class ProviderProbe
{
    /// <summary>
    /// Iterates all enabled secret and config providers, probes each one's health endpoint, and routes
    /// the captured <see cref="ProbeOutcome"/> through the caller's classifier.
    /// </summary>
    /// <typeparam name="TBuckets">Caller-defined bucket container (struct or class) updated by the classifier.</typeparam>
    /// <param name="secretProviders">Registered secret providers.</param>
    /// <param name="configProviders">Registered config-source providers.</param>
    /// <param name="buckets">The bucket container forwarded to the classifier on each provider.</param>
    /// <param name="classify">Callback that updates the buckets based on the probe outcome.</param>
    /// <param name="cancellationToken">Cancellation token forwarded to each probe.</param>
    public static async Task ProbeAllAsync<TBuckets>(
        IEnumerable<RegisteredSecretProvider> secretProviders,
        IEnumerable<RegisteredConfigSourceProvider> configProviders,
        TBuckets buckets,
        ProbeClassifier<TBuckets> classify,
        CancellationToken cancellationToken)
    {
        foreach (var entry in secretProviders.Where(p => p.Registration.IsEnabled))
        {
            var outcome = await RunAsync(entry.Provider.CheckHealthAsync, cancellationToken).ConfigureAwait(false);
            classify("Secret", entry.Provider.ProviderName, entry.Registration.IsRequired, outcome, buckets);
        }

        foreach (var entry in configProviders.Where(p => p.Registration.IsEnabled))
        {
            var outcome = await RunAsync(entry.Provider.CheckHealthAsync, cancellationToken).ConfigureAwait(false);
            classify("Config", entry.Provider.ProviderName, entry.Registration.IsRequired, outcome, buckets);
        }
    }

    private static async Task<ProbeOutcome> RunAsync(Func<CancellationToken, Task<bool>> probe, CancellationToken cancellationToken)
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
