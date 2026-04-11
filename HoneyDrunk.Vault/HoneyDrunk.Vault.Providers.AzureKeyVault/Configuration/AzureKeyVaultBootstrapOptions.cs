namespace HoneyDrunk.Vault.Providers.AzureKeyVault.Configuration;

/// <summary>
/// Configures ADR-0005 bootstrap behavior for Key Vault provider selection.
/// </summary>
public sealed class AzureKeyVaultBootstrapOptions
{
    /// <summary>
    /// Gets or sets the fallback development secrets file path used when AZURE_KEYVAULT_URI is absent.
    /// </summary>
    public string DevelopmentSecretsFilePath { get; set; } = "secrets/dev-secrets.json";
}
