using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Providers.File.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Vault.Tests.Extensions;

/// <summary>
/// Tests for <see cref="FileVaultServiceCollectionExtensions"/>.
/// </summary>
public sealed class FileVaultServiceCollectionExtensionsTests : IDisposable
{
    private readonly string _tempDir;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileVaultServiceCollectionExtensionsTests"/> class with a scratch directory for backing files.
    /// </summary>
    public FileVaultServiceCollectionExtensionsTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"vault-file-ext-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <summary>
    /// Cleans up the scratch directory.
    /// </summary>
    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        catch
        {
            // best-effort
        }
    }

    /// <summary>
    /// Verifies that the no-arg overload registers ISecretStore + IConfigSource.
    /// </summary>
    [Fact]
    public void AddVaultWithFile_NoArgs_RegistersStores()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddVaultWithFile(options =>
        {
            options.SecretsFilePath = Path.Combine(_tempDir, "secrets.json");
            options.ConfigFilePath = Path.Combine(_tempDir, "config.json");
            options.CreateIfNotExists = true;
        });

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ISecretStore>());
        Assert.NotNull(provider.GetService<IConfigSource>());
        Assert.NotNull(provider.GetService<ISecretProvider>());
        Assert.NotNull(provider.GetService<IConfigProvider>());
    }

    /// <summary>
    /// Verifies that null services/configure arguments are rejected.
    /// </summary>
    [Fact]
    public void AddVaultWithFile_ThrowsArgumentNullException_OnNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FileVaultServiceCollectionExtensions.AddVaultWithFile(services: null!));
        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddVaultWithFile(configure: null!));
    }
}
