using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.EventGrid.Constants;
using HoneyDrunk.Vault.EventGrid.Models;
using HoneyDrunk.Vault.EventGrid.Services;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Vault.Tests.EventGrid;

/// <summary>
/// Tests for <see cref="VaultInvalidationWebhookHandler"/>.
/// </summary>
public sealed class VaultInvalidationWebhookHandlerTests
{
    /// <summary>
    /// Verifies that unauthenticated webhook requests are rejected.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenSharedSecretIsMissing()
    {
        // Arrange
        var invalidator = new RecordingSecretCacheInvalidator();
        var handler = CreateHandler(invalidator, new SharedSecretStore());

        // Act
        var response = await handler.HandleAsync(new VaultInvalidationWebhookRequest(
            new Dictionary<string, string?>(),
            "[]"));

        // Assert
        Assert.Equal(401, response.StatusCode);
        Assert.Empty(invalidator.InvalidatedSecrets);
    }

    /// <summary>
    /// Verifies that requests with the wrong shared secret are rejected.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenSharedSecretIsWrong()
    {
        // Arrange
        var invalidator = new RecordingSecretCacheInvalidator();
        var handler = CreateHandler(invalidator, new SharedSecretStore());
        var request = new VaultInvalidationWebhookRequest(
            new Dictionary<string, string?>
            {
                [VaultInvalidationWebhookConstants.SharedSecretHeaderName] = "wrong-secret",
            },
            "[]");

        // Act
        var response = await handler.HandleAsync(request);

        // Assert
        Assert.Equal(401, response.StatusCode);
        Assert.Empty(invalidator.InvalidatedSecrets);
    }

    /// <summary>
    /// Verifies that requests are rejected when the shared secret is not configured in Vault.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenSharedSecretIsNotConfigured()
    {
        // Arrange
        var invalidator = new RecordingSecretCacheInvalidator();
        var handler = CreateHandler(invalidator, new MissingSharedSecretStore());
        var request = new VaultInvalidationWebhookRequest(
            CreateHeaders(),
            "[]");

        // Act
        var response = await handler.HandleAsync(request);

        // Assert
        Assert.Equal(401, response.StatusCode);
        Assert.Empty(invalidator.InvalidatedSecrets);
    }

    /// <summary>
    /// Verifies that the Event Grid validation handshake returns the validation code.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsync_ReturnsValidationResponse_ForSubscriptionValidationHandshake()
    {
        // Arrange
        var handler = CreateHandler(new RecordingSecretCacheInvalidator(), new SharedSecretStore());
        const string requestBody =
            """
            [
              {
                "id": "evt-validation-1",
                "eventType": "Microsoft.EventGrid.SubscriptionValidationEvent",
                "subject": "",
                "eventTime": "2026-04-12T12:00:00Z",
                "data": {
                  "validationCode": "abc123"
                },
                "dataVersion": "1",
                "metadataVersion": "1"
              }
            ]
            """;
        var request = new VaultInvalidationWebhookRequest(
            CreateHeaders(),
            requestBody);

        // Act
        var response = await handler.HandleAsync(request);

        // Assert
        Assert.Equal(200, response.StatusCode);
        Assert.NotNull(response.Body);
        Assert.Contains("validationResponse", response.Body, StringComparison.Ordinal);
        Assert.Contains("abc123", response.Body, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that legacy Event Grid schema events invalidate the named secret.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsync_InvalidatesSecret_ForLegacyEventGridSchema()
    {
        // Arrange
        var invalidator = new RecordingSecretCacheInvalidator();
        var handler = CreateHandler(invalidator, new SharedSecretStore());
        const string requestBody =
            """
            [
              {
                "id": "evt-secret-1",
                "eventType": "Microsoft.KeyVault.SecretNewVersionCreated",
                "subject": "DbPassword",
                "eventTime": "2026-04-12T12:00:00Z",
                "data": {
                  "id": "https://vault.vault.azure.net/secrets/DbPassword/version1",
                  "vaultName": "vault",
                  "objectType": "Secret",
                  "objectName": "DbPassword"
                },
                "dataVersion": "1",
                "metadataVersion": "1"
              }
            ]
            """;
        var request = new VaultInvalidationWebhookRequest(
            CreateHeaders(),
            requestBody);

        // Act
        var response = await handler.HandleAsync(request);

        // Assert
        Assert.Equal(200, response.StatusCode);
        Assert.Single(invalidator.InvalidatedSecrets);
        Assert.Equal("DbPassword", invalidator.InvalidatedSecrets[0]);
    }

    /// <summary>
    /// Verifies that CloudEvents schema events invalidate the named secret.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task HandleAsync_InvalidatesSecret_ForCloudEventsSchema()
    {
        // Arrange
        var invalidator = new RecordingSecretCacheInvalidator();
        var handler = CreateHandler(invalidator, new SharedSecretStore());
        const string requestBody =
            """
            [
              {
                "id": "evt-cloud-1",
                "source": "/subscriptions/test/resourceGroups/rg/providers/Microsoft.KeyVault/vaults/vault",
                "type": "Microsoft.KeyVault.SecretNewVersionCreated",
                "specversion": "1.0",
                "data": {
                  "id": "https://vault.vault.azure.net/secrets/ApiKey/version1",
                  "vaultName": "vault",
                  "objectType": "Secret",
                  "objectName": "ApiKey"
                }
              }
            ]
            """;
        var request = new VaultInvalidationWebhookRequest(
            CreateHeaders(),
            requestBody);

        // Act
        var response = await handler.HandleAsync(request);

        // Assert
        Assert.Equal(200, response.StatusCode);
        Assert.Single(invalidator.InvalidatedSecrets);
        Assert.Equal("ApiKey", invalidator.InvalidatedSecrets[0]);
    }

    private static VaultInvalidationWebhookHandler CreateHandler(
        RecordingSecretCacheInvalidator invalidator,
        ISecretStore secretStore)
    {
        return new VaultInvalidationWebhookHandler(
            secretStore,
            invalidator,
            NullLogger<VaultInvalidationWebhookHandler>.Instance);
    }

    private static Dictionary<string, string?> CreateHeaders()
    {
        return new Dictionary<string, string?>
        {
            [VaultInvalidationWebhookConstants.SharedSecretHeaderName] = SharedSecretStore.SharedSecretValue,
        };
    }

    private sealed class RecordingSecretCacheInvalidator : ISecretCacheInvalidator
    {
        public List<string> InvalidatedSecrets { get; } = [];

        public void Invalidate(string secretName)
        {
            InvalidatedSecrets.Add(secretName);
        }

        public void InvalidateAll()
        {
            InvalidatedSecrets.Add("*");
        }
    }

    private sealed class SharedSecretStore : ISecretStore
    {
        public const string SharedSecretValue = "expected-shared-secret";

        public Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SecretValue(identifier, SharedSecretValue, "v1"));
        }

        public Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(VaultResult.Success(new SecretValue(identifier, SharedSecretValue, "v1")));
        }

        public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SecretVersion>>([]);
        }
    }

    private sealed class MissingSharedSecretStore : ISecretStore
    {
        public Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("The handler should use TryGetSecretAsync for webhook secret lookup.");
        }

        public Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(VaultResult.Failure<SecretValue>("Secret not found"));
        }

        public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SecretVersion>>([]);
        }
    }
}
