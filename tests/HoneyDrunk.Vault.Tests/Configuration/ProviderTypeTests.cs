using HoneyDrunk.Vault.Configuration;

namespace HoneyDrunk.Vault.Tests.Configuration;

/// <summary>
/// Tests for <see cref="ProviderType"/>.
/// </summary>
public sealed class ProviderTypeTests
{
    /// <summary>
    /// Verifies that all provider types are defined.
    /// </summary>
    [Fact]
    public void AllProviderTypes_AreDefined()
    {
        // Assert
        Assert.True(Enum.IsDefined(ProviderType.File));
        Assert.True(Enum.IsDefined(ProviderType.AzureKeyVault));
        Assert.True(Enum.IsDefined(ProviderType.AwsSecretsManager));
        Assert.True(Enum.IsDefined(ProviderType.InMemory));
        Assert.True(Enum.IsDefined(ProviderType.Configuration));
    }

    /// <summary>
    /// Verifies that provider types can be converted to and from strings.
    /// </summary>
    /// <param name="type">The provider type to test.</param>
    /// <param name="expected">The expected string representation.</param>
    [Theory]
    [InlineData(ProviderType.File, "File")]
    [InlineData(ProviderType.AzureKeyVault, "AzureKeyVault")]
    [InlineData(ProviderType.AwsSecretsManager, "AwsSecretsManager")]
    [InlineData(ProviderType.InMemory, "InMemory")]
    [InlineData(ProviderType.Configuration, "Configuration")]
    public void ProviderType_StringConversion_WorksCorrectly(ProviderType type, string expected)
    {
        // Assert
        Assert.Equal(expected, type.ToString());
        Assert.True(Enum.TryParse<ProviderType>(expected, out var parsed));
        Assert.Equal(type, parsed);
    }
}
