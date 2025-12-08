namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// Options for the file-based provider.
/// </summary>
public sealed class FileProviderOptions
{
    /// <summary>
    /// Gets or sets the file path to the secrets file.
    /// </summary>
    public string FilePath { get; set; } = "secrets.json";

    /// <summary>
    /// Gets or sets the source for the encryption key (environment variable name or file path).
    /// </summary>
    public string? EncryptionKeySource { get; set; }
}
