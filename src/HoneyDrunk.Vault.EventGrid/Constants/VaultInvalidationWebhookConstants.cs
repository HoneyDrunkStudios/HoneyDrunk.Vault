namespace HoneyDrunk.Vault.EventGrid.Constants;

/// <summary>
/// Constants for Vault Event Grid invalidation handlers.
/// </summary>
public static class VaultInvalidationWebhookConstants
{
    /// <summary>
    /// The secret name used to look up the webhook shared secret via <c>ISecretStore</c>.
    /// </summary>
    public const string SharedSecretName = "VaultInvalidationWebhookSecret";

    /// <summary>
    /// The header name that carries the shared secret.
    /// </summary>
    public const string SharedSecretHeaderName = "X-HoneyDrunk-Vault-Webhook-Secret";

    /// <summary>
    /// The Event Grid event type for Key Vault secret version creation.
    /// </summary>
    public const string SecretNewVersionCreatedEventType = "Microsoft.KeyVault.SecretNewVersionCreated";

    /// <summary>
    /// The Event Grid subscription validation event type.
    /// </summary>
    public const string SubscriptionValidationEventType = "Microsoft.EventGrid.SubscriptionValidationEvent";
}
