# HoneyDrunk.Vault.Providers.AzureKeyVault

Azure Key Vault provider for HoneyDrunk.Vault. **Recommended for Azure-hosted applications.**

## Overview

This provider integrates with Azure Key Vault for enterprise-grade secret management. Perfect for production workloads running in Azure.

**Features:**
- Managed Identity authentication (recommended for production)
- Service Principal authentication (for local development)
- Automatic secret versioning
- Native Azure integration
- RBAC and access policies
- Hardware Security Module (HSM) backed secrets

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.AzureKeyVault
```

## Prerequisites

- Azure subscription
- Azure Key Vault instance
- Either:
  - Application with Managed Identity enabled (recommended)
  - Service Principal with appropriate permissions

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

### Using Service Principal

```csharp
builder.Services.AddVaultWithAzureKeyVault(options =>
{
    options.VaultUri = new Uri("https://my-vault.vault.azure.net/");
    options.TenantId = "your-tenant-id";
    options.ClientId = "your-client-id";
    // ClientSecret should come from configuration, not hardcoded
});
```

## Configuration Options

### AzureKeyVaultProviderOptions

```csharp
public class AzureKeyVaultProviderOptions
{
    public Uri? VaultUri { get; set; }
    public bool UseManagedIdentity { get; set; } = true;
    public string? ClientId { get; set; }
    public string? TenantId { get; set; }
}
```

## Setup Instructions

### 1. Create Azure Key Vault

```bash
az keyvault create --name my-vault --resource-group my-rg --location eastus
```

### 2. Configure Managed Identity

For Azure App Service or Azure Container Instances:

```bash
# Enable system-assigned managed identity
az webapp identity assign --name my-app --resource-group my-rg

# Grant access to Key Vault
az keyvault set-policy \
  --name my-vault \
  --object-id <identity-object-id> \
  --secret-permissions get list \
  --certificate-permissions get list \
  --key-permissions get list
```

### 3. Add Secrets to Key Vault

```bash
az keyvault secret set --vault-name my-vault --name db-connection-string --value "Server=..."
az keyvault secret set --vault-name my-vault --name api-key --value "your-api-key"
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

### Managed Identity Permissions

```bash
# Read secrets
az keyvault set-policy \
  --name my-vault \
  --object-id <managed-identity-id> \
  --secret-permissions get list

# Read certificates
az keyvault set-policy \
  --name my-vault \
  --object-id <managed-identity-id> \
  --certificate-permissions get list

# Read keys
az keyvault set-policy \
  --name my-vault \
  --object-id <managed-identity-id> \
  --key-permissions get list
```

### Service Principal Permissions

```bash
# Create service principal
az ad sp create-for-rbac --name my-sp

# Grant permissions
az keyvault set-policy \
  --name my-vault \
  --spn <service-principal-id> \
  --secret-permissions get list \
  --certificate-permissions get list
```

## Environment Configuration

Store credentials securely:

```json
{
  "Vault": {
    "AzureKeyVault": {
      "VaultUri": "https://my-vault.vault.azure.net/",
      "UseManagedIdentity": true,
      "TenantId": "optional-for-service-principal",
      "ClientId": "optional-for-service-principal"
    }
  }
}
```

## Best Practices

1. **Use Managed Identity** - More secure than storing credentials
2. **Enable Soft Delete** - Prevent accidental deletion
3. **Enable Purge Protection** - Protect critical secrets
4. **Rotate Credentials** - Regularly update service principal credentials
5. **Use Secret Versions** - Maintain history and rollback capability
6. **Principle of Least Privilege** - Grant minimal required permissions
7. **Enable Logging** - Monitor access to secrets
8. **Use RBAC** - Leverage Azure AD roles for access control

## Performance Considerations

- **Caching** - Use Vault's caching to reduce API calls
- **Rate Limiting** - Key Vault has rate limits (see Azure docs)
- **Network** - Prefer Azure Private Endpoints for security
- **Secrets Size** - Keep secrets reasonably sized (<4KB)

## Troubleshooting

### Authentication Errors

```
MsalServiceException: AADSTS700016: Application not found in directory
```

- Verify Application ID and Tenant ID
- Ensure service principal has required permissions
- Check managed identity is enabled

### Permission Errors

```
KeyVaultErrorException: Access denied. Make sure that the caller has permissions
```

- Verify access policies in Key Vault
- Check managed identity or service principal has `get` and `list` permissions
- Use `az keyvault show-deleted --name my-vault` to check deletion status

### Timeout Errors

- Enable caching to reduce API calls
- Check network connectivity
- Consider using Private Endpoints

## Related Providers

- [HoneyDrunk.Vault.Providers.Aws](../HoneyDrunk.Vault.Providers.Aws) - For AWS Secrets Manager
- [HoneyDrunk.Vault.Providers.File](../HoneyDrunk.Vault.Providers.File) - For local development
- [HoneyDrunk.Vault.Providers.InMemory](../HoneyDrunk.Vault.Providers.InMemory) - For testing

## References

- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [Managed Identities for Azure Resources](https://docs.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/)
- [Azure SDK for .NET - Key Vault](https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/keyvault)

## License

MIT License - see LICENSE file for details.
