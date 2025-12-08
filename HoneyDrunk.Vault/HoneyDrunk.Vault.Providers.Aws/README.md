# HoneyDrunk.Vault.Providers.Aws

AWS Secrets Manager provider for HoneyDrunk.Vault. Provides secure secret management for AWS-hosted applications.

## Overview

This provider integrates with AWS Secrets Manager for enterprise-grade secret management. It supports:
- IAM role-based authentication (recommended)
- Access key authentication
- EC2 instance profiles
- Secret versioning
- Automatic secret rotation
- Cross-region replication

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.Aws
```

## Prerequisites

- AWS account
- AWS Secrets Manager service available in your region
- Either:
  - IAM role with Secrets Manager permissions (recommended)
  - AWS Access Keys with appropriate permissions
  - EC2 instance profile

## Quick Start

### Using IAM Role (Recommended)

```csharp
using HoneyDrunk.Vault.Providers.Aws.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVaultWithAwsSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.UseInstanceProfile = true;
    options.SecretPrefix = "prod/myapp/";
});

var app = builder.Build();
```

### Using Access Keys

```csharp
builder.Services.AddVaultWithAwsSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.AccessKeyId = "AKIAIOSFODNN7EXAMPLE";
    // SecretAccessKey should come from configuration, not hardcoded
    options.UseInstanceProfile = false;
});
```

### Using EC2 Instance Profile

```csharp
builder.Services.AddVaultWithAwsSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.UseInstanceProfile = true;
});
```

## Configuration Options

### AwsSecretsManagerOptions

```csharp
public class AwsSecretsManagerOptions
{
    public string? Region { get; set; }
    public string? AccessKeyId { get; set; }
    public bool UseInstanceProfile { get; set; } = true;
    public string? SecretPrefix { get; set; }
    public string? ServiceUrl { get; set; }
}
```

## Setup Instructions

### 1. Create IAM Role

```bash
# Create policy
aws iam create-policy \
  --policy-name SecretsManagerAccess \
  --policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:ListSecrets",
        "secretsmanager:ListSecretVersionIds"
      ],
      "Resource": "arn:aws:secretsmanager:*:*:secret:*"
    }]
  }'

# Create role
aws iam create-role \
  --role-name SecretsManagerRole \
  --assume-role-policy-document '{
    "Version": "2012-10-17",
    "Statement": [{
      "Effect": "Allow",
      "Principal": {"Service": "ec2.amazonaws.com"},
      "Action": "sts:AssumeRole"
    }]
  }'

# Attach policy
aws iam attach-role-policy \
  --role-name SecretsManagerRole \
  --policy-arn arn:aws:iam::ACCOUNT_ID:policy/SecretsManagerAccess
```

### 2. Create Secrets

```bash
# Create a secret
aws secretsmanager create-secret \
  --name prod/myapp/db-connection-string \
  --secret-string "Server=...;Database=...;User=...;Password=..."

# Create another secret
aws secretsmanager create-secret \
  --name prod/myapp/api-key \
  --secret-string "your-api-key-value"
```

### 3. Configure Rotation (Optional)

```bash
aws secretsmanager rotate-secret \
  --secret-id prod/myapp/db-connection-string \
  --rotation-rules AutomaticallyAfterDays=30
```

## Usage Examples

### Get Secret

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("db-connection-string"));
console.WriteLine($"Connection: {secret.Value}");
```

### Get Specific Version

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key", version: "abc123def456"));
```

### List Versions

```csharp
var versions = await secretStore.ListSecretVersionsAsync("api-key");
foreach (var version in versions)
{
    console.WriteLine($"Version: {version.VersionId}, Created: {version.CreatedAt}");
}
```

### Handle Missing Secrets

```csharp
var result = await secretStore.TryGetSecretAsync(
    new SecretIdentifier("optional-secret"));

if (result.IsSuccess)
{
    console.WriteLine($"Secret: {result.Value!.Value}");
}
else
{
    console.WriteLine($"Secret not found: {result.ErrorMessage}");
}
```

## Access Control

### IAM Policy Examples

Minimal permissions:
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": [
      "secretsmanager:GetSecretValue"
    ],
    "Resource": "arn:aws:secretsmanager:region:account:secret:prod/myapp/*"
  }]
}
```

With versioning:
```json
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": [
      "secretsmanager:GetSecretValue",
      "secretsmanager:ListSecretVersionIds"
    ],
    "Resource": "arn:aws:secretsmanager:region:account:secret:prod/myapp/*"
  }]
}
```

## Environment Configuration

Store credentials in AWS Systems Manager Parameter Store:

```csharp
// Configuration via appsettings.json
{
  "Vault": {
    "AwsSecretsManager": {
      "Region": "us-east-1",
      "UseInstanceProfile": true,
      "SecretPrefix": "prod/myapp/"
    }
  }
}
```

## Best Practices

1. **Use IAM Roles** - More secure than access keys
2. **Use Instance Profiles** - On EC2 for automatic credential rotation
3. **Enable Secret Rotation** - Regularly rotate sensitive credentials
4. **Use Secret Prefix** - Organize secrets by application/environment
5. **Enable Automatic Rotation** - For database passwords and API keys
6. **Principle of Least Privilege** - Grant minimal required permissions
7. **Use Resource-based Policies** - Control cross-account access
8. **Enable CloudTrail Logging** - Monitor access to secrets
9. **Use KMS Encryption** - For at-rest encryption
10. **Avoid Hardcoding Credentials** - Use IAM roles or instance profiles

## Performance Considerations

- **Caching** - Use Vault's caching to reduce API calls
- **Rate Limiting** - Secrets Manager has rate limits (see AWS docs)
- **Network** - Use VPC endpoints for better performance and security
- **Batch Operations** - Group secret retrievals when possible
- **Prefix Strategy** - Use prefixes to organize and filter secrets

## Troubleshooting

### Authentication Errors

```
AmazonSecretsManagerException: The security token included in the request is invalid
```

- Verify IAM credentials are correct
- Check IAM role permissions
- Ensure instance profile is attached to EC2 instance
- Verify region is correct

### Permission Errors

```
AmazonSecretsManagerException: User is not authorized to perform: secretsmanager:GetSecretValue
```

- Verify IAM policy is attached
- Check resource ARN in policy
- Ensure secret exists in specified region
- Review secret-based policies

### Rate Limiting

- Implement exponential backoff (automatic in SDK)
- Use caching to reduce requests
- Consider request batching
- Monitor CloudWatch metrics

## Related Providers

- [HoneyDrunk.Vault.Providers.AzureKeyVault](../HoneyDrunk.Vault.Providers.AzureKeyVault) - For Azure Key Vault
- [HoneyDrunk.Vault.Providers.File](../HoneyDrunk.Vault.Providers.File) - For local development
- [HoneyDrunk.Vault.Providers.InMemory](../HoneyDrunk.Vault.Providers.InMemory) - For testing

## References

- [AWS Secrets Manager Documentation](https://docs.aws.amazon.com/secretsmanager/)
- [AWS SDK for .NET](https://github.com/aws/aws-sdk-net)
- [IAM Roles for Amazon EC2](https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/iam-roles-for-amazon-ec2.html)

## License

MIT License - see LICENSE file for details.
