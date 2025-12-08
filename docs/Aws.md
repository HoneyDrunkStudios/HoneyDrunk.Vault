# ☁️ AWS Secrets Manager Provider

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Authentication](#authentication)
- [AwsSecretsManagerSecretStore.cs](#awssecretsmanagersecretstorecs)
- [Best Practices](#best-practices)

---

## Overview

AWS Secrets Manager provider for secure secret management in AWS-hosted applications. Integrates with AWS Secrets Manager for centralized secret storage with IAM-based access control.

**Location:** `HoneyDrunk.Vault.Providers.Aws/`

**Use Cases:**
- Production AWS-hosted applications
- EC2, ECS, EKS, Lambda deployments
- Cross-region secret replication
- Automatic secret rotation
- IAM-based access control

---

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.Aws
```

---

## Prerequisites

1. **AWS Account** with Secrets Manager access
2. **Secrets** created in AWS Secrets Manager
3. **Authentication** configured:
   - IAM Role (recommended for AWS-hosted apps)
   - EC2 Instance Profile
   - Access Keys (for local development)

---

## Configuration

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
    options.AccessKeyId = builder.Configuration["AWS:AccessKeyId"];
    // SecretAccessKey should come from environment or secure config
    options.UseInstanceProfile = false;
});
```

### AwsSecretsManagerOptions

```csharp
public sealed class AwsSecretsManagerOptions
{
    /// <summary>
    /// The AWS region where secrets are stored.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// The AWS access key ID (for access key auth).
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// Whether to use EC2 instance profile for authentication.
    /// Default: true
    /// </summary>
    public bool UseInstanceProfile { get; set; } = true;

    /// <summary>
    /// Optional prefix for secret names (e.g., "prod/myapp/").
    /// </summary>
    public string? SecretPrefix { get; set; }
}
```

---

## Authentication

### IAM Role (Recommended)

For EC2, ECS, EKS, or Lambda:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:DescribeSecret",
        "secretsmanager:ListSecretVersionIds"
      ],
      "Resource": "arn:aws:secretsmanager:us-east-1:123456789012:secret:prod/myapp/*"
    }
  ]
}
```

### EC2 Instance Profile

```bash
# Create IAM role for EC2
aws iam create-role \
  --role-name EC2SecretsRole \
  --assume-role-policy-document file://trust-policy.json

# Attach secrets policy
aws iam attach-role-policy \
  --role-name EC2SecretsRole \
  --policy-arn arn:aws:iam::123456789012:policy/SecretsManagerReadOnly

# Create instance profile
aws iam create-instance-profile \
  --instance-profile-name EC2SecretsProfile

aws iam add-role-to-instance-profile \
  --instance-profile-name EC2SecretsProfile \
  --role-name EC2SecretsRole
```

### EKS Service Account

```yaml
# IAM role for service account (IRSA)
apiVersion: v1
kind: ServiceAccount
metadata:
  name: my-app
  annotations:
    eks.amazonaws.com/role-arn: arn:aws:iam::123456789012:role/MyAppSecretsRole
```

---

## AwsSecretsManagerSecretStore.cs

AWS Secrets Manager implementation of `ISecretStore` and `ISecretProvider`.

```csharp
public sealed class AwsSecretsManagerSecretStore : ISecretStore, ISecretProvider
{
    public string ProviderName => "aws-secrets-manager";
    public bool IsAvailable => true;

    public async Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public async Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
        string secretName,
        CancellationToken cancellationToken = default);

    public async Task<bool> CheckHealthAsync(
        CancellationToken cancellationToken = default);
}
```

### Implementation Details

- Uses `AWSSDK.SecretsManager`
- Supports versioned secret retrieval (AWSCURRENT, AWSPREVIOUS)
- Supports secret prefix for organization
- Maps AWS exceptions to vault exceptions

### Usage Example

```csharp
public class DatabaseService(ISecretStore secretStore)
{
    public async Task<string> GetConnectionStringAsync(CancellationToken ct)
    {
        // Secret name: "prod/myapp/database-connection"
        var secret = await secretStore.GetSecretAsync(
            new SecretIdentifier("database-connection"),
            ct);

        return secret.Value;
    }
}
```

### Versioned Secrets

```csharp
// Get current version
var current = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key"),
    ct);

// Get previous version
var previous = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key", "AWSPREVIOUS"),
    ct);

// List all versions
var versions = await secretStore.ListSecretVersionsAsync("api-key", ct);
```

### JSON Secrets

AWS Secrets Manager supports JSON secrets:

```json
{
  "username": "admin",
  "password": "secret123",
  "host": "db.example.com",
  "port": "5432"
}
```

```csharp
// Parse JSON secret
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("database-credentials"),
    ct);

var credentials = JsonSerializer.Deserialize<DatabaseCredentials>(secret.Value);
```

---

## Best Practices

### 1. Use IAM Roles

```csharp
// ✅ Good - IAM Role/Instance Profile
options.UseInstanceProfile = true;

// ❌ Avoid - Access keys in code
options.AccessKeyId = "AKIAIOSFODNN7EXAMPLE";
```

### 2. Minimal Permissions

```json
{
  "Effect": "Allow",
  "Action": ["secretsmanager:GetSecretValue"],
  "Resource": "arn:aws:secretsmanager:*:*:secret:prod/myapp/*"
}
```

### 3. Use Secret Prefixes

```csharp
// Organize secrets by environment/application
options.SecretPrefix = "prod/myapp/";

// Secret names become: prod/myapp/database-connection
```

### 4. Enable Rotation

```bash
# Configure automatic rotation
aws secretsmanager rotate-secret \
  --secret-id prod/myapp/database-password \
  --rotation-lambda-arn arn:aws:lambda:...
```

### 5. VPC Endpoints

```bash
# Create VPC endpoint for Secrets Manager
aws ec2 create-vpc-endpoint \
  --vpc-id vpc-12345678 \
  --service-name com.amazonaws.us-east-1.secretsmanager \
  --vpc-endpoint-type Interface
```

---

## Summary

AWS Secrets Manager provides enterprise-grade secret management:

| Feature | Supported |
|---------|-----------|
| Secrets | ✅ |
| Configuration | ❌ (use Parameter Store) |
| Versioning | ✅ |
| IAM Roles | ✅ |
| Instance Profiles | ✅ |
| Automatic Rotation | ✅ |
| Cross-Region | ✅ |
| VPC Endpoints | ✅ |
| Production use | ✅ |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
