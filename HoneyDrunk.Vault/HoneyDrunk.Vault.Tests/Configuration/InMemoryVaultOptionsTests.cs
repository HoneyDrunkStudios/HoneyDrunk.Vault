using HoneyDrunk.Vault.Providers.InMemory.Configuration;

namespace HoneyDrunk.Vault.Tests.Configuration;

/// <summary>
/// Tests for <see cref="InMemoryVaultOptions"/>.
/// </summary>
public sealed class InMemoryVaultOptionsTests
{
    /// <summary>
    /// Verifies that the default options have empty case-insensitive dictionaries.
    /// </summary>
    [Fact]
    public void Defaults_ExposeEmptyCaseInsensitiveDictionaries()
    {
        var options = new InMemoryVaultOptions();

        Assert.Empty(options.Secrets);
        Assert.Empty(options.ConfigurationValues);
        options.Secrets["MIXED"] = "value";
        Assert.True(options.Secrets.ContainsKey("mixed"));
        options.ConfigurationValues["MIXED"] = "value";
        Assert.True(options.ConfigurationValues.ContainsKey("mixed"));
    }

    /// <summary>
    /// Verifies that AddSecret records the value and returns the options for chaining.
    /// </summary>
    [Fact]
    public void AddSecret_RecordsValueAndReturnsSelf()
    {
        var options = new InMemoryVaultOptions();

        var returned = options.AddSecret("api-key", "secret-value");

        Assert.Same(options, returned);
        Assert.Equal("secret-value", options.Secrets["api-key"]);
    }

    /// <summary>
    /// Verifies that AddConfigValue records the value and returns the options for chaining.
    /// </summary>
    [Fact]
    public void AddConfigValue_RecordsValueAndReturnsSelf()
    {
        var options = new InMemoryVaultOptions();

        var returned = options.AddConfigValue("Database:Host", "localhost");

        Assert.Same(options, returned);
        Assert.Equal("localhost", options.ConfigurationValues["Database:Host"]);
    }

    /// <summary>
    /// Verifies that AddSecret overwrites an existing value.
    /// </summary>
    [Fact]
    public void AddSecret_OverwritesExistingValue()
    {
        var options = new InMemoryVaultOptions();
        options.AddSecret("api-key", "first");
        options.AddSecret("api-key", "second");

        Assert.Equal("second", options.Secrets["api-key"]);
    }
}
