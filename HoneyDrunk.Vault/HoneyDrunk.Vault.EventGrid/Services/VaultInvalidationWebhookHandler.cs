using Azure.Messaging;
using Azure.Messaging.EventGrid;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.EventGrid.Constants;
using HoneyDrunk.Vault.EventGrid.Models;
using HoneyDrunk.Vault.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace HoneyDrunk.Vault.EventGrid.Services;

/// <summary>
/// Handles Event Grid webhook requests that invalidate <see cref="ISecretCacheInvalidator"/> entries.
/// This supports ADR-0006 Tier 3 and preserves invariant 21 by forcing the next read to resolve latest.
/// </summary>
public sealed class VaultInvalidationWebhookHandler(
    ISecretStore secretStore,
    ISecretCacheInvalidator cacheInvalidator,
    ILogger<VaultInvalidationWebhookHandler> logger)
{
    private readonly ISecretStore _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    private readonly ISecretCacheInvalidator _cacheInvalidator = cacheInvalidator ?? throw new ArgumentNullException(nameof(cacheInvalidator));
    private readonly ILogger<VaultInvalidationWebhookHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Handles an Event Grid webhook request.
    /// </summary>
    /// <param name="request">The webhook request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The webhook response.</returns>
    public async Task<VaultInvalidationWebhookResponse> HandleAsync(
        VaultInvalidationWebhookRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await IsAuthenticatedAsync(request.Headers, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogWarning("Rejected Vault invalidation webhook request due to invalid authentication");
            return new VaultInvalidationWebhookResponse(StatusCodes.Status401Unauthorized);
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            return new VaultInvalidationWebhookResponse(StatusCodes.Status400BadRequest);
        }

        var body = BinaryData.FromString(request.Body);

        if (TryParseEventGridEvents(body, out var eventGridEvents))
        {
            return HandleEventGridEvents(eventGridEvents);
        }

        if (TryParseCloudEvents(body, out var cloudEvents))
        {
            return HandleCloudEvents(cloudEvents);
        }

        return new VaultInvalidationWebhookResponse(StatusCodes.Status400BadRequest);
    }

    private static bool FixedTimeEquals(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var providedBytes = Encoding.UTF8.GetBytes(provided);
        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }

    private static bool TryParseCloudEvents(BinaryData body, out CloudEvent[] cloudEvents)
    {
        try
        {
            cloudEvents = body.ToObjectFromJson<CloudEvent[]>() ?? [];
            return cloudEvents.Length > 0;
        }
        catch
        {
            cloudEvents = [];
            return false;
        }
    }

    private static bool TryParseEventGridEvents(BinaryData body, out EventGridEvent[] eventGridEvents)
    {
        try
        {
            eventGridEvents = EventGridEvent.ParseMany(body);
            return eventGridEvents.Length > 0;
        }
        catch
        {
            eventGridEvents = [];
            return false;
        }
    }

    private static bool TryGetHeader(
        IReadOnlyDictionary<string, string?> headers,
        string key,
        out string? value)
    {
        foreach (var pair in headers)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static VaultInvalidationWebhookResponse BuildInvalidationResponse(int invalidatedCount)
    {
        return new VaultInvalidationWebhookResponse(
            StatusCodes.Status200OK,
            $$"""{"invalidated":{{invalidatedCount}}}""");
    }

    private static VaultInvalidationWebhookResponse BuildValidationResponse(string validationCode)
    {
        return new VaultInvalidationWebhookResponse(
            StatusCodes.Status200OK,
            $$"""{"validationResponse":"{{validationCode}}"}""");
    }

    private static string? TryGetSecretName(CloudEvent cloudEvent)
    {
        if (!string.Equals(
            cloudEvent.Type,
            VaultInvalidationWebhookConstants.SecretNewVersionCreatedEventType,
            StringComparison.Ordinal))
        {
            return null;
        }

        return TryGetDataString(cloudEvent.Data, "objectName");
    }

    private static string? TryGetSecretName(EventGridEvent eventGridEvent)
    {
        if (!string.Equals(
            eventGridEvent.EventType,
            VaultInvalidationWebhookConstants.SecretNewVersionCreatedEventType,
            StringComparison.Ordinal))
        {
            return null;
        }

        return TryGetDataString(eventGridEvent.Data, "objectName");
    }

    private static string? TryGetValidationCode(BinaryData? data)
    {
        return TryGetDataString(data, "validationCode");
    }

    private static string? TryGetDataString(BinaryData? data, string propertyName)
    {
        if (data == null)
        {
            return null;
        }

        using var document = JsonDocument.Parse(data);
        return document.RootElement.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private async Task<bool> IsAuthenticatedAsync(
        IReadOnlyDictionary<string, string?> headers,
        CancellationToken cancellationToken)
    {
        if (!TryGetHeader(headers, VaultInvalidationWebhookConstants.SharedSecretHeaderName, out var providedSecret) ||
            string.IsNullOrEmpty(providedSecret))
        {
            return false;
        }

        var expectedSecretResult = await _secretStore.TryGetSecretAsync(
            new SecretIdentifier(VaultInvalidationWebhookConstants.SharedSecretName),
            cancellationToken).ConfigureAwait(false);

        if (!expectedSecretResult.IsSuccess || expectedSecretResult.Value == null)
        {
            _logger.LogWarning(
                "Vault invalidation webhook secret '{SecretName}' is not configured",
                VaultInvalidationWebhookConstants.SharedSecretName);
            return false;
        }

        return FixedTimeEquals(expectedSecretResult.Value.Value, providedSecret);
    }

    private VaultInvalidationWebhookResponse HandleCloudEvents(IEnumerable<CloudEvent> cloudEvents)
    {
        var invalidatedCount = 0;
        foreach (var cloudEvent in cloudEvents)
        {
            if (string.Equals(
                cloudEvent.Type,
                VaultInvalidationWebhookConstants.SubscriptionValidationEventType,
                StringComparison.Ordinal))
            {
                var validationCode = TryGetValidationCode(cloudEvent.Data);
                if (!string.IsNullOrWhiteSpace(validationCode))
                {
                    _logger.LogInformation("Completed Event Grid subscription validation handshake");
                    return BuildValidationResponse(validationCode);
                }
            }

            var secretName = TryGetSecretName(cloudEvent);
            if (string.IsNullOrWhiteSpace(secretName))
            {
                continue;
            }

            _cacheInvalidator.Invalidate(secretName);
            invalidatedCount++;
            _logger.LogInformation("Invalidated Vault cache for secret '{SecretName}'", secretName);
        }

        return BuildInvalidationResponse(invalidatedCount);
    }

    private VaultInvalidationWebhookResponse HandleEventGridEvents(IEnumerable<EventGridEvent> eventGridEvents)
    {
        var invalidatedCount = 0;
        foreach (var eventGridEvent in eventGridEvents)
        {
            if (string.Equals(
                eventGridEvent.EventType,
                VaultInvalidationWebhookConstants.SubscriptionValidationEventType,
                StringComparison.Ordinal))
            {
                var validationCode = TryGetValidationCode(eventGridEvent.Data);
                if (!string.IsNullOrWhiteSpace(validationCode))
                {
                    _logger.LogInformation("Completed Event Grid subscription validation handshake");
                    return BuildValidationResponse(validationCode);
                }
            }

            var secretName = TryGetSecretName(eventGridEvent);
            if (string.IsNullOrWhiteSpace(secretName))
            {
                continue;
            }

            _cacheInvalidator.Invalidate(secretName);
            invalidatedCount++;
            _logger.LogInformation("Invalidated Vault cache for secret '{SecretName}'", secretName);
        }

        return BuildInvalidationResponse(invalidatedCount);
    }
}
