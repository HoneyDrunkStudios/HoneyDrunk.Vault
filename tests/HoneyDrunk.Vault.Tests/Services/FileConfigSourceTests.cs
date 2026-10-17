using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Providers.File.Configuration;
using HoneyDrunk.Vault.Providers.File.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Tests.Services;

/// <summary>
/// Tests for <see cref="FileConfigSource"/>.
/// </summary>
public sealed class FileConfigSourceTests : IDisposable
{
    private readonly string _tempFilePath;
    private IConfigSource? _source;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileConfigSourceTests"/> class.
    /// </summary>
    public FileConfigSourceTests()
    {
        _tempFilePath = Path.GetTempFileName();
        File.Delete(_tempFilePath);
    }

    /// <summary>
    /// Verifies missing config files are created when configured.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task Constructor_CreatesEmptyFile_WhenCreateIfNotExistsEnabled()
    {
        // Act
        _source = CreateSource(createIfNotExists: true);

        // Assert
        Assert.True(File.Exists(_tempFilePath));
        Assert.Equal("{}", await File.ReadAllTextAsync(_tempFilePath));
    }

    /// <summary>
    /// Verifies existing string values are loaded from the JSON config file.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetConfigValueAsync_ReturnsValue_WhenKeyExists()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "{\"Feature:Enabled\":\"true\",\"Timeout\":\"30\"}");
        _source = CreateSource();

        // Act
        var value = await _source.GetConfigValueAsync("Feature:Enabled");

        // Assert
        Assert.Equal("true", value);
    }

    /// <summary>
    /// Verifies missing string values throw the expected not-found exception.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetConfigValueAsync_ThrowsConfigurationNotFoundException_WhenKeyIsMissing()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "{}");
        _source = CreateSource();

        // Act & Assert
        await Assert.ThrowsAsync<ConfigurationNotFoundException>(() => _source.GetConfigValueAsync("missing"));
    }

    /// <summary>
    /// Verifies missing values return null through the try-get path.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_ReturnsNull_WhenKeyIsMissing()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "{}");
        _source = CreateSource();

        // Act
        var value = await _source.TryGetConfigValueAsync("missing");

        // Assert
        Assert.Null(value);
    }

    /// <summary>
    /// Verifies typed values are converted from file strings.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task GetConfigValueAsync_Typed_ReturnsConvertedValue()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "{\"Timeout\":\"30\"}");
        _source = CreateSource();

        // Act
        var value = await _source.GetConfigValueAsync<int>("Timeout");

        // Assert
        Assert.Equal(30, value);
    }

    /// <summary>
    /// Verifies typed try-get returns the default value when conversion fails.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task TryGetConfigValueAsync_Typed_ReturnsDefault_WhenConversionFails()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "{\"Timeout\":\"not-an-int\"}");
        _source = CreateSource();

        // Act
        var value = await _source.TryGetConfigValueAsync("Timeout", 10);

        // Assert
        Assert.Equal(10, value);
    }

    /// <summary>
    /// Verifies <see cref="IConfigProvider"/> members delegate to the config-source behavior.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task IConfigProviderMembers_DelegateToConfigSourceBehavior()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "{\"Name\":\"vault\",\"Retries\":\"4\"}");
        _source = CreateSource();

        // Act & Assert
        var provider = (IConfigProvider)_source;
        Assert.Equal("vault", await provider.GetValueAsync("Name"));
        Assert.Equal("vault", await provider.TryGetValueAsync("Name"));
        Assert.Equal(4, await provider.GetValueAsync<int>("Retries"));
        Assert.Equal(4, await provider.GetValueAsync("Retries", 1));
    }

    /// <summary>
    /// Verifies invalid JSON is swallowed and leaves the source empty.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task Constructor_LeavesSourceEmpty_WhenJsonIsInvalid()
    {
        // Arrange
        await File.WriteAllTextAsync(_tempFilePath, "{ invalid json");

        // Act
        _source = CreateSource();

        // Assert
        await Assert.ThrowsAsync<ConfigurationNotFoundException>(() => _source.GetConfigValueAsync("any"));
    }

    /// <summary>
    /// Disposes of test resources.
    /// </summary>
    public void Dispose()
    {
        (_source as IDisposable)?.Dispose();
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }

    private FileConfigSource CreateSource(bool createIfNotExists = false)
    {
        var options = Options.Create(new FileVaultOptions
        {
            ConfigFilePath = _tempFilePath,
            CreateIfNotExists = createIfNotExists,
            WatchForChanges = false,
        });

        return new FileConfigSource(options, NullLogger<FileConfigSource>.Instance);
    }
}
