namespace HoneyDrunk.Vault.EventGrid.Models;

/// <summary>
/// Represents a webhook request for Vault cache invalidation.
/// </summary>
/// <param name="Headers">The request headers.</param>
/// <param name="Body">The raw request body.</param>
public sealed record VaultInvalidationWebhookRequest(
    IReadOnlyDictionary<string, string?> Headers,
    string Body);
