namespace HoneyDrunk.Vault.Providers.AzureKeyVault.Configuration;

/// <summary>
/// Configuration options for the Azure Key Vault provider.
/// </summary>
public sealed class AzureKeyVaultOptions
{
    /// <summary>
    /// Gets or sets the URI of the Azure Key Vault.
    /// </summary>
    public Uri? VaultUri { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use managed identity for authentication.
    /// Default is true.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Gets or sets the tenant ID for authentication (optional, used with service principal).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the client ID for authentication (optional, used with service principal or user-assigned managed identity).
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the client secret for authentication (optional, used with service principal).
    /// </summary>
    public string? ClientSecret { get; set; }
}
