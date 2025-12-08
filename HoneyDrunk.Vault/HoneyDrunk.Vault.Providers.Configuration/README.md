# HoneyDrunk.Vault.Providers.Configuration

Configuration provider for HoneyDrunk.Vault. Enables reading secrets and configuration from .NET configuration sources.

## Overview

This provider bridges HoneyDrunk.Vault with the standard .NET configuration system (appsettings.json, environment variables, etc.). It supports:
- Reading from IConfiguration
- Multiple configuration sources
- Environment variable overrides
- User secrets integration
- Configuration reloading

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.Configuration
```

## Quick Start

### Basic Setup

```csharp
using HoneyDrunk.Vault.Providers.Configuration.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVaultWithConfiguration();

var app = builder.Build();
```

### With Prefix

```csharp
builder.Services.AddVaultWithConfiguration(options =>
{
    options.Prefix = "Vault:";  // Reads from Vault:* configuration keys
});
```

## Configuration Format

### appsettings.json

```json
{
  "Vault": {
    "DatabaseConnection": "Server=prod-db.azure.com;Database=myapp;",
    "ApiKey": "your-api-key-12345",
    "JwtSecret": "your-jwt-secret"
  }
}
```

### appsettings.Production.json

```json
{
  "Vault": {
    "DatabaseConnection": "Server=prod-db.azure.com;Database=myapp;",
    "ApiKey": "prod-api-key",
    "JwtSecret": "prod-jwt-secret"
  }
}
```

### Environment Variables

```bash
export Vault__DatabaseConnection="Server=..."
export Vault__ApiKey="api-key-value"
export Vault__JwtSecret="jwt-secret-value"
```

### User Secrets (Development)

```bash
dotnet user-secrets set "Vault:DatabaseConnection" "Server=dev-db;..."
dotnet user-secrets set "Vault:ApiKey" "dev-api-key"
```

## Usage Examples

### Get Secret

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("DatabaseConnection"));
console.WriteLine($"Connection: {secret.Value}");
```

### Get Configuration Value

```csharp
var apiKey = await configProvider.TryGetValueAsync("ApiKey");
if (apiKey != null)
{
    console.WriteLine($"API Key: {apiKey}");
}
```

### Get Typed Configuration

```csharp
public class VaultConfiguration
{
    public string? DatabaseConnection { get; set; }
    public string? ApiKey { get; set; }
    public int Timeout { get; set; } = 30;
}

var config = configuration.GetSection("Vault").Get<VaultConfiguration>();
console.WriteLine($"Database: {config?.DatabaseConnection}");
console.WriteLine($"Timeout: {config?.Timeout}");
```

## Configuration Options

### ConfigurationProviderOptions

```csharp
public class ConfigurationProviderOptions
{
    public string Prefix { get; set; } = "Vault";
    public bool ReloadOnChange { get; set; } = true;
    public IConfiguration? Configuration { get; set; }
}
```

## Configuration Hierarchy

Configuration is loaded in this order (later sources override earlier ones):

1. appsettings.json
2. appsettings.{Environment}.json
3. Environment variables
4. User secrets (Development only)
5. Command-line arguments

### Example Hierarchy

```
appsettings.json              # Base configuration
??? "DatabaseConnection": "localhost"
??? "ApiKey": "dev-key"

appsettings.Production.json   # Overrides
??? "DatabaseConnection": "prod-db.azure.com"
??? "ApiKey": "prod-key"

Environment variables         # Final overrides
??? Vault__DatabaseConnection=override-db
??? Vault__ApiKey=override-key
```

## Advanced Configuration

### Named Sections

```json
{
  "Vault": {
    "Database": {
      "ConnectionString": "...",
      "Timeout": 30
    },
    "Api": {
      "Key": "...",
      "Endpoint": "..."
    }
  }
}
```

```csharp
var dbConfig = configuration.GetSection("Vault:Database").Get<DatabaseConfig>();
var apiConfig = configuration.GetSection("Vault:Api").Get<ApiConfig>();
```

### Configuration Binding

```csharp
public class VaultOptions
{
    public DatabaseSettings? Database { get; set; }
    public ApiSettings? Api { get; set; }
}

public class DatabaseSettings
{
    public string? ConnectionString { get; set; }
    public int Timeout { get; set; }
    public bool Pooling { get; set; }
}

public class ApiSettings
{
    public string? Key { get; set; }
    public string? Endpoint { get; set; }
    public int MaxRetries { get; set; }
}

var options = new VaultOptions();
configuration.GetSection("Vault").Bind(options);
```

## Best Practices

1. **Use Environment-Specific Files** - Keep production secrets separate
2. **User Secrets in Development** - Never commit local development secrets
3. **Environment Variables in Production** - Use deployment settings
4. **Validate Configuration** - Check required values exist
5. **Use Typed Options** - Leverage strong typing
6. **Prefix Organization** - Organize related secrets logically
7. **Document Defaults** - Make default values clear
8. **Reload on Change** - Enable for development convenience

## Integration with Dependency Injection

### Options Pattern

```csharp
builder.Services.Configure<VaultOptions>(
    configuration.GetSection("Vault"));

public class MyService
{
    private readonly IOptions<VaultOptions> _options;

    public MyService(IOptions<VaultOptions> options)
    {
        _options = options;
    }

    public string GetApiKey() => _options.Value.ApiKey!;
}
```

### Direct Injection

```csharp
builder.Services.AddSingleton(
    configuration.GetSection("Vault").Get<VaultOptions>() 
    ?? new VaultOptions());
```

## Configuration Validation

### Data Annotations

```csharp
public class VaultOptions
{
    [Required]
    [MinLength(10)]
    public string? DatabaseConnection { get; set; }

    [Required]
    public string? ApiKey { get; set; }

    [Range(1, 300)]
    public int Timeout { get; set; } = 30;
}

var options = new VaultOptions();
configuration.GetSection("Vault").Bind(options);

var context = new ValidationContext(options);
Validator.ValidateObject(options, context, validateAllProperties: true);
```

### Custom Validation

```csharp
if (string.IsNullOrEmpty(options.DatabaseConnection))
{
    throw new InvalidOperationException(
        "Vault:DatabaseConnection is required");
}
```

## Limitations

- No secret versioning
- No encryption at rest (use Azure Key Vault or similar)
- Configuration must be in code or config files
- Not suitable for sensitive production secrets
- Limited to configured sources only

## Use Cases

- Development and testing
- Non-sensitive configuration values
- Feature flags and settings
- Application behavior configuration
- Local overrides for development

## Related Providers

- [HoneyDrunk.Vault.Providers.AzureKeyVault](../HoneyDrunk.Vault.Providers.AzureKeyVault) - For production secrets
- [HoneyDrunk.Vault.Providers.Aws](../HoneyDrunk.Vault.Providers.Aws) - For AWS Secrets Manager
- [HoneyDrunk.Vault.Providers.File](../HoneyDrunk.Vault.Providers.File) - For file-based development
- [HoneyDrunk.Vault.Providers.InMemory](../HoneyDrunk.Vault.Providers.InMemory) - For testing

## References

- [.NET Configuration](https://docs.microsoft.com/en-us/dotnet/core/extensions/configuration)
- [Options Pattern in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/options)
- [User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)

## License

MIT License - see LICENSE file for details.
