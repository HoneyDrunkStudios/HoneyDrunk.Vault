# HoneyDrunk.Vault.Providers.File

File-based secret and configuration provider for HoneyDrunk.Vault. Designed for local development and testing.

## Overview

This provider stores secrets and configuration values in JSON files on the local filesystem. It supports:
- Automatic file watching and reloading
- Optional file encryption via environment variables or file paths
- Both secrets and configuration storage
- Development-friendly error handling

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.File
```

## Quick Start

### Basic Setup

```csharp
using HoneyDrunk.Vault.Providers.File.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVaultWithFile(options =>
{
    options.SecretsFilePath = "secrets/dev-secrets.json";
    options.ConfigFilePath = "secrets/dev-config.json";
    options.WatchForChanges = true;
    options.CreateIfNotExists = true;
});

var app = builder.Build();
```

### Secrets File Format

Create `secrets/dev-secrets.json`:

```json
{
  "database-connection-string": "Server=localhost;Database=myapp;User Id=dev;Password=devpass;",
  "api-key": "dev-api-key-12345",
  "jwt-secret": "your-jwt-secret-for-development"
}
```

### Configuration File Format

Create `secrets/dev-config.json`:

```json
{
  "logging:level": "Debug",
  "cache:ttl": "00:15:00",
  "feature:new-ui": "true",
  "max-connections": "100"
}
```

## Features

### File Watching

Automatically reloads secrets and configuration when files change:

```csharp
options.WatchForChanges = true;  // Default: false
```

### Automatic File Creation

Creates empty files if they don't exist:

```csharp
options.CreateIfNotExists = true;  // Default: false
```

### Optional Encryption

Encrypt sensitive data using an environment variable or file:

```csharp
options.EncryptionKeySource = "MY_ENCRYPTION_KEY";  // Environment variable
// OR
options.EncryptionKeySource = "/path/to/key.txt";   // File path
```

## Configuration Options

### FileVaultOptions

```csharp
public class FileVaultOptions
{
    public string SecretsFilePath { get; set; }
    public string ConfigFilePath { get; set; }
    public bool WatchForChanges { get; set; }
    public bool CreateIfNotExists { get; set; }
    public string? EncryptionKeySource { get; set; }
}
```

## Usage Examples

### Get Secret

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("database-connection-string"));
console.WriteLine($"Connection: {secret.Value}");
```

### Try Get Configuration

```csharp
var result = await configProvider.TryGetValueAsync<int>(
    "max-connections", 
    defaultValue: 50);
console.WriteLine($"Max connections: {result}");
```

### List Secret Versions

```csharp
var versions = await secretStore.ListSecretVersionsAsync("api-key");
foreach (var version in versions)
{
    console.WriteLine($"Version: {version.VersionId}, Created: {version.CreatedAt}");
}
```

## Best Practices

1. **Development Only** - Never use in production
2. **Git Ignore** - Add `secrets/` directory to `.gitignore`
3. **Template Files** - Keep `secrets.example.json` for team reference
4. **Watch Changes** - Enable in development for faster iteration
5. **Create If Not Exists** - Helpful for initial setup
6. **Validate Structure** - Ensure JSON is valid before running

## Error Handling

```csharp
try
{
    var secret = await secretStore.GetSecretAsync(identifier);
}
catch (SecretNotFoundException)
{
    console.WriteLine("Secret not found in file store");
}
catch (System.IO.FileNotFoundException)
{
    console.WriteLine("Secrets file not found");
}
```

## File Structure Example

```
project-root/
??? secrets/
?   ??? dev-secrets.json
?   ??? dev-config.json
?   ??? secrets.example.json
?   ??? .gitignore
??? .gitignore
??? Program.cs
```

### .gitignore for Secrets

```gitignore
# Secrets
secrets/dev-*.json
secrets/local-*.json
!secrets/*.example.json
```

## Performance Considerations

- **File I/O** - Reading from disk on every request (use caching)
- **File Watching** - Minimal overhead, but may impact performance on network drives
- **Large Files** - Consider splitting into multiple files if >1MB
- **JSON Parsing** - Negligible impact for typical configurations

## Limitations

- Single instance only (no distributed coordination)
- No versioning support
- Plain JSON storage (encryption optional)
- File system dependent
- Development-only use case

## Related Providers

- [HoneyDrunk.Vault.Providers.InMemory](../HoneyDrunk.Vault.Providers.InMemory) - For testing
- [HoneyDrunk.Vault.Providers.AzureKeyVault](../HoneyDrunk.Vault.Providers.AzureKeyVault) - For production on Azure
- [HoneyDrunk.Vault.Providers.Aws](../HoneyDrunk.Vault.Providers.Aws) - For production on AWS

## License

MIT License - see LICENSE file for details.
