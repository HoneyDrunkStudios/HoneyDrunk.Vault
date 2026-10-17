using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="ConfigSourceFacade"/>.
/// </summary>
public sealed class ConfigSourceFacadeTests
{
    /// <summary>
    /// Verifies that try-get conversion returns the default value when conversion fails.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetValueAsync_ReturnsDefault_WhenConversionFails()
    {
        // Act
        var result = await ConfigSourceFacade.TryGetValueAsync(
            static (_, _) => Task.FromResult<string?>("not-an-int"),
            "timeout",
            30,
            logger: NullLogger.Instance);

        // Assert
        Assert.Equal(30, result);
    }

    /// <summary>
    /// Verifies that cooperative cancellation is not converted into a default value.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetValueAsync_RethrowsOperationCanceledException_WhenCancellationRequested()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ConfigSourceFacade.TryGetValueAsync(
                static (_, cancellationToken) => Task.FromCanceled<string?>(cancellationToken),
                "timeout",
                30,
                NullLogger.Instance,
                cts.Token));
    }
}
