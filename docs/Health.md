# ❤️ Health - Monitoring and Health Checks

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [VaultHealthContributor.cs](#vaulthealthcontributorcs)
- [VaultReadinessContributor.cs](#vaultreadinesscontributorcs)

---

## Overview

Health monitoring integration for the vault system. Integrates with Kernel health aggregation for Kubernetes readiness/liveness probes.

**Location:** `HoneyDrunk.Vault/Health/`

Vault health contributors are registered automatically when using Kernel integration and are included in the node's composite health check.

---

## VaultHealthContributor.cs

Health contributor that reports vault operational status. Used for Kubernetes liveness probes.

```csharp
public sealed class VaultHealthContributor : IHealthContributor
{
    public string Name => "HoneyDrunk.Vault";
    public int Priority => 100;
    public bool IsCritical => true;
    
    public async Task<(HealthStatus status, string? message)> CheckHealthAsync(
        CancellationToken cancellationToken = default);
}
```

### Health Check Logic

```
CheckHealthAsync()
        │
        ▼
┌───────────────────────────────────┐
│ Is HealthCheckSecretKey set?      │
└───────────────┬───────────────────┘
                │
        ┌───────┴───────┐
        │               │
       Yes              No
        │               │
        ▼               ▼
┌───────────────┐ ┌───────────────────────┐
│ TryGetSecret  │ │ Return Healthy        │
│ (health key)  │ │ "Vault is operational"│
└───────┬───────┘ └───────────────────────┘
        │
   ┌────┴────┐
   │         │
Success   Failure
   │         │
   ▼         ▼
Healthy   Unhealthy
```

### Usage Example

```csharp
public sealed class VaultHealthContributor(
    ISecretStore secretStore,
    IOptions<VaultOptions> options,
    ILogger<VaultHealthContributor> logger) : IHealthContributor
{
    public string Name => "HoneyDrunk.Vault";
    public int Priority => 100;
    public bool IsCritical => true;

    public async Task<(HealthStatus status, string? message)> CheckHealthAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Performing vault health check");

        try
        {
            // If a health check secret key is configured, try to fetch it
            if (!string.IsNullOrEmpty(_options.HealthCheckSecretKey))
            {
                var result = await _secretStore.TryGetSecretAsync(
                    new SecretIdentifier(_options.HealthCheckSecretKey),
                    cancellationToken);

                if (result.IsSuccess)
                {
                    return (HealthStatus.Healthy, 
                        "Vault is operational and health check secret is accessible");
                }

                // Secret not found is still healthy (just not configured)
                return (HealthStatus.Healthy, 
                    "Vault is operational (health check secret not found)");
            }

            // No health check secret configured, just report as healthy
            return (HealthStatus.Healthy, "Vault is operational");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vault health check failed");
            return (HealthStatus.Unhealthy, $"Vault health check failed: {ex.Message}");
        }
    }
}
```

### Configuration

```csharp
builder.Services.AddVault(options =>
{
    // Optional: Configure a secret to verify on health check
    options.HealthCheckSecretKey = "health-check-secret";
});
```

[↑ Back to top](#table-of-contents)

---

## VaultReadinessContributor.cs

Readiness contributor that determines if the vault is ready to serve requests. Used for Kubernetes readiness probes.

```csharp
public sealed class VaultReadinessContributor : IReadinessContributor
{
    public string Name => "HoneyDrunk.Vault";
    public int Priority => 100;
    public bool IsRequired => true;
    
    public async Task<(bool isReady, string? message)> CheckReadinessAsync(
        CancellationToken cancellationToken = default);
}
```

### Readiness Check Logic

```
CheckReadinessAsync()
        │
        ▼
┌───────────────────────────────────┐
│ Count enabled providers           │
└───────────────┬───────────────────┘
                │
        ┌───────┴───────┐
        │               │
      > 0              = 0
        │               │
        ▼               ▼
┌───────────────┐ ┌───────────────────────┐
│ Check warmup  │ │ Return NotReady       │
│ keys loaded   │ │ "No providers enabled"│
└───────┬───────┘ └───────────────────────┘
        │
   ┌────┴────┐
   │         │
 Loaded   Failed
   │         │
   ▼         ▼
 Ready    NotReady
```

### Usage Example

```csharp
public sealed class VaultReadinessContributor(
    ISecretStore secretStore,
    IOptions<VaultOptions> options,
    ILogger<VaultReadinessContributor> logger) : IReadinessContributor
{
    public string Name => "HoneyDrunk.Vault";
    public int Priority => 100;
    public bool IsRequired => true;

    public async Task<(bool, string?)> CheckReadinessAsync(
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Performing vault readiness check");

        try
        {
            // Check that at least one provider is configured and enabled
            var enabledProviders = _options.Providers.Values.Count(p => p.IsEnabled);
            if (enabledProviders == 0)
            {
                _logger.LogWarning("No vault providers are enabled");
                return (false, "No vault providers are enabled");
            }

            // If warmup keys are configured, verify they were loaded
            if (_options.WarmupKeys.Count > 0 && 
                !string.IsNullOrEmpty(_options.HealthCheckSecretKey))
            {
                var result = await _secretStore.TryGetSecretAsync(
                    new SecretIdentifier(_options.HealthCheckSecretKey),
                    cancellationToken);

                if (!result.IsSuccess)
                {
                    return (false, "Warmup secrets not yet loaded");
                }
            }

            return (true, "Vault is ready");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vault readiness check failed");
            return (false, $"Vault readiness check failed: {ex.Message}");
        }
    }
}
```

### Kubernetes Integration

```yaml
# Kubernetes deployment using health probes
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
      - name: my-app
        livenessProbe:
          httpGet:
            path: /health/live    # Uses VaultHealthContributor
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health/ready   # Uses VaultReadinessContributor
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
```

[↑ Back to top](#table-of-contents)

---

## Summary

Health checks provide visibility into vault availability and readiness:

| Contributor | Probe Type | Checks |
|-------------|------------|--------|
| `VaultHealthContributor` | Liveness | Can reach provider, optional secret accessible |
| `VaultReadinessContributor` | Readiness | Providers enabled, warmup complete |

Both contributors integrate with Kernel's health aggregation model, allowing the vault status to be included in the node's overall health report.

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
