namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// Options for the Azure Key Vault provider.
/// </summary>
public sealed class AzureKeyVaultProviderOptions
{
    /// <summary>
    /// Gets or sets the Azure Key Vault URI.
    /// </summary>
    public Uri? VaultUri { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use managed identity.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Gets or sets the client ID for authentication.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the tenant ID for authentication.
    /// </summary>
    public string? TenantId { get; set; }
}
