# Changelog - HoneyDrunk.Vault

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.6.0] - 2026-05-26

### Changed (breaking)
- `ConfigSourceFacade.TryGetValueAsync<T>` parameter order: `ILogger? logger = null` now precedes `CancellationToken cancellationToken = default` (CA1068). All in-repo callers updated.
- `VaultStartupHook.ExecuteAsync` drops the default value on its `CancellationToken` parameter to match the `IStartupHook` interface (Sonar S1006).

### Changed
- Restored SDK-generated `AssemblyVersion` (removed `GenerateAssemblyInfo=false` and `CA1016` `NoWarn`). Assemblies now report a version derived from `<Version>`.
- Reordered `GetConfigValueAsync` / `TryGetConfigValueAsync` overloads to be adjacent across `IConfigSource`, `IConfigProvider`, `IVaultClient`, `VaultClient`, and `CompositeConfigSource` (Sonar S4136).
- Routed `ex` through `LogDebug` in `SecretStoreFacade.TryGetSecretAsync` 404 path (Sonar S2486).
- Refactored `VaultHealthContributor.CheckHealthAsync` and `VaultReadinessContributor.CheckReadinessAsync` to extract per-provider probe and summary helpers; cognitive complexity 25/41 → under 15.
- Switched `ConfigurationConfigSource.GetConfigValueAsync<T>` null-check to `EqualityComparer<T>.Default.Equals(value!, default!)` (Sonar — value-type generics).
- Extracted the `"vault.result"` literal in `CompositeSecretStore` to a `ResultTag` const (Sonar S1192).
- Converted `TenantScopedSecretResolver` to a primary constructor.
- Removed the dead `LogError` in `VaultTelemetry.ExecuteWithTelemetryAsync` catch block; the activity is still tagged `Error` and the exception rethrown for the caller to log (Sonar S2139).
- Bumped `HoneyDrunk.Kernel` / `HoneyDrunk.Kernel.Abstractions` `0.7.0 → 0.8.0` (Vault doesn't consume the breaking surface).
- Refreshed `Microsoft.Extensions.{Caching.Memory, Options}` to `10.0.8` and `Microsoft.Extensions.Resilience` to `10.6.0`.

## [0.5.0] - 2026-05-18

### Changed
- Aligned Vault package versions and Kernel references with `HoneyDrunk.Kernel`/`HoneyDrunk.Kernel.Abstractions` v0.7.0.
- Centralized provider bootstrap configuration resolution, secret-store facade wrappers, and config-source value conversion/orchestration to reduce duplicate provider helper logic.

## [0.4.0] - 2026-05-04

### Added
- Added `TenantScopedSecretResolver` for ADR-0026 tenant-aware secret lookup using `tenant-{tenantId}-{secretName}` with shared-path fallback.
- Added tenancy documentation covering the per-tenant secret scoping pattern and internal-tenant behavior.
## [0.3.0] - 2026-04-11

### Added
- Added bootstrap extension surface across provider packages for env-var-driven Key Vault and App Configuration discovery.
- Added `HoneyDrunk.Vault.Providers.AppConfiguration` package integration to the solution and tests.
- Added `ISecretCacheInvalidator` and explicit `SecretCache` invalidation support so rotated secrets can propagate independently of TTL per ADR-0006 Tier 3.
- Added optional `HoneyDrunk.Vault.EventGrid` webhook helpers for Event Grid subscription validation, shared-secret auth, and `SecretNewVersionCreated` cache invalidation.

## [0.2.0] - 2026-01-25

### Added
- Architecture canary tests for enforcing Kernel context ownership invariants
- `CanaryInvariantException` for reporting invariant violations
- `KernelContextOwnershipInvariant` - validates context stability during Vault operations
- `NoContextCreationInvariant` - IL scanning to detect forbidden context construction
- `ProviderBoundaryInvariant` - validates provider assemblies have no Kernel dependencies
- `VaultArchitectureCanaryTests` - comprehensive test suite for architecture validation

### Changed
- Tests now use full composite stack with proper provider registration
- Improved documentation for provider slot architecture

## [0.1.0] - 2025-01-01

### Added
- Initial release of HoneyDrunk.Vault core library
- ISecretStore abstraction for secret access
- IConfigProvider abstraction for typed configuration access
- Support for multiple provider configurations with prioritization
- VaultClient for orchestrating secret retrieval across providers
- SecretCache for in-memory caching with configurable TTL
- VaultHealthContributor for health check integration
- VaultTelemetry for distributed tracing without leaking secrets
- VaultStartupHook for configuration validation and cache warming
- ProviderRegistration for flexible provider configuration
- Resilience options (retry and circuit breaker)
- Caching options (TTL, max size, sliding expiration)
- Full Kernel integration (lifecycle, health, telemetry)

### Configuration
- VaultOptions for configuring vault system
- VaultCacheOptions for cache behavior
- VaultResilienceOptions for retry and circuit breaker policies
- ProviderRegistration with priority-based selection

### Interfaces
- ISecretStore - Secret access interface
- ISecretProvider - Provider implementation interface
- IConfigSource - Configuration source interface
- IConfigProvider - Typed configuration provider

### Exceptions
- SecretNotFoundException - Thrown when secret is not found
- ConfigurationNotFoundException - Thrown when configuration value is not found
- VaultOperationException - Thrown for vault operation errors
