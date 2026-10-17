# HoneyDrunk.Vault.Providers.Configuration

Configuration provider for HoneyDrunk.Vault. Bridges Vault abstractions with .NET's `IConfiguration` system.

## Overview

This provider lets you use Vault's `ISecretStore` and `IConfigProvider` interfaces while reading values from standard .NET configuration sources (appsettings.json, environment variables, user secrets). It's designed for:
- **Local development** - Use appsettings.json or user secrets instead of cloud providers
- **Migration scenarios** - Gradually move from config files to proper secret stores
- **Testing and prototyping** - Quick setup without external dependencies

**Important Limitations:**
- **No secret versioning** - Only supports latest values
- **No rotation** - Static configuration, no automatic updates
- **No encryption at rest** - Plain text in config files
- **Not suitable for sensitive production secrets** - Use Azure Key Vault or AWS Secrets Manager instead

**How it works:**
- Secrets accessed via `ISecretStore` → reads from `Secrets:` configuration section
- Config accessed via `IConfigProvider` → reads from any configuration key
- Everything else → standard .NET `IConfiguration` binding

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.Configuration
```

## Quick Start

### Basic Setup

```csharp
using HoneyDrunk.Vault.Providers.Configuration.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVaultWithConfiguration(builder.Configuration);

var app = builder.Build();
```

This registers `ISecretStore` and `IConfigProvider` backed by `IConfiguration`.

## Configuration Format

### Secrets (Accessed via ISecretStore)

Secrets must be under the `Secrets:` section:

**appsettings.json**
```json
{
  "Secrets": {
    "DatabaseConnection": "Server=localhost;Database=myapp;",
    "ApiKey": "dev-api-key-12345",
    "JwtSecret": "dev-jwt-secret"
  }
}
```

**appsettings.Production.json**
```json
{
  "Secrets": {
    "DatabaseConnection": "Server=prod-db.azure.com;Database=myapp;",
    "ApiKey": "prod-api-key",
    "JwtSecret": "prod-jwt-secret"
  }
}
```

**Environment Variables**
```bash
export Secrets__DatabaseConnection="Server=..."
export Secrets__ApiKey="api-key-value"
export Secrets__JwtSecret="jwt-secret-value"
```

**User Secrets (Development)**
```bash
dotnet user-secrets set "Secrets:DatabaseConnection" "Server=dev-db;..."
dotnet user-secrets set "Secrets:ApiKey" "dev-api-key"
```

## Usage Examples

### Using Vault Abstractions (ISecretStore)

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("DatabaseConnection"),
    ct);

Console.WriteLine($"Connection string length: {secret.Value.Length}");
```

This reads from `Secrets:DatabaseConnection` in configuration.

### Using Vault Abstractions (IConfigProvider)

```csharp
var apiKey = await configProvider.TryGetValueAsync("ApiSettings:Key", ct);
if (apiKey != null)
{
    Console.WriteLine($"API Key configured: {apiKey.Length} chars");
}
```

This reads from any configuration key (not limited to `Secrets:`).

### Plain .NET Configuration (Not Vault)

The examples below use standard .NET `IConfiguration` directly. This is **not part of Vault**—these are native .NET patterns:

```csharp
// Typed configuration binding (standard .NET)
public class ApiSettings
{
    public string? Key { get; set; }
    public string? Endpoint { get; set; }
    public int Timeout { get; set; } = 30;
}

var apiSettings = configuration.GetSection("ApiSettings").Get<ApiSettings>();
Console.WriteLine($"Endpoint: {apiSettings?.Endpoint}");
```

## Configuration Hierarchy

Configuration sources are loaded in this order (later sources override earlier ones):

1. appsettings.json
2. appsettings.{Environment}.json
3. Environment variables
4. User secrets (Development only)
5. Command-line arguments

**Example:**

```
appsettings.json
└─ "Secrets:DatabaseConnection": "localhost"
└─ "Secrets:ApiKey": "dev-key"

appsettings.Production.json (overrides)
└─ "Secrets:DatabaseConnection": "prod-db.azure.com"
└─ "Secrets:ApiKey": "prod-key"

Environment variables (final overrides)
└─ Secrets__DatabaseConnection=override-db
└─ Secrets__ApiKey=override-key
```

## Best Practices

1. **Use for development only** - Not suitable for production secrets
2. **Separate secrets from config** - Keep secrets under `Secrets:` section
3. **Use User Secrets in dev** - Never commit sensitive values to source control
4. **Use environment variables in production** - Or migrate to Azure Key Vault / AWS Secrets Manager
5. **Document required secrets** - Make it clear which `Secrets:` keys must be configured
6. **Validate on startup** - Check required secrets exist before running

## When to Use This Provider

**Good for:**
- Local development and debugging
- Unit testing without external dependencies
- Prototyping and proof-of-concept work
- Migration from config files to proper secret stores

**Not suitable for:**
- Production secrets (database passwords, API keys, certificates)
- Scenarios requiring secret rotation or versioning
- Multi-tenant applications with per-tenant secrets
- Compliance requirements (SOC2, PCI-DSS, HIPAA)

## Related Providers

- [HoneyDrunk.Vault.Providers.AzureKeyVault](../HoneyDrunk.Vault.Providers.AzureKeyVault) - For production secrets in Azure
- [HoneyDrunk.Vault.Providers.Aws](../HoneyDrunk.Vault.Providers.Aws) - For production secrets in AWS
- [HoneyDrunk.Vault.Providers.File](../HoneyDrunk.Vault.Providers.File) - For file-based development
- [HoneyDrunk.Vault.Providers.InMemory](../HoneyDrunk.Vault.Providers.InMemory) - For testing

## License

MIT License
