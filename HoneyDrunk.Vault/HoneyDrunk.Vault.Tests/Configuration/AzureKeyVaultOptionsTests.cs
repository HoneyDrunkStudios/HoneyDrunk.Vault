using HoneyDrunk.Vault.Providers.AzureKeyVault.Configuration;

namespace HoneyDrunk.Vault.Tests.Configuration;

/// <summary>
/// Tests for <see cref="AzureKeyVaultOptions"/>.
/// </summary>
public sealed class AzureKeyVaultOptionsTests
{
    /// <summary>
    /// Verifies the defaults — managed identity enabled, no URI/credentials populated.
    /// </summary>
    [Fact]
    public void Defaults_MatchManagedIdentityShape()
    {
        var options = new AzureKeyVaultOptions();

        Assert.Null(options.VaultUri);
        Assert.True(options.UseManagedIdentity);
        Assert.Null(options.TenantId);
        Assert.Null(options.ClientId);
        Assert.Null(options.ClientSecret);
    }

    /// <summary>
    /// Verifies that properties are settable.
    /// </summary>
    [Fact]
    public void Properties_AreSettable()
    {
        var uri = new Uri("https://kv.example.net/");
        var options = new AzureKeyVaultOptions
        {
            VaultUri = uri,
            UseManagedIdentity = false,
            TenantId = "tenant",
            ClientId = "client",
            ClientSecret = "secret",
        };

        Assert.Equal(uri, options.VaultUri);
        Assert.False(options.UseManagedIdentity);
        Assert.Equal("tenant", options.TenantId);
        Assert.Equal("client", options.ClientId);
        Assert.Equal("secret", options.ClientSecret);
    }
}
