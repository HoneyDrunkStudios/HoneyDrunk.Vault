namespace HoneyDrunk.Vault.Health;

/// <summary>
/// Callback signature used by <see cref="ProviderProbe.ProbeAllAsync"/> to classify a single probe outcome
/// into caller-defined buckets.
/// </summary>
/// <typeparam name="TBuckets">Caller-defined bucket container forwarded back into the callback.</typeparam>
/// <param name="providerKind">The provider kind label (<c>"Secret"</c> or <c>"Config"</c>).</param>
/// <param name="providerName">The provider's stable name.</param>
/// <param name="isRequired">Whether the registration marked the provider as required.</param>
/// <param name="outcome">The captured probe outcome.</param>
/// <param name="buckets">The caller's bucket container.</param>
internal delegate void ProbeClassifier<in TBuckets>(string providerKind, string providerName, bool isRequired, ProbeOutcome outcome, TBuckets buckets);
