using HoneyDrunk.Vault.EventGrid.Models;

namespace HoneyDrunk.Vault.EventGrid.Services;

/// <summary>
/// Functions-friendly wrapper around <see cref="VaultInvalidationWebhookHandler"/>.
/// </summary>
public sealed class VaultInvalidationFunctionHandler(VaultInvalidationWebhookHandler handler)
{
    private readonly VaultInvalidationWebhookHandler _handler = handler ?? throw new ArgumentNullException(nameof(handler));

    /// <summary>
    /// Handles a Function-hosted Event Grid webhook request.
    /// </summary>
    /// <param name="headers">The request headers.</param>
    /// <param name="body">The raw request body.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The webhook response.</returns>
    public Task<VaultInvalidationWebhookResponse> HandleAsync(
        IReadOnlyDictionary<string, string?> headers,
        string body,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(headers);

        return _handler.HandleAsync(new VaultInvalidationWebhookRequest(headers, body), cancellationToken);
    }
}
