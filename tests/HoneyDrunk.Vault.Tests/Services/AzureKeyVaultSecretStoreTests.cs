using Azure;
using Azure.Security.KeyVault.Secrets;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>Verifies the Key Vault SDK boundary without contacting Azure.</summary>
public sealed class AzureKeyVaultSecretStoreTests
{
    /// <summary>Versioned and latest lookups retain the requested identifier and returned version.</summary>
    /// <param name="version">The requested version, or null for latest.</param>
    /// <returns>The test task.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("v1")]
    public async Task LookupPreservesVersionAndCancellation(string? version)
    {
        var client = Substitute.For<SecretClient>();
        var properties = SecretModelFactory.SecretProperties(version: "v1", name: "example");
        var secret = SecretModelFactory.KeyVaultSecret(properties, "test-value");
        using var cancellation = new CancellationTokenSource();
        var response = Response.FromValue(secret, Substitute.For<Response>());
        if (version is null)
        {
            client.GetSecretAsync("example", cancellationToken: cancellation.Token).Returns(response);
        }
        else
        {
            client.GetSecretAsync("example", version, cancellation.Token).Returns(response);
        }

        var store = CreateStore(client);
        var identifier = new SecretIdentifier("example", version);

        var result = await store.GetSecretAsync(identifier, cancellation.Token);

        Assert.Equal("test-value", result.Value);
        Assert.Equal("v1", result.Version);
        Assert.Same(identifier, result.Identifier);
        Assert.Equal("azure-key-vault", store.ProviderName);
        Assert.True(store.IsAvailable);
        if (version is null)
        {
            await client.Received(1).GetSecretAsync("example", cancellationToken: cancellation.Token);
        }
        else
        {
            await client.Received(1).GetSecretAsync("example", version, cancellation.Token);
        }
    }

    /// <summary>Missing secrets are distinct from provider errors for reads and version listing.</summary>
    /// <param name="status">The simulated Azure status, or zero for a non-Azure failure.</param>
    /// <returns>The test task.</returns>
    [Theory]
    [InlineData(404)]
    [InlineData(403)]
    [InlineData(0)]
    public async Task ProviderFailuresPreserveTheirCause(int status)
    {
        var client = Substitute.For<SecretClient>();
        Exception failure = status == 0 ? new InvalidOperationException("provider failure") : new RequestFailedException(status, "provider failure");
        client.GetSecretAsync("example", cancellationToken: Arg.Any<CancellationToken>()).Returns<Task<Response<KeyVaultSecret>>>(_ => throw failure);
        client.GetPropertiesOfSecretVersionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns<AsyncPageable<SecretProperties>>(_ => throw failure);
        client.GetPropertiesOfSecretsAsync(Arg.Any<CancellationToken>()).Returns<AsyncPageable<SecretProperties>>(_ => throw failure);
        var store = CreateStore(client);
        var expected = status == 404 ? typeof(SecretNotFoundException) : typeof(VaultOperationException);

        var read = await Assert.ThrowsAsync(expected, () => store.GetSecretAsync(new SecretIdentifier("example")));
        var list = await Assert.ThrowsAsync(expected, () => store.ListSecretVersionsAsync("example"));

        Assert.Same(failure, read.InnerException);
        Assert.Same(failure, list.InnerException);
        Assert.False(await store.CheckHealthAsync());
    }

    /// <summary>Version listing filters disabled secrets, and an empty vault is healthy.</summary>
    /// <returns>The test task.</returns>
    [Fact]
    public async Task VersionListingFiltersDisabledSecrets()
    {
        var client = Substitute.For<SecretClient>();
        var created = DateTimeOffset.UtcNow.AddDays(-1);
        var enabled = SecretModelFactory.SecretProperties(version: "enabled", createdOn: created);
        enabled.Enabled = true;
        var disabled = SecretModelFactory.SecretProperties(version: "disabled");
        disabled.Enabled = false;
        var withoutTimestamp = SecretModelFactory.SecretProperties(version: "fallback");
        withoutTimestamp.Enabled = true;
        var page = Page<SecretProperties>.FromValues(new[] { enabled, disabled, withoutTimestamp }, null, Substitute.For<Response>());
        client.GetPropertiesOfSecretVersionsAsync("example", Arg.Any<CancellationToken>()).Returns(AsyncPageable<SecretProperties>.FromPages(new[] { page }));
        client.GetPropertiesOfSecretsAsync(Arg.Any<CancellationToken>()).Returns(AsyncPageable<SecretProperties>.FromPages(Array.Empty<Page<SecretProperties>>()));
        var store = CreateStore(client);
        var before = DateTimeOffset.UtcNow;

        var versions = await store.ListSecretVersionsAsync("example");

        Assert.Equal(2, versions.Count);
        Assert.Equal("enabled", versions[0].Version);
        Assert.Equal(created, versions[0].CreatedOn);
        Assert.Equal("fallback", versions[1].Version);
        Assert.InRange(versions[1].CreatedOn, before, DateTimeOffset.UtcNow);
        Assert.True(await store.CheckHealthAsync());
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.GetSecretAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(() => store.ListSecretVersionsAsync(" "));
    }

    private static AzureKeyVaultSecretStore CreateStore(SecretClient client) => new(client, NullLogger<AzureKeyVaultSecretStore>.Instance);
}
