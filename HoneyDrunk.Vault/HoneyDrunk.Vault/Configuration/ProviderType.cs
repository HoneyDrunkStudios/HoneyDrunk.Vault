namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// The type of vault provider.
/// </summary>
public enum ProviderType
{
    /// <summary>
    /// File-based provider for local development.
    /// </summary>
    File,

    /// <summary>
    /// Azure Key Vault provider.
    /// </summary>
    AzureKeyVault,

    /// <summary>
    /// AWS Secrets Manager provider.
    /// </summary>
    AwsSecretsManager,

    /// <summary>
    /// In-memory provider for testing.
    /// </summary>
    InMemory,

    /// <summary>
    /// Configuration-based provider.
    /// </summary>
    Configuration,
}
