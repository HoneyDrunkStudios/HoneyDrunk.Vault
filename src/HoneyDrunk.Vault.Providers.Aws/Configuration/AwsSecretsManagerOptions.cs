namespace HoneyDrunk.Vault.Providers.Aws.Configuration;

/// <summary>
/// Configuration options for AWS Secrets Manager provider.
/// </summary>
public sealed class AwsSecretsManagerOptions
{
    /// <summary>
    /// Gets or sets the AWS region for Secrets Manager.
    /// If not specified, uses the default region from AWS configuration.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the profile name for AWS credentials.
    /// If not specified, uses the default profile chain.
    /// </summary>
    public string? ProfileName { get; set; }

    /// <summary>
    /// Gets or sets the service URL for AWS Secrets Manager.
    /// Useful for local development with LocalStack or similar tools.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the prefix to add to all secret names.
    /// </summary>
    public string? SecretPrefix { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use the secret version ID as the version.
    /// </summary>
    public bool UseVersionId { get; set; } = true;

    /// <summary>
    /// Gets or sets the version stage to use when fetching secrets.
    /// Defaults to "AWSCURRENT".
    /// </summary>
    public string VersionStage { get; set; } = "AWSCURRENT";
}
