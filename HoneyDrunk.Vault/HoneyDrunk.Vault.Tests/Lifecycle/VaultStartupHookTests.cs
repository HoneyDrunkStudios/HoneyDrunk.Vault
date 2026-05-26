using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Lifecycle;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Tests.Lifecycle;

/// <summary>
/// Tests for <see cref="VaultStartupHook"/> configuration validation and cache warmup.
/// </summary>
public sealed class VaultStartupHookTests
{
    /// <summary>
    /// Verifies startup metadata remains stable for lifecycle ordering.
    /// </summary>
    [Fact]
    public void Priority_ReturnsExpectedValue()
    {
        // Arrange
        var hook = CreateHook(new VaultOptions(), new TestSecretStore());

        // Assert
        Assert.Equal(100, hook.Priority);
    }

    /// <summary>
    /// Verifies startup succeeds when no providers are configured.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteAsync_Completes_WhenNoProvidersConfigured()
    {
        // Arrange
        var store = new TestSecretStore();
        var hook = CreateHook(new VaultOptions(), store);

        // Act
        await hook.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Empty(store.RequestedSecrets);
    }

    /// <summary>
    /// Verifies Azure Key Vault provider validation requires a VaultUri setting.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteAsync_Throws_WhenAzureKeyVaultProviderHasNoVaultUri()
    {
        // Arrange
        var options = new VaultOptions();
        options.AddProvider("akv", provider =>
        {
            provider.ProviderType = ProviderType.AzureKeyVault;
            provider.IsEnabled = true;
        });
        var hook = CreateHook(options, new TestSecretStore());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => hook.ExecuteAsync(CancellationToken.None));
        Assert.Contains("requires VaultUri", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies disabled providers are skipped by startup validation.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteAsync_SkipsValidation_ForDisabledProviders()
    {
        // Arrange
        var options = new VaultOptions();
        options.AddProvider("akv", provider =>
        {
            provider.ProviderType = ProviderType.AzureKeyVault;
            provider.IsEnabled = false;
        });
        var hook = CreateHook(options, new TestSecretStore());

        // Act & Assert
        var ex = await Record.ExceptionAsync(() => hook.ExecuteAsync(CancellationToken.None));
        Assert.Null(ex);
    }

    /// <summary>
    /// Verifies provider-specific optional settings do not block startup.
    /// </summary>
    /// <param name="providerType">The provider type to validate.</param>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Theory]
    [InlineData(ProviderType.AwsSecretsManager)]
    [InlineData(ProviderType.File)]
    [InlineData(ProviderType.InMemory)]
    [InlineData(ProviderType.Configuration)]
    public async Task ExecuteAsync_Completes_ForProvidersWithoutRequiredSettings(ProviderType providerType)
    {
        // Arrange
        var options = new VaultOptions();
        options.AddProvider("provider", provider =>
        {
            provider.ProviderType = providerType;
            provider.IsEnabled = true;
        });
        var hook = CreateHook(options, new TestSecretStore());

        // Act & Assert
        var ex = await Record.ExceptionAsync(() => hook.ExecuteAsync(CancellationToken.None));
        Assert.Null(ex);
    }

    /// <summary>
    /// Verifies startup warms each configured secret key.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task ExecuteAsync_WarmsConfiguredKeys()
    {
        // Arrange
        var options = new VaultOptions();
        options.WarmupKeys.AddRange(["api-key", "missing", "throwing"]);
        var store = new TestSecretStore(
            successNames: ["api-key"],
            exceptionNames: ["throwing"]);
        var hook = CreateHook(options, store);

        // Act
        await hook.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Equal(["api-key", "missing", "throwing"], store.RequestedSecrets);
    }

    private static VaultStartupHook CreateHook(VaultOptions options, ISecretStore store)
    {
        return new VaultStartupHook(
            store,
            Options.Create(options),
            NullLogger<VaultStartupHook>.Instance);
    }

    private sealed class TestSecretStore(IEnumerable<string>? successNames = null, IEnumerable<string>? exceptionNames = null) : ISecretStore
    {
        private readonly HashSet<string> _successNames = successNames?.ToHashSet(StringComparer.Ordinal) ?? [];
        private readonly HashSet<string> _exceptionNames = exceptionNames?.ToHashSet(StringComparer.Ordinal) ?? [];

        public List<string> RequestedSecrets { get; } = [];

        public Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        {
            RequestedSecrets.Add(identifier.Name);

            if (_exceptionNames.Contains(identifier.Name))
            {
                throw new InvalidOperationException("warmup failed");
            }

            if (!_successNames.Contains(identifier.Name))
            {
                return Task.FromResult(VaultResult.Failure<SecretValue>("not found"));
            }

            return Task.FromResult(VaultResult.Success(new SecretValue(identifier, "value", identifier.Version)));
        }

        public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
