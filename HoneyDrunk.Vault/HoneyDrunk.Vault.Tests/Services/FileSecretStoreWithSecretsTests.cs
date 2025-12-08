using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.File.Configuration;
using HoneyDrunk.Vault.Providers.File.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="FileSecretStore"/> with pre-populated secrets.
/// </summary>
public sealed class FileSecretStoreWithSecretsTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly FileSecretStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSecretStoreWithSecretsTests"/> class.
    /// </summary>
    public FileSecretStoreWithSecretsTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"test-secrets-{Guid.NewGuid()}.json");

        // Write initial secrets
        File.WriteAllText(_tempFilePath, """{"api-key":"secret-value","connection-string":"Server=localhost"}""");

        var options = Options.Create(new FileVaultOptions
        {
            SecretsFilePath = _tempFilePath,
            WatchForChanges = false,
            CreateIfNotExists = false,
        });
        _store = new FileSecretStore(options, NullLogger<FileSecretStore>.Instance);
    }

    /// <summary>
    /// Cleans up the test file.
    /// </summary>
    public void Dispose()
    {
        _store.Dispose();
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    /// <summary>
    /// Verifies that GetSecretAsync returns the secret.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSecretAsync_ReturnsSecret()
    {
        // Arrange
        var identifier = new SecretIdentifier("api-key");

        // Act
        var result = await _store.GetSecretAsync(identifier);

        // Assert
        Assert.Equal("secret-value", result.Value);
        Assert.Equal("api-key", result.Identifier.Name);
        Assert.Equal("latest", result.Version);
    }

    /// <summary>
    /// Verifies that TryGetSecretAsync returns success for existing secret.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ReturnsSuccess_ForExistingSecret()
    {
        // Arrange
        var identifier = new SecretIdentifier("connection-string");

        // Act
        var result = await _store.TryGetSecretAsync(identifier);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("Server=localhost", result.Value!.Value);
    }

    /// <summary>
    /// Verifies that ListSecretVersionsAsync returns versions for existing secret.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ListSecretVersionsAsync_ReturnsVersions_ForExistingSecret()
    {
        // Act
        var versions = await _store.ListSecretVersionsAsync("api-key");

        // Assert
        Assert.Single(versions);
        Assert.Equal("latest", versions[0].Version);
    }

    /// <summary>
    /// Verifies that FetchSecretAsync returns the secret.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FetchSecretAsync_ReturnsSecret()
    {
        // Act
        var result = await _store.FetchSecretAsync("api-key");

        // Assert
        Assert.Equal("secret-value", result.Value);
    }

    /// <summary>
    /// Verifies that TryFetchSecretAsync returns success for existing secret.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryFetchSecretAsync_ReturnsSuccess_ForExistingSecret()
    {
        // Act
        var result = await _store.TryFetchSecretAsync("api-key");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("secret-value", result.Value!.Value);
    }

    /// <summary>
    /// Verifies that IsAvailable returns true when file exists.
    /// </summary>
    [Fact]
    public void IsAvailable_ReturnsTrue_WhenFileExists()
    {
        // Assert
        Assert.True(_store.IsAvailable);
    }
}
