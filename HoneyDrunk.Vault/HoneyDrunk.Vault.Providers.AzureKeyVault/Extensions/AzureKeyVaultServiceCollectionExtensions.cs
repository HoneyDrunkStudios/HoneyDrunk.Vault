using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Extensions;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Configuration;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;

/// <summary>
/// Extension methods for configuring Azure Key Vault as a vault provider.
/// </summary>
public static class AzureKeyVaultServiceCollectionExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as the vault provider with the specified options.
    /// This automatically registers the vault core services and Azure Key Vault implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action for Azure Key Vault options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultWithAzureKeyVault(
        this IServiceCollection services,
        Action<AzureKeyVaultOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AzureKeyVaultOptions();
        configure(options);

        return services.AddVaultWithAzureKeyVault(options);
    }

    /// <summary>
    /// Adds Azure Key Vault as the vault provider with the specified options.
    /// This automatically registers the vault core services and Azure Key Vault implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The Azure Key Vault options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultWithAzureKeyVault(
        this IServiceCollection services,
        AzureKeyVaultOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        if (options.VaultUri == null)
        {
            throw new ArgumentException("VaultUri must be specified in AzureKeyVaultOptions.", nameof(options));
        }

        // Register core vault services
        services.AddVaultCore();

        // Register Azure Key Vault Secret Client
        services.TryAddSingleton(_ =>
        {
            var credential = CreateTokenCredential(options);
            return new SecretClient(options.VaultUri, credential);
        });

        // Register Azure Key Vault provider implementations
        services.TryAddSingleton<ISecretStore, AzureKeyVaultSecretStore>();
        services.TryAddSingleton<IConfigSource, AzureKeyVaultConfigSource>();

        return services;
    }

    private static TokenCredential CreateTokenCredential(AzureKeyVaultOptions options)
    {
        if (options.UseManagedIdentity)
        {
            // Use managed identity (system-assigned or user-assigned if ClientId is provided)
            if (!string.IsNullOrWhiteSpace(options.ClientId))
            {
                return new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ManagedIdentityClientId = options.ClientId,
                });
            }

            return new DefaultAzureCredential();
        }

        // Use service principal with client secret
        if (string.IsNullOrWhiteSpace(options.TenantId) ||
            string.IsNullOrWhiteSpace(options.ClientId) ||
            string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new InvalidOperationException(
                "TenantId, ClientId, and ClientSecret must be specified when not using managed identity.");
        }

        return new ClientSecretCredential(options.TenantId, options.ClientId, options.ClientSecret);
    }
}
