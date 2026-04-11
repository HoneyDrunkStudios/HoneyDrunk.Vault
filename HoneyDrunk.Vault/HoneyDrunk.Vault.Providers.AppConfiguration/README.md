# HoneyDrunk.Vault.Providers.AppConfiguration

ADR-0005 bootstrap extensions for wiring Azure App Configuration from environment variables.

## Bootstrap settings

- `AZURE_APPCONFIG_ENDPOINT`
- `HONEYDRUNK_NODE_ID`

## Usage

```csharp
builder.AddAppConfiguration();
```
