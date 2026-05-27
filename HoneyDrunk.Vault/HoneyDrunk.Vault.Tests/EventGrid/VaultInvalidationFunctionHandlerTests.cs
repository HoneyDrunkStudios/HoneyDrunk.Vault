using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.EventGrid.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace HoneyDrunk.Vault.Tests.EventGrid;

/// <summary>
/// Tests for <see cref="VaultInvalidationFunctionHandler"/>. The handler is a thin
/// wrapper around <see cref="VaultInvalidationWebhookHandler"/>; the tests focus on
/// argument validation and request-shape forwarding.
/// </summary>
public sealed class VaultInvalidationFunctionHandlerTests
{
    /// <summary>
    /// Verifies that the constructor rejects a null webhook handler.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNullHandler()
    {
        Assert.Throws<ArgumentNullException>(() => new VaultInvalidationFunctionHandler(null!));
    }

    /// <summary>
    /// Verifies that HandleAsync rejects null headers.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task HandleAsync_ThrowsForNullHeaders()
    {
        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(headers: null!, body: "[]"));
    }

    /// <summary>
    /// Verifies that HandleAsync forwards through to the webhook handler and returns
    /// 401 Unauthorized when no shared-secret header is provided (proving the bridge works).
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task HandleAsync_ReturnsUnauthorized_WhenSharedSecretHeaderMissing()
    {
        var handler = CreateHandler();

        var response = await handler.HandleAsync(
            new Dictionary<string, string?>(),
            body: "[]");

        Assert.Equal(401, response.StatusCode);
    }

    private static VaultInvalidationFunctionHandler CreateHandler()
    {
        var webhookHandler = new VaultInvalidationWebhookHandler(
            Substitute.For<ISecretStore>(),
            Substitute.For<ISecretCacheInvalidator>(),
            NullLogger<VaultInvalidationWebhookHandler>.Instance);

        return new VaultInvalidationFunctionHandler(webhookHandler);
    }
}
