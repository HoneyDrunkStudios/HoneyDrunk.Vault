using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.Aws.Configuration;
using HoneyDrunk.Vault.Providers.Aws.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="AwsSecretsManagerSecretStore"/> using NSubstitute against
/// <see cref="IAmazonSecretsManager"/>.
/// </summary>
public sealed class AwsSecretsManagerSecretStoreTests
{
    /// <summary>
    /// Verifies that GetSecretAsync returns the secret value and version when the
    /// SDK returns a successful response.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetSecretAsync_ReturnsSecretValue()
    {
        var client = Substitute.For<IAmazonSecretsManager>();
        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetSecretValueResponse
            {
                SecretString = "secret-value",
                VersionStages = new List<string> { "AWSCURRENT" },
            });

        // UseVersionId = false → the secret store reports the version stage instead of the GUID.
        using var store = CreateStore(client, new AwsSecretsManagerOptions { UseVersionId = false });
        var value = await store.GetSecretAsync(new SecretIdentifier("api-key"));

        Assert.Equal("secret-value", value.Value);
        Assert.Equal("AWSCURRENT", value.Version);
    }

    /// <summary>
    /// Verifies that GetSecretAsync prefers VersionId when UseVersionId is enabled.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetSecretAsync_UsesVersionId_WhenConfigured()
    {
        var client = Substitute.For<IAmazonSecretsManager>();
        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetSecretValueResponse
            {
                SecretString = "value",
                VersionId = "00000000000000000000000000000001",
                VersionStages = new List<string> { "AWSCURRENT" },
            });

        using var store = CreateStore(client, new AwsSecretsManagerOptions { UseVersionId = true });
        var value = await store.GetSecretAsync(new SecretIdentifier("api-key"));

        Assert.Equal("00000000000000000000000000000001", value.Version);
    }

    /// <summary>
    /// Verifies that GetSecretAsync wraps ResourceNotFoundException as SecretNotFoundException.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetSecretAsync_WrapsResourceNotFound()
    {
        var client = Substitute.For<IAmazonSecretsManager>();
        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<GetSecretValueResponse>>(_ => throw new ResourceNotFoundException("missing"));

        using var store = CreateStore(client);
        await Assert.ThrowsAsync<SecretNotFoundException>(
            () => store.GetSecretAsync(new SecretIdentifier("missing")));
    }

    /// <summary>
    /// Verifies that GetSecretAsync wraps generic AWS exceptions as VaultOperationException.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetSecretAsync_WrapsAmazonException()
    {
        var client = Substitute.For<IAmazonSecretsManager>();
        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<GetSecretValueResponse>>(_ => throw new AmazonSecretsManagerException("boom"));

        using var store = CreateStore(client);
        await Assert.ThrowsAsync<VaultOperationException>(
            () => store.GetSecretAsync(new SecretIdentifier("any")));
    }

    /// <summary>
    /// Verifies that the secret prefix is applied to the lookup key.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetSecretAsync_AppliesSecretPrefix()
    {
        var client = Substitute.For<IAmazonSecretsManager>();
        client.GetSecretValueAsync(Arg.Any<GetSecretValueRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetSecretValueResponse { SecretString = "v", VersionStages = new List<string> { "AWSCURRENT" } });

        using var store = CreateStore(client, new AwsSecretsManagerOptions { SecretPrefix = "prod/" });
        await store.GetSecretAsync(new SecretIdentifier("api-key"));

        await client.Received().GetSecretValueAsync(
            Arg.Is<GetSecretValueRequest>(r => r.SecretId == "prod/api-key"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that ListSecretVersionsAsync returns the paginated versions.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ListSecretVersionsAsync_ReturnsVersions()
    {
        var client = Substitute.For<IAmazonSecretsManager>();
        client.ListSecretVersionIdsAsync(Arg.Any<ListSecretVersionIdsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ListSecretVersionIdsResponse
            {
                Versions = new List<SecretVersionsListEntry>
                {
                    new SecretVersionsListEntry
                    {
                        VersionStages = new List<string> { "AWSCURRENT" },
                        CreatedDate = DateTime.UtcNow,
                    },
                },
            });

        using var store = CreateStore(client, new AwsSecretsManagerOptions { UseVersionId = false });
        var versions = await store.ListSecretVersionsAsync("api-key");

        Assert.Single(versions);
        Assert.Equal("AWSCURRENT", versions[0].Version);
    }

    /// <summary>
    /// Verifies that ListSecretVersionsAsync wraps ResourceNotFoundException.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ListSecretVersionsAsync_WrapsResourceNotFound()
    {
        var client = Substitute.For<IAmazonSecretsManager>();
        client.ListSecretVersionIdsAsync(Arg.Any<ListSecretVersionIdsRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<ListSecretVersionIdsResponse>>(_ => throw new ResourceNotFoundException("missing"));

        using var store = CreateStore(client);
        await Assert.ThrowsAsync<SecretNotFoundException>(
            () => store.ListSecretVersionsAsync("missing"));
    }

    /// <summary>
    /// Verifies that CheckHealthAsync reports true on a successful list-secrets call.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckHealthAsync_ReturnsTrue_OnSuccessfulPing()
    {
        var client = Substitute.For<IAmazonSecretsManager>();
        client.ListSecretsAsync(Arg.Any<ListSecretsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ListSecretsResponse());

        using var store = CreateStore(client);

        Assert.True(await store.CheckHealthAsync());
    }

    /// <summary>
    /// Verifies that CheckHealthAsync swallows exceptions and reports false.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CheckHealthAsync_ReturnsFalse_OnException()
    {
        var client = Substitute.For<IAmazonSecretsManager>();
        client.ListSecretsAsync(Arg.Any<ListSecretsRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<ListSecretsResponse>>(_ => throw new InvalidOperationException("boom"));

        using var store = CreateStore(client);

        Assert.False(await store.CheckHealthAsync());
    }

    /// <summary>
    /// Verifies that constructor rejects null options.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNullOptions()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AwsSecretsManagerSecretStore(
                options: null!,
                Substitute.For<IAmazonSecretsManager>(),
                NullLogger<AwsSecretsManagerSecretStore>.Instance));
    }

    /// <summary>
    /// Verifies that Dispose does not throw and is idempotent.
    /// </summary>
    [Fact]
    public void Dispose_IsIdempotent()
    {
        var client = Substitute.For<IAmazonSecretsManager>();

        // The `using` block's automatic Dispose IS the second call. If the
        // explicit store.Dispose() below throws, the using cleanup still runs
        // (CodeQL cs/dispose-not-called-on-throw); if the explicit Dispose
        // succeeds, the using cleanup then verifies the idempotent path.
        using var store = CreateStore(client);
        store.Dispose();
    }

    private static AwsSecretsManagerSecretStore CreateStore(
        IAmazonSecretsManager client,
        AwsSecretsManagerOptions? options = null)
    {
        return new AwsSecretsManagerSecretStore(
            Options.Create(options ?? new AwsSecretsManagerOptions()),
            client,
            NullLogger<AwsSecretsManagerSecretStore>.Instance);
    }
}
