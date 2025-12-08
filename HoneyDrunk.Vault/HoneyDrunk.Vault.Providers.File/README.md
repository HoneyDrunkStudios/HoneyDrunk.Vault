# HoneyDrunk.Vault.Providers.File

File-based provider for HoneyDrunk.Vault. **Designed for local development and testing only.**

## Overview

This provider stores secrets and configuration in JSON files on the local filesystem. It lets you use Vault abstractions (`ISecretStore`, `IConfigProvider`) without configuring cloud services.

**Intended for:**
- Local development
- Quick prototyping
- Integration testing without external dependencies

**Important Limitations:**
- **No versioning** - Only returns synthetic "latest" version
- **No rotation** - Static file content, manual updates only
- **Single instance only** - File watching does not propagate across multiple running processes
- **Not production-ready** - Even with encryption, this is not secure for production secrets

**Features:**
- JSON file storage for secrets and configuration
- Optional file watching and hot-reloading (single instance only)
- Optional encryption using environment variable or file-based keys
- Automatic file creation if not exists
- No external dependencies or cloud accounts needed

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

### File Watching (Single Instance Only)

Automatically reloads secrets and configuration when files change:

```csharp
options.WatchForChanges = true;  // Default: true
```

**Warning:** File watching only affects the current process. If you run multiple instances (e.g., multiple debug sessions), each maintains its own in-memory cache. Changes won't propagate across instances.

### Automatic File Creation

Creates empty files if they don't exist:

```csharp
options.CreateIfNotExists = true;  // Default: true
```

### Optional Encryption (Development Only)

**This encryption is for protecting secrets from accidental commits to source control, not for production security.**

Encrypt file contents using a key from an environment variable:

```csharp
options.EncryptionKeyEnvironmentVariable = "MY_ENCRYPTION_KEY";
```

Or from a file:

```csharp
options.EncryptionKeyFilePath = "/path/to/key.txt";
```

**How it works:**
- Encryption key is loaded at startup from the specified source
- Secrets file is encrypted at rest on disk
- Provider decrypts in memory when reading secrets
- **Does not protect against:**
  - Local attackers with file system access
  - Memory dumps or debugging tools
  - Key compromise if stored alongside secrets

**Never use this for production secrets.** Use Azure Key Vault or AWS Secrets Manager instead.

## Configuration Options

```csharp
public sealed class FileVaultOptions
{
    /// <summary>
    /// Path to the secrets file. Default: "secrets.json".
    /// </summary>
    public string SecretsFilePath { get; set; } = "secrets.json";

    /// <summary>
    /// Path to the configuration file. Default: "config.json".
    /// </summary>
    public string ConfigFilePath { get; set; } = "config.json";

    /// <summary>
    /// Environment variable name containing the encryption key.
    /// If not set, secrets are stored in plain text.
    /// </summary>
    public string? EncryptionKeyEnvironmentVariable { get; set; }

    /// <summary>
    /// Path to a file containing the encryption key.
    /// </summary>
    public string? EncryptionKeyFilePath { get; set; }

    /// <summary>
    /// Whether to watch for file changes. Default: true.
    /// </summary>
    public bool WatchForChanges { get; set; } = true;

    /// <summary>
    /// Whether to create files if they don't exist. Default: true.
    /// </summary>
    public bool CreateIfNotExists { get; set; } = true;
}
```

## Usage Examples

### Get Secret

```csharp
var secret = await secretStore.GetSecretAsync(
    new SecretIdentifier("database-connection-string"),
    ct);

Console.WriteLine($"Connection string length: {secret.Value.Length}");
```

### Try Get Configuration

```csharp
var result = await configProvider.TryGetValueAsync<int>(
    "max-connections", 
    defaultValue: 50,
    ct);

Console.WriteLine($"Max connections: {result}");
```

### List Secret Versions (Always Returns Single Synthetic Version)

```csharp
var versions = await secretStore.ListSecretVersionsAsync("api-key", ct);

// File provider always returns a single "latest" version
foreach (var version in versions)
{
    Console.WriteLine($"Version: {version.Version}, Created: {version.CreatedOn}");
}
```

**Note:** File provider does not support true versioning. `ListSecretVersionsAsync` always returns a single synthetic version labeled "latest".

## Best Practices

1. **Never use in production** - Not secure even with encryption enabled
2. **Add to .gitignore** - Prevent accidental secret commits: `secrets/dev-*.json`
3. **Provide template files** - Keep `secrets.example.json` in source control (see example below)
4. **Enable watching in dev** - Set `WatchForChanges = true` for faster iteration
5. **Document required secrets** - List all expected keys in your README or template
6. **Validate on startup** - Check that required secrets exist before running
7. **Single instance assumption** - Don't rely on hot-reload across multiple processes

## Error Handling

```csharp
try
{
    var secret = await secretStore.GetSecretAsync(identifier, ct);
}
catch (SecretNotFoundException)
{
    Console.WriteLine("Secret not found in file store");
}
catch (System.IO.FileNotFoundException)
{
    Console.WriteLine("Secrets file not found");
}
```

## File Structure Example

```
project-root/
├─ secrets/
│  ├─ dev-secrets.json        # Actual secrets (git-ignored)
│  ├─ dev-config.json         # Actual config (git-ignored)
│  ├─ secrets.example.json    # Template (committed to git)
│  └─ .gitignore
├─ .gitignore
└─ Program.cs
```

### secrets.example.json Template

```json
{
  "database-connection-string": "Server=localhost;Database=myapp;User Id=dev;Password=CHANGE_ME;",
  "api-key": "CHANGE_ME",
  "jwt-secret": "CHANGE_ME"
}
```

**Team members copy this to `dev-secrets.json` and fill in real values.**

### .gitignore for Secrets

```gitignore
# Ignore actual secret files
secrets/dev-*.json
secrets/local-*.json

# Keep template files
!secrets/*.example.json
```

## Performance Considerations

- **File I/O** - Reads from disk and caches in memory (use Vault caching layer for better performance)
- **File Watching** - Minimal overhead for single instance; does not coordinate across multiple processes
- **Large Files** - Keep files under 1MB; consider splitting if larger
- **JSON Parsing** - Negligible impact for typical secret counts (<1000 keys)

## Limitations

- **Development only** - Not suitable for production secrets
- **No true versioning** - Only synthetic "latest" version returned
- **No rotation** - Manual file edits required
- **Single instance coordination** - Hot-reload does not propagate across multiple processes
- **Encryption is not production-grade** - Protects against accidental commits, not attackers
- **File system dependent** - Platform-specific path handling may vary

## Related Providers

- [HoneyDrunk.Vault.Providers.InMemory](../HoneyDrunk.Vault.Providers.InMemory) - For testing
- [HoneyDrunk.Vault.Providers.AzureKeyVault](../HoneyDrunk.Vault.Providers.AzureKeyVault) - For production on Azure
- [HoneyDrunk.Vault.Providers.Aws](../HoneyDrunk.Vault.Providers.Aws) - For production on AWS

## License

MIT License - see LICENSE file for details.
