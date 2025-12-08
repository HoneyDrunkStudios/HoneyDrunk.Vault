namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// Options for the AWS Secrets Manager provider.
/// </summary>
public sealed class AwsSecretsManagerProviderOptions
{
    /// <summary>
    /// Gets or sets the AWS region.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets the access key ID (optional if using instance profile).
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to use EC2 instance profile for authentication.
    /// </summary>
    public bool UseInstanceProfile { get; set; } = true;
}
