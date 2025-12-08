using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.File.Configuration;
using HoneyDrunk.Vault.Providers.File.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="FileSecretStore"/>.
/// </summary>
public sealed class FileSecretStoreTests : IDisposable
{
    private readonly string _tempFilePath;
    private readonly FileSecretStore _store;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSecretStoreTests"/> class.
    /// </summary>
    public FileSecretStoreTests()
    {
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"test-secrets-{Guid.NewGuid()}.json");
        var options = Options.Create(new FileVaultOptions
        {
            SecretsFilePath = _tempFilePath,
            WatchForChanges = false,
            CreateIfNotExists = true,
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
    /// Verifies that ProviderName returns "file".
    /// </summary>
    [Fact]
    public void ProviderName_ReturnsFile()
    {
        // Assert
        Assert.Equal("file", _store.ProviderName);
    }

    /// <summary>
    /// Verifies that IsAvailable returns true when CreateIfNotExists is true.
    /// </summary>
    [Fact]
    public void IsAvailable_ReturnsTrue_WhenCreateIfNotExistsEnabled()
    {
        // Assert
        Assert.True(_store.IsAvailable);
    }

    /// <summary>
    /// Verifies that GetSecretAsync throws for null identifier.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSecretAsync_ThrowsForNullIdentifier()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.GetSecretAsync(null!));
    }

    /// <summary>
    /// Verifies that GetSecretAsync throws SecretNotFoundException for non-existent secret.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSecretAsync_ThrowsSecretNotFoundException_ForNonExistentSecret()
    {
        // Arrange
        var identifier = new SecretIdentifier("non-existent");

        // Act & Assert
        await Assert.ThrowsAsync<SecretNotFoundException>(() => _store.GetSecretAsync(identifier));
    }

    /// <summary>
    /// Verifies that TryGetSecretAsync returns failure for non-existent secret.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ReturnsFailure_ForNonExistentSecret()
    {
        // Arrange
        var identifier = new SecretIdentifier("non-existent");

        // Act
        var result = await _store.TryGetSecretAsync(identifier);

        // Assert
        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// Verifies that TryGetSecretAsync throws for null identifier.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ThrowsForNullIdentifier()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _store.TryGetSecretAsync(null!));
    }

    /// <summary>
    /// Verifies that ListSecretVersionsAsync throws for null/whitespace name.
    /// </summary>
    /// <param name="name">The invalid secret name to test.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ListSecretVersionsAsync_ThrowsForInvalidName(string? name)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _store.ListSecretVersionsAsync(name!));
    }

    /// <summary>
    /// Verifies that ListSecretVersionsAsync throws SecretNotFoundException for non-existent secret.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ListSecretVersionsAsync_ThrowsSecretNotFoundException_ForNonExistentSecret()
    {
        // Act & Assert
        await Assert.ThrowsAsync<SecretNotFoundException>(() => _store.ListSecretVersionsAsync("non-existent"));
    }

    /// <summary>
    /// Verifies that FetchSecretAsync calls GetSecretAsync.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FetchSecretAsync_ThrowsSecretNotFoundException_ForNonExistentSecret()
    {
        // Act & Assert
        await Assert.ThrowsAsync<SecretNotFoundException>(() => _store.FetchSecretAsync("non-existent"));
    }

    /// <summary>
    /// Verifies that TryFetchSecretAsync returns failure for non-existent secret.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryFetchSecretAsync_ReturnsFailure_ForNonExistentSecret()
    {
        // Act
        var result = await _store.TryFetchSecretAsync("non-existent");

        // Assert
        Assert.False(result.IsSuccess);
    }

    /// <summary>
    /// Verifies that CheckHealthAsync returns true when file exists or CreateIfNotExists is enabled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task CheckHealthAsync_ReturnsTrue_WhenCreateIfNotExistsEnabled()
    {
        // Act
        var healthy = await _store.CheckHealthAsync();

        // Assert
        Assert.True(healthy);
    }
}
