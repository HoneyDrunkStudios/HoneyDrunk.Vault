using HoneyDrunk.Vault.Telemetry;
using System.Diagnostics;

namespace HoneyDrunk.Vault.Tests.Telemetry;

/// <summary>
/// Tests for <see cref="VaultTelemetry"/> to ensure secrets are not leaked in telemetry.
/// </summary>
public sealed class VaultTelemetryTests : IDisposable
{
    private readonly ActivitySource _activitySource;
    private readonly ActivityListener _listener;
    private readonly List<Activity> _recordedActivities = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultTelemetryTests"/> class.
    /// </summary>
    public VaultTelemetryTests()
    {
        _activitySource = new ActivitySource("HoneyDrunk.Vault.Tests");
        _listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStarted = _recordedActivities.Add,
        };
        ActivitySource.AddActivityListener(_listener);
    }

    /// <summary>
    /// Verifies that activity tags don't contain secret values.
    /// </summary>
    [Fact]
    public void Activity_DoesNotContainSecretValue_InTags()
    {
        // This test verifies that no secret values appear in telemetry tags
        // Arrange
        const string secretName = "super-secret-key";
        const string secretValue = "super-secret-password-12345!@#$%";

        using var activity = _activitySource.StartActivity("vault.secret.get");
        Assert.NotNull(activity);

        // Act - Set appropriate tags (name is OK, but NOT the value)
        activity.SetTag(VaultTelemetryTags.Key, secretName);
        activity.SetTag(VaultTelemetryTags.Provider, "in-memory");

        // Assert - Verify no tags contain the secret value
        foreach (var tag in activity.Tags)
        {
            Assert.DoesNotContain(secretValue, tag.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual("value", tag.Key.ToLowerInvariant());
            Assert.NotEqual("secret", tag.Key.ToLowerInvariant());
            Assert.NotEqual("secretvalue", tag.Key.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Verifies that activity baggage doesn't contain secret values.
    /// </summary>
    [Fact]
    public void Activity_DoesNotContainSecretValue_InBaggage()
    {
        // Arrange
        const string secretValue = "password123";

        using var activity = _activitySource.StartActivity("vault.secret.get");
        Assert.NotNull(activity);

        // Assert - Verify no baggage contains secret values
        foreach (var baggage in activity.Baggage)
        {
            Assert.DoesNotContain(secretValue, baggage.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            Assert.NotEqual("value", baggage.Key.ToLowerInvariant());
        }
    }

    /// <summary>
    /// Verifies that VaultTelemetryTags doesn't define a value tag.
    /// </summary>
    [Fact]
    public void VaultTelemetryTags_DoesNotDefineValueTag()
    {
        // This test ensures the telemetry tags class doesn't have a tag for secret values
        // Arrange
        var tagFields = typeof(VaultTelemetryTags).GetFields();

        // Assert - None of the tag names should allow storing secret values
        foreach (var field in tagFields)
        {
            var tagName = field.GetValue(null)?.ToString() ?? string.Empty;
            Assert.DoesNotContain("value", tagName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("password", tagName, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("secret", tagName, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    public void Dispose()
    {
        _listener.Dispose();
        _activitySource.Dispose();
    }
}
