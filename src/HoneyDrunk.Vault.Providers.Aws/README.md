# HoneyDrunk.Vault.Providers.Aws

AWS Secrets Manager provider for HoneyDrunk.Vault. **Recommended for AWS hosted applications.**

## Overview

This provider integrates HoneyDrunk.Vault with AWS Secrets Manager. It lets you use the Vault abstractions (`ISecretStore`, `IConfigProvider`, `SecretIdentifier`, `SecretValue`) while actually storing secrets in AWS.

**Features:**
- IAM role based authentication (recommended for production)
- EC2 instance profile and ECS task role support
- Optional access key authentication for local development
- Secret prefixing by environment or application
- Versioned secrets and rollback
- Works with Vault caching, resilience and telemetry

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.Aws
```

## Prerequisites

- AWS account
- AWS Secrets Manager available in your region
- One of:
  - IAM role with Secrets Manager permissions (recommended)
  - EC2 instance profile or ECS task role
  - AWS access keys with least privilege for local development

## Quick Start

### Using IAM Role or Instance Profile (Recommended)

```csharp
using HoneyDrunk.Vault.Providers.Aws.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVaultWithAwsSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.SecretPrefix = "prod/myapp/";
    // Uses default AWS credential chain (instance profile, env vars, etc.)
});

var app = builder.Build();
```

**With this configuration:**
- `new SecretIdentifier("db-connection-string")` will resolve to the AWS secret named `prod/myapp/db-connection-string`
- Credentials are resolved from the default AWS credential chain for the instance or task

### Using Named Profile (Local Development)

```csharp
builder.Services.AddVaultWithAwsSecretsManager(options =>
{
    options.Region = builder.Configuration["AWS:Region"];
    options.ProfileName = "my-dev-profile"; // Uses named profile from ~/.aws/credentials
    options.SecretPrefix = "dev/myapp/";
});
```

**Avoid hardcoding access keys in code.** Use user secrets, environment variables or local config instead.

## Configuration Options

```csharp
public sealed class AwsSecretsManagerOptions
{
    /// <summary>
    /// AWS region where secrets are stored, for example "us-east-1".
    /// If not specified, uses the default region from AWS configuration.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// The profile name for AWS credentials.
    /// If not specified, uses the default credential chain.
    /// </summary>
    public string? ProfileName { get; set; }

    /// <summary>
    /// Optional custom service URL for Secrets Manager.
    /// Useful for local development with LocalStack or VPC endpoints.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Optional prefix for secret names, for example "prod/myapp/".
    /// </summary>
    public string? SecretPrefix { get; set; }

    /// <summary>
    /// Whether to use the secret version ID as the version.
    /// Default: true
    /// </summary>
    public bool UseVersionId { get; set; } = true;

    /// <summary>
    /// The version stage to use when fetching secrets.
    /// Default: "AWSCURRENT"
    /// </summary>
    public string VersionStage { get; set; } = "AWSCURRENT";
}
```

**If `SecretPrefix` is set**, a `SecretIdentifier` of `"api-key"` will be resolved as `"prod/myapp/api-key"` in AWS.

## Setup Instructions

### 1. IAM Role and Policy

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
        "secretsmanager:ListSecretVersionIds"
      ],
      "Resource": "arn:aws:secretsmanager:*:*:secret:prod/myapp/*"
    }]
  }'

# Create role (example for EC2)
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

**Attach this role to your EC2 instance or ECS task.**

### 2. Create Secrets

```bash
aws secretsmanager create-secret \
  --name prod/myapp/db-connection-string \
  --secret-string "Server=...;Database=...;User=...;Password=..."

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

**These examples use the Vault abstractions. The AWS provider is hidden behind `ISecretStore`.**

### Get a Secret

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("db-connection-string"),
    ct);

Console.WriteLine($"Connection string length: {secret.Value.Length}");
```

**If `SecretPrefix = "prod/myapp/"`, this reads `prod/myapp/db-connection-string` from AWS.**

### Get a Specific Version

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key", "abc123"),
    ct);
```

### List Versions

```csharp
var versions = await secretStore.ListSecretVersionsAsync("api-key", ct);

foreach (var version in versions)
{
    Console.WriteLine($"Version: {version.Version}, Created: {version.CreatedOn}");
}
```

### Handle Missing Secrets

```csharp
var result = await secretStore.TryGetSecretAsync(
    new SecretIdentifier("optional-secret"),
    ct);

if (result.IsSuccess)
{
    Console.WriteLine("Secret present");
}
else
{
    Console.WriteLine($"Secret not available: {result.ErrorMessage}");
}
```

## Access Control

Minimal IAM policy for a single application prefix:

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

**Apply the principle of least privilege by scoping to the exact prefixes your node uses.**

## Configuration Example

You can drive `AwsSecretsManagerOptions` from configuration:

```json
{
  "Vault": {
    "AwsSecretsManager": {
      "Region": "us-east-1",
      "SecretPrefix": "prod/myapp/",
      "VersionStage": "AWSCURRENT"
    }
  }
}
```

Then bind inside `AddVaultWithAwsSecretsManager`.

## Best Practices

1. **Prefer IAM roles and default credential chain over static access keys**
2. **Use `SecretPrefix` to separate environments and applications**
3. **Enable Vault caching to reduce Secrets Manager calls**
4. **Configure rotation for high value secrets such as database passwords**
5. **Use VPC endpoints for Secrets Manager in private networks**
6. **Monitor access with CloudTrail and CloudWatch metrics**
7. **Keep IAM policies scoped to the smallest necessary set of secrets**

## Troubleshooting

### Authentication Errors

```
AmazonSecretsManagerException: The security token included in the request is invalid
```

- Check that the instance profile or role is attached to the instance or task
- Verify that the region matches where the secrets are stored
- Confirm that your local credentials are valid when using access keys

### Permission Errors

```
AmazonSecretsManagerException: User is not authorized to perform: secretsmanager:GetSecretValue
```

- Check IAM policies on the role or user
- Verify the ARN patterns match your secret names and region

### Throttling and Rate Limits

- Enable Vault caching so you hit Secrets Manager less often
- Use the SDK built in exponential backoff
- Avoid tight polling loops that read the same secret repeatedly

## Related Providers

- [HoneyDrunk.Vault.Providers.AzureKeyVault](../HoneyDrunk.Vault.Providers.AzureKeyVault) for Azure Key Vault
- [HoneyDrunk.Vault.Providers.File](../HoneyDrunk.Vault.Providers.File) for local development
- [HoneyDrunk.Vault.Providers.InMemory](../HoneyDrunk.Vault.Providers.InMemory) for testing

## License

MIT License. See the LICENSE file for details.
