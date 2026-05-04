# Tenancy

ADR-0026 defines tenant-scoped Vault lookup as a naming convention plus a thin resolver, not an `ISecretStore` contract change.

## Secret naming

Tenant-owned secrets use this pattern inside the node's standard vault:

```text
tenant-{tenantId}-{secretName}
```

Examples:

- `tenant-01H2X3Y4Z5ABCDEABCDEABCDEF-resend-api-key`
- `tenant-01H2X3Y4Z5ABCDEABCDEABCDEF-twilio-auth-token`
- `resend-api-key` — the node-standard shared path, with no tenant prefix

`tenantId` is the canonical ULID string form of `HoneyDrunk.Kernel.Abstractions.Identity.TenantId`.

## Resolution behavior

Use `TenantScopedSecretResolver` from `HoneyDrunk.Vault.Services` when a node supports tenant-specific secrets:

1. If `tenantId.IsInternal` is true, resolve `secretName` directly from the node-standard path.
2. Otherwise, try `tenant-{tenantId}-{secretName}` first.
3. If the tenant-scoped secret is absent, fall back to `secretName`.

This keeps internal Grid callers on the existing secret path and lets Free/Starter tenants share node-managed provider keys unless they bring their own tenant-scoped secret.

```csharp
var resolver = new TenantScopedSecretResolver(secretStore);
var resendKey = await resolver.ResolveAsync(tenantId, "resend-api-key", cancellationToken);
```

Do not add tenant overloads to `ISecretStore`; tenancy is an opt-in usage pattern layered on top of the existing secret-store abstraction.
