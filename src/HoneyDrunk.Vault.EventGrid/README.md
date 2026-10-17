# HoneyDrunk.Vault.EventGrid

Azure Event Grid webhook helpers for `HoneyDrunk.Vault`. This package wires Key Vault rotation events into `SecretCache` invalidation so running workloads refresh rotated secrets within seconds instead of waiting on TTL.

## Overview

This package is the transport glue for ADR-0006 Tier 3 rotation propagation. It is designed for hosts that already use `HoneyDrunk.Vault` and need to receive Azure Event Grid events raised by Azure Key Vault, validate the subscription handshake, authenticate inbound webhook requests, and invalidate cached secret names through `ISecretCacheInvalidator`.

**Features:**
- Parses `Microsoft.KeyVault.SecretNewVersionCreated`
- Accepts Event Grid schema and CloudEvents schema payloads
- Handles Event Grid subscription validation handshake
- Authenticates webhook requests with a shared secret resolved from `ISecretStore`
- Exposes ASP.NET Core endpoint mapping helpers
- Exposes a Functions-friendly handler wrapper
- Never logs secret values, only secret names

## Installation

```bash
dotnet add package HoneyDrunk.Vault
dotnet add package HoneyDrunk.Vault.EventGrid
```

## Prerequisites

- Azure Key Vault configured to emit Event Grid events
- An Azure Event Grid subscription targeting your application webhook
- `HoneyDrunk.Vault` configured with a provider capable of resolving `VaultInvalidationWebhookSecret`
- A host application using ASP.NET Core or Azure Functions

## Quick Start

### ASP.NET Core Minimal API

```csharp
using HoneyDrunk.Vault.EventGrid.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddVault(...);
builder.Services.AddVaultEventGridInvalidation();

var app = builder.Build();
app.MapVaultInvalidationWebhook("/internal/vault/invalidate");
app.Run();
```

### Azure Functions Friendly Wrapper

```csharp
public sealed class VaultInvalidationFunction
{
    private readonly VaultInvalidationFunctionHandler _handler;

    public VaultInvalidationFunction(VaultInvalidationFunctionHandler handler)
    {
        _handler = handler;
    }

    public Task<VaultInvalidationWebhookResponse> RunAsync(
        IReadOnlyDictionary<string, string?> headers,
        string body,
        CancellationToken cancellationToken)
    {
        return _handler.HandleAsync(headers, body, cancellationToken);
    }
}
```

## Authentication Model

Requests are authenticated by:

1. Event Grid origin and subscription validation flow
2. A shared secret header, `X-HoneyDrunk-Vault-Webhook-Secret`

The expected shared secret value is not hardcoded and is not read from an environment variable. It is resolved from `ISecretStore` using the secret name `VaultInvalidationWebhookSecret`.

## Event Handling

For `Microsoft.KeyVault.SecretNewVersionCreated`, the handler reads `objectName` from the event payload and calls `ISecretCacheInvalidator.Invalidate(secretName)`.

That means:
- the cache entry for that secret name is removed
- the next `ISecretStore.GetSecretAsync(new SecretIdentifier(name))` call fetches from the provider
- callers continue resolving the latest version without pinning, preserving invariant 21

## Example Event Shapes

### Event Grid Schema

```json
[
  {
    "eventType": "Microsoft.KeyVault.SecretNewVersionCreated",
    "data": {
      "objectName": "DbPassword"
    }
  }
]
```

### CloudEvents Schema

```json
[
  {
    "type": "Microsoft.KeyVault.SecretNewVersionCreated",
    "data": {
      "objectName": "DbPassword"
    }
  }
]
```

## Best Practices

1. Keep the webhook endpoint internal or network-restricted where possible.
2. Store `VaultInvalidationWebhookSecret` in the same secure Vault system as other application secrets.
3. Do not pin applications to a secret version when relying on cache invalidation.
4. Treat cache TTL as a fallback safety net, not the primary propagation mechanism.
5. Log only secret names and operational status, never secret values.

## Related Packages

- [HoneyDrunk.Vault](../HoneyDrunk.Vault)
- [HoneyDrunk.Vault.Providers.AzureKeyVault](../HoneyDrunk.Vault.Providers.AzureKeyVault)
- [HoneyDrunk.Vault.Providers.AppConfiguration](../HoneyDrunk.Vault.Providers.AppConfiguration)

## License

MIT License
