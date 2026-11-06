namespace HoneyDrunk.Vault.Services;

/// <summary>Selects available providers in stable registration-priority order.</summary>
internal static class ProviderSelection
{
    /// <summary>Materializes the eligible providers once without changing equal-priority registration order.</summary>
    /// <typeparam name="TProvider">The registered provider type.</typeparam>
    /// <param name="providers">The registrations already checked by the owning constructor.</param>
    /// <param name="isEligible">The enabled and available check for this provider kind.</param>
    /// <param name="priority">The registration priority selector.</param>
    /// <returns>The selected providers in ascending priority order.</returns>
    internal static IReadOnlyList<TProvider> OrderAvailable<TProvider>(
        IEnumerable<TProvider> providers,
        Func<TProvider, bool> isEligible,
        Func<TProvider, int> priority) =>
        [.. providers.Where(isEligible).OrderBy(priority)];
}
