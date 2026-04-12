namespace HoneyDrunk.Vault.EventGrid.Models;

/// <summary>
/// Represents the outcome of a Vault invalidation webhook request.
/// </summary>
/// <param name="StatusCode">The HTTP status code to return.</param>
/// <param name="Body">The optional response body.</param>
/// <param name="ContentType">The response content type.</param>
public sealed record VaultInvalidationWebhookResponse(
    int StatusCode,
    string? Body = null,
    string ContentType = "application/json");
