using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.InMemory.Services;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="InMemorySecretStore"/>.
/// </summary>
public sealed class InMemorySecretStoreTests
{
    private readonly InMemorySecretStore _store;
    private readonly ConcurrentDictionary<string, string> _secrets;

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemorySecretStoreTests"/> class.
    /// </summary>
    public InMemorySecretStoreTests()
    {
        _secrets = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _store = new InMemorySecretStore(_secrets, NullLogger<InMemorySecretStore>.Instance);
    }

    /// <summary>
    /// Verifies that GetSecretAsync returns secret when it exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetSecretAsync_ReturnsSecret_WhenExists()
    {
        // Arrange
        const string secretName = "test-secret";
        const string secretValue = "secret-value";
        _secrets[secretName] = secretValue;

        // Act
        var result = await _store.GetSecretAsync(new SecretIdentifier(secretName));

        // Assert
        Assert.Equal(secretName, result.Identifier.Name);
        Assert.Equal(secretValue, result.Value);
        Assert.Equal("latest", result.Version);
    }

    /// <summary>
    /// Verifies that GetSecretAsync throws SecretNotFoundException when secret doesn't exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task GetSecretAsync_ThrowsSecretNotFoundException_WhenNotExists()
    {
        // Arrange
        var identifier = new SecretIdentifier("non-existent");

        // Act & Assert
        await Assert.ThrowsAsync<SecretNotFoundException>(
            () => _store.GetSecretAsync(identifier));
    }

    /// <summary>
    /// Verifies that TryGetSecretAsync returns success when secret exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ReturnsSuccess_WhenSecretExists()
    {
        // Arrange
        const string secretName = "existing-secret";
        const string secretValue = "value";
        _secrets[secretName] = secretValue;

        // Act
        var result = await _store.TryGetSecretAsync(new SecretIdentifier(secretName));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(secretValue, result.Value!.Value);
    }

    /// <summary>
    /// Verifies that TryGetSecretAsync returns failure when secret doesn't exist.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task TryGetSecretAsync_ReturnsFailure_WhenSecretNotExists()
    {
        // Act
        var result = await _store.TryGetSecretAsync(new SecretIdentifier("missing"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Verifies that SetSecret adds a new secret.
    /// </summary>
    [Fact]
    public void SetSecret_AddsNewSecret()
    {
        // Arrange
        const string secretName = "new-secret";
        const string secretValue = "new-value";

        // Act
        _store.SetSecret(secretName, secretValue);

        // Assert
        Assert.True(_secrets.ContainsKey(secretName));
        Assert.Equal(secretValue, _secrets[secretName]);
    }

    /// <summary>
    /// Verifies that SetSecret updates an existing secret.
    /// </summary>
    [Fact]
    public void SetSecret_UpdatesExistingSecret()
    {
        // Arrange
        const string secretName = "updateable";
        _secrets[secretName] = "old-value";

        // Act
        _store.SetSecret(secretName, "new-value");

        // Assert
        Assert.Equal("new-value", _secrets[secretName]);
    }

    /// <summary>
    /// Verifies that RemoveSecret returns true when secret existed.
    /// </summary>
    [Fact]
    public void RemoveSecret_ReturnsTrue_WhenSecretExisted()
    {
        // Arrange
        _secrets["removable"] = "value";

        // Act
        var result = _store.RemoveSecret("removable");

        // Assert
        Assert.True(result);
        Assert.False(_secrets.ContainsKey("removable"));
    }

    /// <summary>
    /// Verifies that RemoveSecret returns false when secret doesn't exist.
    /// </summary>
    [Fact]
    public void RemoveSecret_ReturnsFalse_WhenSecretNotExist()
    {
        // Act
        var result = _store.RemoveSecret("non-existent");

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Verifies that ListSecretVersionsAsync returns latest version.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task ListSecretVersionsAsync_ReturnsLatest()
    {
        // Arrange
        _secrets["versioned"] = "value";

        // Act
        var versions = await _store.ListSecretVersionsAsync("versioned");

        // Assert
        Assert.Single(versions);
        Assert.Equal("latest", versions[0].Version);
    }

    /// <summary>
    /// Verifies that Clear removes all secrets.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllSecrets()
    {
        // Arrange
        _secrets["secret1"] = "value1";
        _secrets["secret2"] = "value2";

        // Act
        _store.Clear();

        // Assert
        Assert.Empty(_secrets);
    }

    /// <summary>
    /// Verifies that ProviderName returns in-memory.
    /// </summary>
    [Fact]
    public void ProviderName_ReturnsInMemory()
    {
        // Assert
        Assert.Equal("in-memory", _store.ProviderName);
    }

    /// <summary>
    /// Verifies that IsAvailable returns true.
    /// </summary>
    [Fact]
    public void IsAvailable_ReturnsTrue()
    {
        // Assert
        Assert.True(_store.IsAvailable);
    }

    /// <summary>
    /// Verifies that CheckHealthAsync returns true.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task CheckHealthAsync_ReturnsTrue()
    {
        // Act
        var isHealthy = await _store.CheckHealthAsync();

        // Assert
        Assert.True(isHealthy);
    }

    /// <summary>
    /// Verifies that FetchSecretAsync delegates to GetSecretAsync.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    [Fact]
    public async Task FetchSecretAsync_DelegatesTo_GetSecretAsync()
    {
        // Arrange
        const string secretName = "fetch-test";
        const string secretValue = "fetch-value";
        _secrets[secretName] = secretValue;

        // Act
        var result = await _store.FetchSecretAsync(secretName);

        // Assert
        Assert.Equal(secretValue, result.Value);
    }
}
