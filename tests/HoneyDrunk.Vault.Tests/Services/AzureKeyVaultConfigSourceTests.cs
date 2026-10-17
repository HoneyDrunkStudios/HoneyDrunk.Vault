using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="AzureKeyVaultConfigSource"/>. The implementation delegates to
/// <see cref="ISecretStore"/>, so the tests substitute the store rather than the
/// underlying <c>SecretClient</c> SDK type.
/// </summary>
public sealed class AzureKeyVaultConfigSourceTests
{
    /// <summary>
    /// Verifies that GetConfigValueAsync returns the secret value when the underlying
    /// secret resolves.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetConfigValueAsync_ReturnsSecretValue()
    {
        var secretStore = Substitute.For<ISecretStore>();
        var identifier = new SecretIdentifier("App-Name");
        secretStore.GetSecretAsync(Arg.Any<SecretIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(new SecretValue(identifier, "Vault", "latest"));
        var source = new AzureKeyVaultConfigSource(secretStore, NullLogger<AzureKeyVaultConfigSource>.Instance);

        var value = await source.GetConfigValueAsync("App-Name");

        Assert.Equal("Vault", value);
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync normalizes colon-separated keys to hyphenated
    /// Key Vault names before calling the secret store.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetConfigValueAsync_NormalizesColonAndDoubleUnderscoreKeys()
    {
        var secretStore = Substitute.For<ISecretStore>();
        secretStore.GetSecretAsync(Arg.Any<SecretIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new SecretValue(callInfo.Arg<SecretIdentifier>(), "ok", "latest"));
        var source = new AzureKeyVaultConfigSource(secretStore, NullLogger<AzureKeyVaultConfigSource>.Instance);

        await source.GetConfigValueAsync("Section:Sub__Key.Dotted");

        await secretStore.Received().GetSecretAsync(
            Arg.Is<SecretIdentifier>(id => id.Name == "Section-Sub-Key-Dotted"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync wraps SecretNotFoundException as
    /// ConfigurationNotFoundException.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetConfigValueAsync_WrapsSecretNotFoundAsConfigurationNotFound()
    {
        var secretStore = Substitute.For<ISecretStore>();
        secretStore.GetSecretAsync(Arg.Any<SecretIdentifier>(), Arg.Any<CancellationToken>())
            .Returns<Task<SecretValue>>(_ => throw new SecretNotFoundException("missing"));
        var source = new AzureKeyVaultConfigSource(secretStore, NullLogger<AzureKeyVaultConfigSource>.Instance);

        await Assert.ThrowsAsync<ConfigurationNotFoundException>(
            () => source.GetConfigValueAsync("missing"));
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync rejects null/whitespace keys.
    /// </summary>
    /// <param name="key">The invalid key to test.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetConfigValueAsync_ThrowsForInvalidKey(string? key)
    {
        var source = new AzureKeyVaultConfigSource(
            Substitute.For<ISecretStore>(),
            NullLogger<AzureKeyVaultConfigSource>.Instance);

        await Assert.ThrowsAsync<ArgumentException>(() => source.GetConfigValueAsync(key!));
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync returns the value on success.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_ReturnsValue_OnSuccess()
    {
        var secretStore = Substitute.For<ISecretStore>();
        var identifier = new SecretIdentifier("App-Name");
        secretStore.TryGetSecretAsync(Arg.Any<SecretIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(VaultResult.Success(new SecretValue(identifier, "Vault", "latest")));
        var source = new AzureKeyVaultConfigSource(secretStore, NullLogger<AzureKeyVaultConfigSource>.Instance);

        var value = await source.TryGetConfigValueAsync("App-Name");

        Assert.Equal("Vault", value);
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync returns null when the store returns a failure
    /// result.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_ReturnsNull_OnFailureResult()
    {
        var secretStore = Substitute.For<ISecretStore>();
        secretStore.TryGetSecretAsync(Arg.Any<SecretIdentifier>(), Arg.Any<CancellationToken>())
            .Returns(VaultResult.Failure<SecretValue>("not found"));
        var source = new AzureKeyVaultConfigSource(secretStore, NullLogger<AzureKeyVaultConfigSource>.Instance);

        var value = await source.TryGetConfigValueAsync("missing");

        Assert.Null(value);
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync swallows exceptions from the store and
    /// returns null.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_ReturnsNull_OnStoreException()
    {
        var secretStore = Substitute.For<ISecretStore>();
        secretStore.TryGetSecretAsync(Arg.Any<SecretIdentifier>(), Arg.Any<CancellationToken>())
            .Returns<Task<VaultResult<SecretValue>>>(_ => throw new InvalidOperationException("boom"));
        var source = new AzureKeyVaultConfigSource(secretStore, NullLogger<AzureKeyVaultConfigSource>.Instance);

        var value = await source.TryGetConfigValueAsync("App-Name");

        Assert.Null(value);
    }

    /// <summary>
    /// Verifies that constructor rejects null arguments.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AzureKeyVaultConfigSource(null!, NullLogger<AzureKeyVaultConfigSource>.Instance));
        Assert.Throws<ArgumentNullException>(() =>
            new AzureKeyVaultConfigSource(Substitute.For<ISecretStore>(), null!));
    }
}
