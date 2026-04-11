using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;
using HoneyDrunk.Vault.Extensions;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Services;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Configuration;
using HoneyDrunk.Vault.Providers.File.Configuration;
using HoneyDrunk.Vault.Providers.File.Services;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;

/// <summary>
/// Bootstrap extension methods for Azure Key Vault provider integration with HoneyDrunk.Kernel.
/// </summary>
public static class AzureKeyVaultHoneyDrunkBuilderExtensions
{
    private const string KeyVaultUriSetting = "AZURE_KEYVAULT_URI";
    private const string AspNetCoreEnvironmentSetting = "ASPNETCORE_ENVIRONMENT";
    private const string DotNetEnvironmentSetting = "DOTNET_ENVIRONMENT";

    /// <summary>
    /// Adds Vault bootstrap wiring using environment-variable discovery.
    /// Reads <c>AZURE_KEYVAULT_URI</c> from <see cref="IConfiguration"/> and wires Azure Key Vault with
    /// <see cref="Azure.Identity.DefaultAzureCredential"/> when present.
    /// If the setting is missing in Development, falls back to file-based secrets. If missing outside
    /// Development, throws a descriptive bootstrap exception.
    /// </summary>
    /// <param name="builder">The HoneyDrunk builder.</param>
    /// <param name="configure">Optional bootstrap customization.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddVaultWithAzureKeyVaultBootstrap(
        this IHoneyDrunkBuilder builder,
        Action<AzureKeyVaultBootstrapOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var bootstrapOptions = new AzureKeyVaultBootstrapOptions();
        configure?.Invoke(bootstrapOptions);

        builder.AddVault();

        var configuration = BootstrapConfigurationResolver.Resolve(builder.Services);
        if (BootstrapConfigurationResolver.TryGetKeyVaultUri(configuration, KeyVaultUriSetting, out var vaultUri))
        {
            RegisterAzureKeyVaultProviders(builder.Services, vaultUri);

            return builder;
        }

        if (BootstrapConfigurationResolver.IsDevelopment(configuration, AspNetCoreEnvironmentSetting, DotNetEnvironmentSetting))
        {
            RegisterFileFallbackProviders(builder.Services, bootstrapOptions);

            return builder;
        }

        throw new InvalidOperationException(
            $"Missing required bootstrap setting '{KeyVaultUriSetting}'. " +
            "Deployable services must receive AZURE_KEYVAULT_URI via environment configuration.");
    }

    private static void RegisterAzureKeyVaultProviders(IServiceCollection services, Uri vaultUri)
    {
        services.AddSingleton(sp => new SecretClient(vaultUri, new DefaultAzureCredential()));
        services.AddSingleton<AzureKeyVaultSecretStore>();
        services.AddSingleton<AzureKeyVaultConfigSource>();

        services.AddSecretProvider(
            sp => sp.GetRequiredService<AzureKeyVaultSecretStore>(),
            new ProviderRegistration
            {
                Name = "azure-key-vault",
                ProviderType = ProviderType.AzureKeyVault,
                Priority = 0,
                IsEnabled = true,
                IsRequired = true,
            });

        services.AddConfigSourceProvider(
            sp => new DelegatingConfigSourceProvider(
                "azure-key-vault",
                sp.GetRequiredService<AzureKeyVaultConfigSource>(),
                () => sp.GetRequiredService<AzureKeyVaultSecretStore>().CheckHealthAsync()),
            new ProviderRegistration
            {
                Name = "azure-key-vault",
                ProviderType = ProviderType.AzureKeyVault,
                Priority = 0,
                IsEnabled = true,
                IsRequired = true,
            });
    }

    private static void RegisterFileFallbackProviders(
        IServiceCollection services,
        AzureKeyVaultBootstrapOptions bootstrapOptions)
    {
        services.AddSingleton<IOptions<FileVaultOptions>>(
            Options.Create(new FileVaultOptions
            {
                SecretsFilePath = bootstrapOptions.DevelopmentSecretsFilePath,
            }));

        services.AddSingleton<FileSecretStore>();
        services.AddSingleton<FileConfigSource>();

        services.AddSecretProvider(
            sp => sp.GetRequiredService<FileSecretStore>(),
            new ProviderRegistration
            {
                Name = "file",
                ProviderType = ProviderType.File,
                Priority = 100,
                IsEnabled = true,
                IsRequired = false,
            });

        services.AddConfigSourceProvider(
            sp => new DelegatingConfigSourceProvider(
                "file",
                sp.GetRequiredService<FileConfigSource>(),
                () => sp.GetRequiredService<FileSecretStore>().CheckHealthAsync()),
            new ProviderRegistration
            {
                Name = "file",
                ProviderType = ProviderType.File,
                Priority = 100,
                IsEnabled = true,
                IsRequired = false,
            });
    }

    private sealed class DelegatingConfigSourceProvider(
        string providerName,
        IConfigSource inner,
        Func<Task<bool>> healthCheck) : IConfigSourceProvider
    {
        private readonly IConfigSource _inner = inner;
        private readonly Func<Task<bool>> _healthCheck = healthCheck;

        public string ProviderName { get; } = providerName;

        public bool IsAvailable => true;

        public Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default) => _healthCheck();

        public Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken = default) =>
            _inner.GetConfigValueAsync(key, cancellationToken);

        public Task<string?> TryGetConfigValueAsync(string key, CancellationToken cancellationToken = default) =>
            _inner.TryGetConfigValueAsync(key, cancellationToken);

        public Task<T> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default) =>
            _inner.GetConfigValueAsync<T>(key, cancellationToken);

        public Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default) =>
            _inner.TryGetConfigValueAsync(key, defaultValue, cancellationToken);
    }
}
