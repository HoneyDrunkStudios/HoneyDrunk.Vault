# ☁️ Azure Key Vault Provider

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Prerequisites](#prerequisites)
- [Configuration](#configuration)
- [Authentication](#authentication)
- [AzureKeyVaultSecretStore.cs](#azurekeyvaultsecretstorecs)
- [Best Practices](#best-practices)

---

## Overview

Azure Key Vault provider for enterprise-grade secret management. Integrates with Azure Key Vault for centralized, secure secret storage with access control and auditing.

**Location:** `HoneyDrunk.Vault.Providers.AzureKeyVault/`

**Provider Contract:** The Azure Key Vault provider implements `ISecretProvider` (backend secret access) and `IConfigSource` (string-based config retrieval). `ISecretStore` and `IConfigProvider` are Vault core facades that compose all providers. Application code should inject `ISecretStore` or `IConfigProvider`, not the provider directly.

**Use Cases:**
- Production Azure-hosted applications
- Enterprise secret management
- Compliance requirements (SOC 2, HIPAA, etc.)
- Centralized secret rotation
- Access control and auditing

---

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.AzureKeyVault
```

---

## Prerequisites

1. **Azure Subscription** with Key Vault access
2. **Azure Key Vault Instance** created in your subscription
3. **Authentication** configured:
   - Managed Identity (recommended for Azure-hosted apps)
   - Service Principal (for local development or non-Azure environments)

---

## Configuration

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
    options.TenantId = builder.Configuration["AzureAd:TenantId"];
    options.ClientId = builder.Configuration["AzureAd:ClientId"];
    // ClientSecret should come from environment or secure config
    options.UseManagedIdentity = false;
});
```

### AzureKeyVaultOptions

```csharp
public sealed class AzureKeyVaultOptions
{
    /// <summary>
    /// The URI of the Azure Key Vault instance.
    /// </summary>
    public Uri? VaultUri { get; set; }

    /// <summary>
    /// Whether to use Managed Identity for authentication.
    /// Default: true
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// The Azure AD tenant ID (for service principal auth).
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// The Azure AD client/application ID (for service principal auth).
    /// </summary>
    public string? ClientId { get; set; }
}
```

---

## Authentication

### Managed Identity (Recommended)

For Azure-hosted applications (App Service, Container Apps, AKS, VMs):

```bash
# Enable system-assigned managed identity
az webapp identity assign --name my-app --resource-group my-rg

# Grant Key Vault access
az keyvault set-policy \
  --name my-vault \
  --object-id <identity-object-id> \
  --secret-permissions get list
```

### Service Principal

For local development or non-Azure environments:

```bash
# Create service principal
az ad sp create-for-rbac --name my-app-sp

# Grant Key Vault access
az keyvault set-policy \
  --name my-vault \
  --spn <app-id> \
  --secret-permissions get list
```

### Azure RBAC (Modern)

Using Azure RBAC instead of access policies:

```bash
# Assign Key Vault Secrets User role
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee <identity-object-id> \
  --scope /subscriptions/<sub>/resourceGroups/<rg>/providers/Microsoft.KeyVault/vaults/<vault>
```

---

## AzureKeyVaultSecretStore.cs

Azure Key Vault provider implements `ISecretProvider` (primary backend contract) and `IConfigSource` (string-based configuration). Also implements `ISecretStore` for off-grid scenarios, but applications should inject Vault's exported `ISecretStore` contract instead.

```csharp
public sealed class AzureKeyVaultSecretStore : ISecretProvider, IConfigSource, ISecretStore
{
    public string ProviderName => "azure-key-vault";
    
    // IsAvailable: cheap check (VaultUri configured, credentials present)
    public bool IsAvailable => _options?.VaultUri is not null;

    public async Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public async Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
        string secretName,
        CancellationToken cancellationToken = default);

    // CheckHealthAsync: live Azure Key Vault connectivity check
    public async Task<bool> CheckHealthAsync(
        CancellationToken cancellationToken = default);
}
```

**Availability Semantics:**
- `IsAvailable` returns `true` if the provider is enabled and configuration is present (VaultUri, authentication method)
- `CheckHealthAsync` performs a live Azure Key Vault connectivity and permission check

### Implementation Details

- Uses `Azure.Security.KeyVault.Secrets.SecretClient`
- Supports versioned secret retrieval
- Maps Azure exceptions to vault exceptions
- Includes health check for connectivity

**Secret Name Mapping:** Applications always reference secrets by logical names (e.g., `"payment-gateway-secret"`). The Azure provider translates logical names into actual Key Vault secret identifiers and URIs internally, applying Key Vault naming rules as needed.

### Usage Example

```csharp
public class PaymentService(ISecretStore secretStore)
{
    public async Task<PaymentResult> ProcessPaymentAsync(
        Payment payment,
        CancellationToken ct)
    {
        // Get payment gateway credentials
        var apiKey = await secretStore.GetSecretAsync(
            new SecretIdentifier("payment-gateway-api-key"),
            ct);

        var apiSecret = await secretStore.GetSecretAsync(
            new SecretIdentifier("payment-gateway-secret"),
            ct);

        // Process payment with credentials
        return await ProcessWithGateway(payment, apiKey.Value, apiSecret.Value, ct);
    }
}
```

### Versioned Secrets

**Azure Key Vault Versioning:** Version identifiers in Azure Key Vault are opaque strings (UUIDs). Vault treats them as plain identifiers and does not interpret them. The provider maps Azure version metadata into Vault's `SecretVersion` model.

```csharp
// Get latest version (default behavior)
var latest = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key"),
    ct);

// Get specific version (opaque Azure version ID)
var v1 = await secretStore.GetSecretAsync(
    new SecretIdentifier("api-key", "abc123"),
    ct);

// List all versions (returns Vault's SecretVersion model)
var versions = await secretStore.ListSecretVersionsAsync("api-key", ct);
foreach (var version in versions)
{
    Console.WriteLine($"Version: {version.Version}, Created: {version.CreatedOn}");
}
```

---

## Provider Behavior Mapping

The Azure Key Vault provider maps Azure-specific semantics to Vault abstractions:

### Exception Mapping

| Azure Exception | Vault Exception |
|----------------|----------------|
| `RequestFailedException` (404) | `SecretNotFoundException` |
| `RequestFailedException` (403) | `VaultOperationException` |
| `RequestFailedException` (401) | `VaultOperationException` |
| `AuthenticationFailedException` | `VaultOperationException` |
| `InvalidOperationException` | `VaultOperationException` |

### Metadata Mapping

| Azure Concept | Vault Concept |
|---------------|---------------|
| Azure Version ID (UUID) | `SecretVersion.Version` |
| Secret Value | `SecretValue.Value` |
| Secret Properties | `SecretVersion` metadata (CreatedOn, UpdatedOn) |
| Secret Identifier | Not exposed (internal) |

### Configuration Notes

- **VaultUri:** Must be explicitly configured (e.g., `https://my-vault.vault.azure.net/`)
- **Authentication:** Managed Identity (recommended) or Service Principal
- **Config Support:** Implements `IConfigSource` for string-based retrieval only; not recommended for general configuration. Use File, Configuration Provider, or Azure App Configuration instead.

---

## Best Practices

### 1. Use Managed Identity

```csharp
// ✅ Good - Managed Identity (no secrets in code)
options.UseManagedIdentity = true;

// ❌ Avoid - Hardcoded credentials
options.ClientSecret = "secret-value";
```

### 2. Minimal Permissions

```bash
# ✅ Good - Only required permissions
az keyvault set-policy --secret-permissions get list

# ❌ Avoid - Excessive permissions
az keyvault set-policy --secret-permissions all
```

### 3. Use Private Endpoints

```bash
# Create private endpoint for Key Vault
az keyvault update --name my-vault --public-network-access disabled
az network private-endpoint create ...
```

### 4. Enable Soft Delete

```bash
# Enable soft delete (default on new vaults)
az keyvault update --name my-vault --enable-soft-delete true
az keyvault update --name my-vault --retention-days 90
```

### 5. Do Not Use Key Vault for General Configuration

**Best Practice:** Do not treat Key Vault as a general-purpose configuration store. Key Vault is optimized for secrets (API keys, connection strings, certificates). Use Vault's configuration providers (File, IConfiguration, Azure App Configuration) for non-secret settings like feature flags, timeouts, and application settings.

### 6. Secret Rotation

**Rotation Model:** Azure Key Vault supports rotation policies for keys and certificates, and event-driven rotation for secrets via Key Vault events and automation. Vault is rotation-aware, not rotation-driving. Applications should not pin fixed versions unless explicitly needed. Configure Vault's cache TTL relative to your rotation cadence.

```bash
# Monitor Key Vault events for rotation
az eventgrid event-subscription create \
  --source-resource-id /subscriptions/.../providers/Microsoft.KeyVault/vaults/my-vault \
  --name secret-rotation-events
```

```csharp
// In Vault configuration, set cache TTL relative to rotation schedule
vault.Cache.DefaultTtl = TimeSpan.FromMinutes(15); // Shorter than rotation interval
```

---

## Summary

Azure Key Vault provides enterprise-grade secret management. This provider implements Vault's `ISecretProvider` and `IConfigSource` contracts.

| Feature | Supported |
|---------|-----------||
| Secrets | ✅ |
| Configuration | ❌ (string retrieval via IConfigSource only; use App Configuration for real config) |
| Versioning | ✅ |
| Managed Identity | ✅ |
| RBAC | ✅ |
| Audit Logging | ✅ |
| Private Endpoints | ✅ |
| Soft Delete | ✅ |
| Production use | ✅ |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
