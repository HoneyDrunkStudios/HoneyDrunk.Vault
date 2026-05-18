using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="SecretStoreFacade"/>.
/// </summary>
public sealed class SecretStoreFacadeTests
{
    /// <summary>
    /// Verifies that cooperative cancellation is not converted into a failed Vault result.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetSecretAsync_RethrowsOperationCanceledException_WhenCancellationRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var identifier = new SecretIdentifier("api-key");

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            SecretStoreFacade.TryGetSecretAsync(
                identifier,
                static (_, cancellationToken) => Task.FromCanceled<SecretValue>(cancellationToken),
                NullLogger.Instance,
                "test store",
                cts.Token));
    }
}
