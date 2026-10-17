using HoneyDrunk.Vault.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace HoneyDrunk.Vault.Tests.Telemetry;

/// <summary>
/// Behavioral tests for <see cref="VaultTelemetry"/> operation execution.
/// </summary>
public sealed class VaultTelemetryBehaviorTests : IDisposable
{
    private static readonly string[] HitOrMiss = ["hit", "miss"];
    private readonly ActivityListener _listener;
    private readonly List<Activity> _stoppedActivities = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultTelemetryBehaviorTests"/> class.
    /// </summary>
    public VaultTelemetryBehaviorTests()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == VaultTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = _stoppedActivities.Add,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>
    /// Verifies successful operations return their result and tag the activity with provider/key metadata.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteWithTelemetryAsync_ReturnsResultAndTagsActivity_WhenOperationSucceeds()
    {
        // Arrange
        var telemetry = new VaultTelemetry(NullLogger<VaultTelemetry>.Instance);

        // Act
        var result = await telemetry.ExecuteWithTelemetryAsync(
            "get_secret",
            "in-memory",
            "api-key",
            static _ => Task.FromResult("secret-value"));

        // Assert
        Assert.Equal("secret-value", result);
        var activity = Assert.Single(_stoppedActivities);
        Assert.Equal("vault.get_secret", activity.OperationName);
        Assert.Equal(ActivityKind.Client, activity.Kind);
        Assert.Equal("in-memory", GetTag(activity, "vault.provider"));
        Assert.Equal("api-key", GetTag(activity, "vault.key"));
        Assert.Equal("success", GetTag(activity, "vault.result"));
        Assert.Contains(GetTag(activity, "vault.cache"), HitOrMiss);
        foreach (var tagValue in activity.Tags.Select(tag => tag.Value).Where(value => value is not null))
        {
            Assert.DoesNotContain("secret-value", tagValue, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Verifies failed operations tag the activity as an error and rethrow the original exception.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteWithTelemetryAsync_TagsActivityAndRethrows_WhenOperationFails()
    {
        // Arrange
        var telemetry = new VaultTelemetry(NullLogger<VaultTelemetry>.Instance);
        var expected = new InvalidOperationException("boom");

        // Act
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            telemetry.ExecuteWithTelemetryAsync<string>(
                "get_secret",
                "file",
                "api-key",
                _ => Task.FromException<string>(expected)));

        // Assert
        Assert.Same(expected, actual);
        var activity = Assert.Single(_stoppedActivities);
        Assert.Equal("error", GetTag(activity, "vault.result"));
        Assert.Equal(ActivityStatusCode.Error, activity.Status);
        Assert.Equal("boom", activity.StatusDescription);
    }

    /// <summary>
    /// Verifies cache telemetry helper methods can be invoked without an active activity.
    /// </summary>
    [Fact]
    public void CacheTelemetryHelpers_CompleteWithoutActivity()
    {
        // Arrange
        var telemetry = new VaultTelemetry(NullLogger<VaultTelemetry>.Instance);

        // Act
        telemetry.RecordCacheHit("api-key");
        telemetry.RecordCacheMiss("api-key");

        // Assert
        Assert.Empty(_stoppedActivities);
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    public void Dispose()
    {
        _listener.Dispose();
    }

    private static string? GetTag(Activity activity, string key)
    {
        return activity.Tags.SingleOrDefault(tag => tag.Key == key).Value;
    }
}
