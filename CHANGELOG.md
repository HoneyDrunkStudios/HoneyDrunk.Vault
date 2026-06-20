# Changelog

All notable changes to the HoneyDrunk.Vault repository are summarized here. The
format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and
the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

This is the repository-level summary. For the full, detailed solution changelog
and per-package CHANGELOGs, see
[HoneyDrunk.Vault/CHANGELOG.md](HoneyDrunk.Vault/CHANGELOG.md). All Vault packages
are versioned in lockstep.

## Unreleased

## 0.8.0 - App Configuration host bootstrap

### Added

- `HoneyDrunk.Vault.Providers.AppConfiguration`: `AddAppConfiguration(IConfigurationManager)` overload so Azure Functions and generic-host consumers can bootstrap App Configuration without a workaround `IConfiguration` registration.

### Changed

- All Vault packages aligned to 0.8.0 in lockstep; only the AppConfiguration provider changes behavior, the rest are alignment bumps.

## 0.7.0 - Provider interface consolidation

### Changed

- Breaking: `ISecretProvider` now extends `ISecretStore`, exposing `FetchSecretAsync` / `TryFetchSecretAsync` / `ListVersionsAsync` as default interface methods; per-provider overrides removed. Callers must reach these methods through the interface.
- Added `DictionarySecretLookup` / `DictionaryConfigLookup` helpers to consolidate the dictionary-backed validate/lookup/throw pattern shared by the InMemory and File providers.
- `AddVaultCore` resolves `CompositeSecretStore` via a factory so optional telemetry binds to `null` in the standalone provider DI surface.

## 0.6.0 - Convention and Sonar cleanup

### Changed

- Breaking: `ConfigSourceFacade.TryGetValueAsync<T>` parameter order updated to put `CancellationToken` last per .NET conventions; `VaultStartupHook.ExecuteAsync` drops its default `CancellationToken`.
- Onboarded Vault to SonarQube Cloud (ADR-0011) and triaged the initial findings across config sources, secret stores, health/readiness contributors, and telemetry.
- Re-enabled SDK-generated `AssemblyVersion` across all projects.

## 0.5.0 - Kernel 0.7.0 alignment

### Changed

- Aligned Vault package versions and Kernel references with `HoneyDrunk.Kernel` / `HoneyDrunk.Kernel.Abstractions` v0.7.0.
- Centralized provider bootstrap configuration resolution, secret-store facade wrappers, and config-source value conversion to reduce duplicate provider helper logic.

## 0.4.0 - Tenant-scoped secret resolution

### Added

- `TenantScopedSecretResolver` in `HoneyDrunk.Vault` for `tenant-{tenantId}-{secretName}` lookup with standard-path fallback for internal tenants and missing tenant secrets (ADR-0026).
- Documented ADR-0026 tenant-scoped Vault naming in `docs/Tenancy.md`.

## 0.3.0 - Bootstrap surface and Event Grid invalidation

### Added

- Bootstrap extension surface across provider packages for env-var-driven Key Vault and App Configuration discovery.
- `HoneyDrunk.Vault.Providers.AppConfiguration` package for Azure App Configuration integration.
- `ISecretCacheInvalidator` and explicit `SecretCache` invalidation support for rotated secrets (ADR-0006).
- Optional `HoneyDrunk.Vault.EventGrid` webhook helpers for subscription validation and `SecretNewVersionCreated` cache invalidation.

## 0.2.0 - Architecture canary invariants

### Added

- Architecture canary tests enforcing Kernel context ownership invariants.
- `CanaryInvariantException`, `KernelContextOwnershipInvariant`, `NoContextCreationInvariant`, `ProviderBoundaryInvariant`, and the `VaultArchitectureCanaryTests` suite.

## 0.1.0 - Initial release

### Added

- Initial release of HoneyDrunk.Vault.
- `ISecretStore` abstraction for secret access and `IConfigProvider` for typed configuration access.
- Multiple provider support with priority-based selection: Azure Key Vault, AWS Secrets Manager, File, InMemory, Configuration.
- `VaultClient` orchestrator and `SecretCache` in-memory caching with configurable TTL.
- Health, telemetry, and lifecycle integration with HoneyDrunk.Kernel, plus retry and circuit breaker resilience options.
