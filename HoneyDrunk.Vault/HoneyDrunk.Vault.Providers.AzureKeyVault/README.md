# HoneyDrunk.Vault.Providers.AzureKeyVault

Azure Key Vault provider for HoneyDrunk.Vault. **Recommended for Azure hosted applications.**

## Overview

This provider integrates HoneyDrunk.Vault with Azure Key Vault, giving you secure, versioned secret retrieval through the standard Vault abstractions (`ISecretStore`, `SecretIdentifier`, `SecretValue`). It is the preferred provider for applications running in Azure App Service, Azure Container Apps, AKS or any Azure VM with Managed Identity enabled.

**Features:**
- Managed Identity authentication (best practice for production)
- Optional Service Principal authentication for local development
- Versioned secret retrieval
- Azure RBAC and access policy support
- Integration with Vault caching, resilience and telemetry
- Zero secret values ever logged or emitted in telemetry

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.AzureKeyVault
```

## Prerequisites

- Azure subscription
- Azure Key Vault instance
- One of:
  - Managed Identity enabled on your application (recommended)
  - Service Principal with secret read permissions

## Quick Start

### Using Managed Identity (Recommended)

```csharp
using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVaultWithAzureKeyVault(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    options.UseManagedIdentity = true;
});

var app = builder.Build();
```

### Using Service Principal (Local Dev or Non-Azure Hosts)

```csharp
builder.Services.AddVaultWithAzureKeyVault(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    options.TenantId = builder.Configuration["AzureAd:TenantId"];
    options.ClientId = builder.Configuration["AzureAd:ClientId"];
    // ClientSecret should come from secure config, not source code
    options.UseManagedIdentity = false;
});
```

## Configuration Options

```csharp
public sealed class AzureKeyVaultOptions
{
    public Uri? VaultUri { get; set; }
    public bool UseManagedIdentity { get; set; } = true;
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}
```

**If `UseManagedIdentity = true`**, Vault uses `DefaultAzureCredential` and falls back through the Azure identity chain (Managed Identity, Azure CLI login, Visual Studio login, etc).

## Setup Instructions

### 1. Create Secrets in Key Vault

```bash
az keyvault secret set \
  --vault-name my-vault \
  --name db-connection-string \
  --value "Server=..."
```

**Later updates automatically create new versions.**

### 2. Grant Access to Your Application

```bash
# For Managed Identity
az keyvault set-policy \
  --name my-vault \
  --object-id <identity-object-id> \
  --secret-permissions get list

# For Service Principal
az keyvault set-policy \
  --name my-vault \
  --spn <client-id> \
  --secret-permissions get list
```

**For secret-only access, certificate and key permissions are not required.**

## Usage Examples

### Get the Latest Version of a Secret

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("db-connection-string"),
    ct);

Console.WriteLine(secret.Value.Length);
```

**Azure Key Vault returns the latest version when no version is specified.**

### Get a Specific Version

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key", "3f92c96c7d9e4f1e9a5e2bb0a1b7e3a1"),
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

### Gracefully Handle Missing Secrets

```csharp
var result = await secretStore.TryGetSecretAsync(
    new SecretIdentifier("optional-secret"),
    ct);

if (result.IsSuccess)
    Console.WriteLine("Secret available");
else
    Console.WriteLine($"Not available: {result.ErrorMessage}");
```

## Access Control

### Managed Identity

```bash
az keyvault set-policy \
  --name my-vault \
  --object-id <identity-object-id> \
  --secret-permissions get list
```

### Service Principal

```bash
az keyvault set-policy \
  --name my-vault \
  --spn <client-id> \
  --secret-permissions get list
```

**For secret-only access, certificate and key permissions are not required.**

## Configuration Example

```json
{
  "Vault": {
    "AzureKeyVault": {
      "VaultUri": "https://my-vault.vault.azure.net/",
      "UseManagedIdentity": true
    }
  }
}
```

**You can bind this in `AddVaultWithAzureKeyVault`.**

## Best Practices

1. **Prefer Managed Identity for production**
2. **Enable Soft Delete and Purge Protection on your vault**
3. **Use secret versions intentionally for rollback**
4. **Do not log secret values, only names**
5. **Enable Vault caching to reduce Key Vault API usage**
6. **Use Private Endpoints for secure and high performance networking**
7. **Grant least privilege access (only `get` and `list` for secrets)**

## Troubleshooting

### Authentication Issues

```
MsalServiceException: AADSTS700016: Application not found
```

- Verify `TenantId`, `ClientId` and `ClientSecret` if using a Service Principal
- Ensure Managed Identity is enabled if using MI
- Check Key Vault firewall or private endpoint settings

### Permission Denied

```
KeyVaultErrorException: Access denied
```

- Ensure `get` and `list` permissions are granted for secrets
- Verify the identity object ID used in `set-policy`

### Timeouts or Rate Limiting

- Ensure Vault caching is enabled
- Prefer Private Endpoints for low latency
- Avoid repeatedly fetching the same secret in a hot path

## Related Providers

- [HoneyDrunk.Vault.Providers.Aws](../HoneyDrunk.Vault.Providers.Aws)
- [HoneyDrunk.Vault.Providers.File](../HoneyDrunk.Vault.Providers.File)
- [HoneyDrunk.Vault.Providers.InMemory](../HoneyDrunk.Vault.Providers.InMemory)

## License

MIT License
