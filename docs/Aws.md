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

**Provider Contract:** The AWS provider implements `ISecretProvider` (backend primitive). `ISecretStore` is provided by Vault core and composes all configured providers. Application code should inject `ISecretStore`, not the provider directly. This provider does not implement `IConfigSource`; use another provider (File, Configuration, Azure KV) for typed configuration access.

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
    options.SecretPrefix = "prod/myapp/";
    // Uses default AWS credential chain (instance profile, env vars, etc.)
});

var app = builder.Build();
```

### Using Named Profile (Local Development)

```csharp
builder.Services.AddVaultWithAwsSecretsManager(options =>
{
    options.Region = "us-east-1";
    options.ProfileName = "my-dev-profile"; // Uses named profile from ~/.aws/credentials
    options.SecretPrefix = "dev/myapp/";
});
```

### AwsSecretsManagerOptions

```csharp
public sealed class AwsSecretsManagerOptions
{
    /// <summary>
    /// The AWS region where secrets are stored.
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
    /// Optional prefix for secret names (e.g., "prod/myapp/").
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

AWS Secrets Manager implementation of `ISecretProvider` (primary backend contract). Also implements `ISecretStore` for off-grid scenarios, but applications should inject Vault's exported `ISecretStore` contract instead.

```csharp
public sealed class AwsSecretsManagerSecretStore : ISecretProvider, ISecretStore
{
    public string ProviderName => "aws-secrets-manager";
    
    // IsAvailable: cheap check (config present, credentials detectable)
    public bool IsAvailable => /* region configured && credentials available */;

    public async Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public async Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
        string secretName,
        CancellationToken cancellationToken = default);

    // CheckHealthAsync: live AWS connectivity and permission check
    public async Task<bool> CheckHealthAsync(
        CancellationToken cancellationToken = default);
}
```

**Availability Semantics:**
- `IsAvailable` returns `true` if the provider is enabled and basic configuration is present (region, auth source)
- `CheckHealthAsync` performs a live AWS Secrets Manager connectivity and permission check

### Implementation Details

- Uses `AWSSDK.SecretsManager`
- Supports versioned secret retrieval (see versioning semantics below)
- Supports secret prefix for organization
- Maps AWS exceptions to vault exceptions

**Secret Name Mapping:** Application code always uses unprefixed logical names (e.g., `"database-connection"`). The AWS provider prepends `SecretPrefix` internally when resolving keys. Fully qualified AWS secret names are constructed by the provider, not by application code.

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

**AWS-Native Versioning:** AWS Secrets Manager uses version labels like `AWSCURRENT` and `AWSPREVIOUS`. Vault treats these as opaque version identifiers. Providers map AWS version metadata to Vault's `SecretVersion` model.

```csharp
// Get current version (default behavior)
var current = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key"),
    ct);

// Get previous version using AWS-native label (passed through as opaque ID)
var previous = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key", "AWSPREVIOUS"),
    ct);

// List all versions (returns Vault's SecretVersion model)
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

## Provider Behavior Mapping

The AWS provider maps AWS-specific semantics to Vault abstractions:

### Exception Mapping

| AWS Exception | Vault Exception |
|---------------|----------------|
| `ResourceNotFoundException` | `SecretNotFoundException` |
| `InvalidRequestException` | `VaultOperationException` |
| `InvalidParameterException` | `VaultOperationException` |
| `DecryptionFailureException` | `VaultOperationException` |
| `InternalServiceErrorException` | `VaultOperationException` |
| `ThrottlingException` | `VaultOperationException` (retry via resilience policy) |

### Metadata Mapping

| AWS Concept | Vault Concept |
|-------------|---------------|
| AWS Version ID (UUID) | `SecretVersion.Version` |
| AWS Version Labels (AWSCURRENT, etc.) | Opaque version identifiers |
| Secret String | `SecretValue.Value` |
| Secret ARN | Not exposed (internal) |

### Configuration Notes

- **Region:** Must be explicitly configured via `AwsSecretsManagerOptions.Region`
- **Credentials:** Follows AWS SDK credential chain (IAM roles → instance profile → environment variables → access keys)
- **No Config Support:** This provider implements only the secret path (`ISecretProvider`), not the configuration path (`IConfigSource`)

---

## Best Practices

### 1. Use Default Credential Chain

```csharp
// ✅ Good - Default credential chain (IAM role, instance profile, env vars)
options.Region = "us-east-1";
options.SecretPrefix = "prod/myapp/";
// Credentials resolved automatically from environment

// For local dev with named profile:
options.ProfileName = "my-dev-profile";
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

**Rotation Model:** Rotation is performed by AWS Secrets Manager. Vault does not rotate secrets itself; it is rotation-aware. Applications should not pin fixed versions unless explicitly needed. Configure Vault's cache TTL relative to your rotation cadence.

```bash
# Configure automatic rotation in AWS
aws secretsmanager rotate-secret \
  --secret-id prod/myapp/database-password \
  --rotation-lambda-arn arn:aws:lambda:...
```

```csharp
// In Vault configuration, set cache TTL relative to rotation schedule
vault.Cache.DefaultTtl = TimeSpan.FromMinutes(15); // Shorter than rotation interval
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

AWS Secrets Manager provides enterprise-grade secret management. This provider implements Vault's `ISecretProvider` contract.

| Feature | Supported |
|---------|-----------||
| Secrets | ✅ |
| Configuration | ❌ (use Parameter Store or another provider) |
| Versioning | ✅ |
| IAM Roles | ✅ |
| Instance Profiles | ✅ |
| Automatic Rotation | ✅ (AWS-managed) |
| Cross-Region | ✅ |
| VPC Endpoints | ✅ |
| Production use | ✅ |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
