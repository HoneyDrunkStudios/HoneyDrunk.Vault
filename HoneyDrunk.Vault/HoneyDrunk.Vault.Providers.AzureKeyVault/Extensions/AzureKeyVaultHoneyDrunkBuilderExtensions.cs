using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.Extensions;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Configuration;
using HoneyDrunk.Vault.Providers.File.Extensions;
using Microsoft.Extensions.Configuration;

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
        Action<AzureKeyVaultBootstrapOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var bootstrapOptions = new AzureKeyVaultBootstrapOptions();
        configure?.Invoke(bootstrapOptions);

        builder.AddVault();

        var configuration = BootstrapConfigurationResolver.Resolve(builder.Services);
        if (BootstrapConfigurationResolver.TryGetKeyVaultUri(configuration, KeyVaultUriSetting, out var vaultUri))
        {
            builder.Services.AddVaultWithAzureKeyVault(new AzureKeyVaultOptions
            {
                VaultUri = vaultUri,
                UseManagedIdentity = true,
            });

            return builder;
        }

        if (BootstrapConfigurationResolver.IsDevelopment(configuration, AspNetCoreEnvironmentSetting, DotNetEnvironmentSetting))
        {
            builder.Services.AddVaultWithFile(options =>
            {
                options.SecretsFilePath = bootstrapOptions.DevelopmentSecretsFilePath;
            });

            return builder;
        }

        throw new InvalidOperationException(
            $"Missing required bootstrap setting '{KeyVaultUriSetting}'. " +
            "Deployable services must receive AZURE_KEYVAULT_URI via environment configuration.");
    }
}
