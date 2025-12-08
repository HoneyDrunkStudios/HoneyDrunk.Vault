using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="VaultClient"/>.
/// </summary>
public sealed class VaultClientTests
{
    private readonly Mock<ISecretStore> _mockSecretStore;
    private readonly Mock<IConfigSource> _mockConfigSource;
    private readonly VaultClient _vaultClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultClientTests"/> class.
    /// </summary>
    public VaultClientTests()
    {
        _mockSecretStore = new Mock<ISecretStore>();
        _mockConfigSource = new Mock<IConfigSource>();
        _vaultClient = new VaultClient(
            _mockSecretStore.Object,
            _mockConfigSource.Object,
            NullLogger<VaultClient>.Instance);
    }

    /// <summary>
    /// Verifies that GetSecretAsync returns secret from store.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSecretAsync_ReturnsSecretFromStore()
    {
        // Arrange
        var identifier = new SecretIdentifier("test-secret");
        var expectedValue = new SecretValue(identifier, "secret-value", "v1");
        _mockSecretStore
            .Setup(s => s.GetSecretAsync(identifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _vaultClient.GetSecretAsync(identifier);

        // Assert
        Assert.Equal(expectedValue, result);
        _mockSecretStore.Verify(s => s.GetSecretAsync(identifier, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that GetSecretAsync propagates SecretNotFoundException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSecretAsync_PropagatesSecretNotFoundException()
    {
        // Arrange
        var identifier = new SecretIdentifier("missing-secret");
        _mockSecretStore
            .Setup(s => s.GetSecretAsync(identifier, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SecretNotFoundException("missing-secret"));

        // Act & Assert
        await Assert.ThrowsAsync<SecretNotFoundException>(
            () => _vaultClient.GetSecretAsync(identifier));
    }

    /// <summary>
    /// Verifies that GetSecretAsync wraps other exceptions in VaultOperationException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetSecretAsync_WrapsOtherExceptionsInVaultOperationException()
    {
        // Arrange
        var identifier = new SecretIdentifier("error-secret");
        _mockSecretStore
            .Setup(s => s.GetSecretAsync(identifier, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<VaultOperationException>(
            () => _vaultClient.GetSecretAsync(identifier));
        Assert.Contains("error-secret", ex.Message);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that TryGetSecretAsync returns success from store.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ReturnsSuccessFromStore()
    {
        // Arrange
        var identifier = new SecretIdentifier("test-secret");
        var expectedValue = new SecretValue(identifier, "secret-value", "v1");
        _mockSecretStore
            .Setup(s => s.TryGetSecretAsync(identifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VaultResult.Success(expectedValue));

        // Act
        var result = await _vaultClient.TryGetSecretAsync(identifier);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedValue, result.Value);
    }

    /// <summary>
    /// Verifies that TryGetSecretAsync returns failure from store.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ReturnsFailureFromStore()
    {
        // Arrange
        var identifier = new SecretIdentifier("missing-secret");
        _mockSecretStore
            .Setup(s => s.TryGetSecretAsync(identifier, It.IsAny<CancellationToken>()))
            .ReturnsAsync(VaultResult.Failure<SecretValue>("Not found"));

        // Act
        var result = await _vaultClient.TryGetSecretAsync(identifier);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Not found", result.ErrorMessage);
    }

    /// <summary>
    /// Verifies that TryGetSecretAsync returns failure on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ReturnsFailureOnException()
    {
        // Arrange
        var identifier = new SecretIdentifier("error-secret");
        _mockSecretStore
            .Setup(s => s.TryGetSecretAsync(identifier, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Error"));

        // Act
        var result = await _vaultClient.TryGetSecretAsync(identifier);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Error", result.ErrorMessage);
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync returns value from source.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetConfigValueAsync_ReturnsValueFromSource()
    {
        // Arrange
        const string key = "config-key";
        const string expectedValue = "config-value";
        _mockConfigSource
            .Setup(s => s.GetConfigValueAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _vaultClient.GetConfigValueAsync(key);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync propagates ConfigurationNotFoundException.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetConfigValueAsync_PropagatesConfigurationNotFoundException()
    {
        // Arrange
        const string key = "missing-key";
        _mockConfigSource
            .Setup(s => s.GetConfigValueAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConfigurationNotFoundException(key));

        // Act & Assert
        await Assert.ThrowsAsync<ConfigurationNotFoundException>(
            () => _vaultClient.GetConfigValueAsync(key));
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync wraps other exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetConfigValueAsync_WrapsOtherExceptions()
    {
        // Arrange
        const string key = "error-key";
        _mockConfigSource
            .Setup(s => s.GetConfigValueAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<VaultOperationException>(
            () => _vaultClient.GetConfigValueAsync(key));
        Assert.Contains("error-key", ex.Message);
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync returns value from source.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_ReturnsValueFromSource()
    {
        // Arrange
        const string key = "config-key";
        const string expectedValue = "config-value";
        _mockConfigSource
            .Setup(s => s.TryGetConfigValueAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedValue);

        // Act
        var result = await _vaultClient.TryGetConfigValueAsync(key);

        // Assert
        Assert.Equal(expectedValue, result);
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync returns null on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_ReturnsNullOnException()
    {
        // Arrange
        const string key = "error-key";
        _mockConfigSource
            .Setup(s => s.TryGetConfigValueAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Error"));

        // Act
        var result = await _vaultClient.TryGetConfigValueAsync(key);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync with type returns converted value.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetConfigValueAsync_Typed_ReturnsConvertedValue()
    {
        // Arrange
        const string key = "int-key";
        _mockConfigSource
            .Setup(s => s.GetConfigValueAsync<int>(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var result = await _vaultClient.GetConfigValueAsync<int>(key);

        // Assert
        Assert.Equal(42, result);
    }

    /// <summary>
    /// Verifies that GetConfigValueAsync with type wraps exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task GetConfigValueAsync_Typed_WrapsExceptions()
    {
        // Arrange
        const string key = "error-key";
        _mockConfigSource
            .Setup(s => s.GetConfigValueAsync<int>(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Conversion error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<VaultOperationException>(
            () => _vaultClient.GetConfigValueAsync<int>(key));
        Assert.Contains("error-key", ex.Message);
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync with type returns value from source.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_Typed_ReturnsValueFromSource()
    {
        // Arrange
        const string key = "int-key";
        _mockConfigSource
            .Setup(s => s.TryGetConfigValueAsync(key, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var result = await _vaultClient.TryGetConfigValueAsync(key, 0);

        // Assert
        Assert.Equal(42, result);
    }

    /// <summary>
    /// Verifies that TryGetConfigValueAsync with type returns default on exception.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_Typed_ReturnsDefaultOnException()
    {
        // Arrange
        const string key = "error-key";
        _mockConfigSource
            .Setup(s => s.TryGetConfigValueAsync(key, 100, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Error"));

        // Act
        var result = await _vaultClient.TryGetConfigValueAsync(key, 100);

        // Assert
        Assert.Equal(100, result);
    }

    /// <summary>
    /// Verifies that ListSecretVersionsAsync returns versions from store.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ListSecretVersionsAsync_ReturnsVersionsFromStore()
    {
        // Arrange
        const string secretName = "versioned-secret";
        var expectedVersions = new List<SecretVersion>
        {
            new("v1", DateTimeOffset.UtcNow.AddDays(-1)),
            new("v2", DateTimeOffset.UtcNow),
        };
        _mockSecretStore
            .Setup(s => s.ListSecretVersionsAsync(secretName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedVersions);

        // Act
        var result = await _vaultClient.ListSecretVersionsAsync(secretName);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("v1", result[0].Version);
        Assert.Equal("v2", result[1].Version);
    }

    /// <summary>
    /// Verifies that ListSecretVersionsAsync wraps exceptions.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ListSecretVersionsAsync_WrapsExceptions()
    {
        // Arrange
        const string secretName = "error-secret";
        _mockSecretStore
            .Setup(s => s.ListSecretVersionsAsync(secretName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Error"));

        // Act & Assert
        var ex = await Assert.ThrowsAsync<VaultOperationException>(
            () => _vaultClient.ListSecretVersionsAsync(secretName));
        Assert.Contains("error-secret", ex.Message);
    }

    /// <summary>
    /// Verifies that constructor throws for null secret store.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNullSecretStore()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VaultClient(
            null!,
            _mockConfigSource.Object,
            NullLogger<VaultClient>.Instance));
    }

    /// <summary>
    /// Verifies that constructor throws for null config source.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNullConfigSource()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VaultClient(
            _mockSecretStore.Object,
            null!,
            NullLogger<VaultClient>.Instance));
    }

    /// <summary>
    /// Verifies that constructor throws for null logger.
    /// </summary>
    [Fact]
    public void Constructor_ThrowsForNullLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new VaultClient(
            _mockSecretStore.Object,
            _mockConfigSource.Object,
            null!));
    }
}
