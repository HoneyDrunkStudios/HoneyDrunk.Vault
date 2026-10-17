namespace HoneyDrunk.Vault.Providers.File.Configuration;

/// <summary>
/// Configuration options for the file-based vault provider.
/// </summary>
public sealed class FileVaultOptions
{
    /// <summary>
    /// Gets or sets the path to the secrets file.
    /// </summary>
    public string SecretsFilePath { get; set; } = "secrets.json";

    /// <summary>
    /// Gets or sets the path to the configuration file.
    /// </summary>
    public string ConfigFilePath { get; set; } = "config.json";

    /// <summary>
    /// Gets or sets the environment variable name that contains the encryption key.
    /// If not set, secrets are stored in plain text (for development only).
    /// </summary>
    public string? EncryptionKeyEnvironmentVariable { get; set; }

    /// <summary>
    /// Gets or sets the path to a file containing the encryption key.
    /// </summary>
    public string? EncryptionKeyFilePath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to watch for file changes.
    /// </summary>
    public bool WatchForChanges { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to create the file if it doesn't exist.
    /// </summary>
    public bool CreateIfNotExists { get; set; } = true;
}
