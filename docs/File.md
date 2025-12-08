# 📁 File Provider - Local Development

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Configuration](#configuration)
- [File Format](#file-format)
- [Features](#features)
- [FileSecretStore.cs](#filesecretstorecs)
- [FileConfigSource.cs](#fileconfigsourcecs)

---

## Overview

File-based secret and configuration provider designed for local development and testing. Stores secrets and configuration in JSON files on the local filesystem.

**Location:** `HoneyDrunk.Vault.Providers.File/`

**Use Cases:**
- Local development without cloud dependencies
- Testing with predictable secret values
- Debugging secret-related issues
- Demonstration and prototyping

**⚠️ Warning:** Not recommended for production use. Use Azure Key Vault or AWS Secrets Manager for production.

---

## Installation

```bash
dotnet add package HoneyDrunk.Vault.Providers.File
```

---

## Configuration

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

### FileVaultOptions

```csharp
public sealed class FileVaultOptions
{
    /// <summary>
    /// Path to the secrets file.
    /// Default: "secrets.json"
    /// </summary>
    public string SecretsFilePath { get; set; } = "secrets.json";

    /// <summary>
    /// Path to the configuration file.
    /// Default: "config.json"
    /// </summary>
    public string ConfigFilePath { get; set; } = "config.json";

    /// <summary>
    /// Environment variable containing the encryption key.
    /// </summary>
    public string? EncryptionKeyEnvironmentVariable { get; set; }

    /// <summary>
    /// File path containing the encryption key.
    /// </summary>
    public string? EncryptionKeyFilePath { get; set; }

    /// <summary>
    /// Whether to watch for file changes and reload.
    /// Default: false
    /// </summary>
    public bool WatchForChanges { get; set; }

    /// <summary>
    /// Whether to create empty files if they don't exist.
    /// Default: false
    /// </summary>
    public bool CreateIfNotExists { get; set; }
}
```

---

## File Format

### Secrets File (secrets.json)

```json
{
  "database-connection-string": "Server=localhost;Database=myapp;User Id=dev;Password=devpass;",
  "api-key": "dev-api-key-12345",
  "jwt-secret": "your-jwt-secret-for-development",
  "redis-password": "redis-local-password"
}
```

### Configuration File (config.json)

```json
{
  "logging:level": "Debug",
  "cache:ttl": "00:15:00",
  "feature:new-ui": "true",
  "max-connections": "100",
  "api:timeout-seconds": "30"
}
```

---

## Features

### File Watching

Automatically reloads secrets and configuration when files change:

```csharp
options.WatchForChanges = true;
```

This uses `FileSystemWatcher` to detect changes and reload the files.

### Automatic File Creation

Creates empty JSON files if they don't exist:

```csharp
options.CreateIfNotExists = true;
```

### Optional Encryption

Encrypt sensitive data using an environment variable:

```csharp
options.EncryptionKeyEnvironmentVariable = "MY_ENCRYPTION_KEY";
```

Or using a key file:

```csharp
options.EncryptionKeyFilePath = "/path/to/key.txt";
```

---

## FileSecretStore.cs

File-based implementation of `ISecretStore` and `ISecretProvider`.

```csharp
public sealed class FileSecretStore : ISecretStore, ISecretProvider, IDisposable
{
    public string ProviderName => "file";
    public bool IsAvailable => true;

    public Task<SecretValue> GetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public Task<VaultResult<SecretValue>> TryGetSecretAsync(
        SecretIdentifier identifier,
        CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
        string secretName,
        CancellationToken cancellationToken = default);
}
```

### Implementation Details

- Uses `ConcurrentDictionary` for thread-safe access
- Loads secrets from JSON file at startup
- Optionally watches for file changes
- Supports optional encryption for stored values

### Usage Example

```csharp
public class DatabaseService(ISecretStore secretStore)
{
    public async Task<SqlConnection> GetConnectionAsync(CancellationToken ct)
    {
        var secret = await secretStore.GetSecretAsync(
            new SecretIdentifier("database-connection-string"),
            ct);

        return new SqlConnection(secret.Value);
    }
}
```

---

## FileConfigSource.cs

File-based implementation of `IConfigSource`.

```csharp
public sealed class FileConfigSource : IConfigSource, IDisposable
{
    public Task<string> GetConfigValueAsync(
        string key,
        CancellationToken cancellationToken = default);

    public Task<string?> TryGetConfigValueAsync(
        string key,
        CancellationToken cancellationToken = default);

    public Task<T> GetConfigValueAsync<T>(
        string key,
        CancellationToken cancellationToken = default);

    public Task<T> TryGetConfigValueAsync<T>(
        string key,
        T defaultValue,
        CancellationToken cancellationToken = default);
}
```

### Usage Example

```csharp
public class FeatureService(IConfigProvider configProvider)
{
    public async Task<bool> IsNewUiEnabledAsync(CancellationToken ct)
    {
        return await configProvider.GetValueAsync<bool>(
            "feature:new-ui",
            defaultValue: false,
            ct);
    }
}
```

---

## Directory Structure

```
project/
├── secrets/
│   ├── dev-secrets.json
│   └── dev-config.json
├── appsettings.json
├── Program.cs
└── ...
```

### .gitignore

```gitignore
# Ignore local secrets
secrets/
*.secrets.json
*-secrets.json
```

---

## Summary

The File provider is ideal for local development:

| Feature | Supported |
|---------|-----------|
| Secrets | ✅ |
| Configuration | ✅ |
| File watching | ✅ |
| Encryption | ✅ |
| Versioning | ❌ |
| Production use | ❌ |

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
