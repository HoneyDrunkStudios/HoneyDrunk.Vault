# Changelog

## [0.8.1] - 2026-09-26

### Changed

- Refresh stable NuGet dependencies; preserve target frameworks and HoneyDrunk public contracts.

| Dependency | Previous | Updated |
| --- | --- | --- |
| AWSSDK.SSO | 4.0.2.31 | 4.0.100.16 |
| AWSSDK.SSOOIDC | 4.0.4 | 4.0.100.15 |
| AWSSDK.SecretsManager | 4.0.4.24 | 4.0.100.14 |
| Azure.Security.KeyVault.Secrets | 4.11.0 | 4.11.1 |
| Microsoft.Azure.AppConfiguration.AspNetCore | 8.5.0 | 8.6.0 |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.201 | 10.0.401 |
| Microsoft.Extensions.Caching.Memory | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.Configuration.AzureAppConfiguration | 8.5.0 | 8.6.0 |
| Microsoft.Extensions.Configuration.Binder | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.Configuration.Json | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.DependencyInjection | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.Options | 10.0.8 | 10.0.12 |
| Microsoft.Extensions.Resilience | 10.6.0 | 10.10.0 |
| Microsoft.FeatureManagement.AspNetCore | 4.5.0 | 4.8.0 |


All notable changes to the HoneyDrunk.Vault repository are summarized here. The
format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and
the project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

This is the repository-level summary. For the full, detailed repository changelog
and per-package CHANGELOGs, see
[HoneyDrunk.Vault/CHANGELOG.md](HoneyDrunk.Vault/CHANGELOG.md). All Vault packages
are versioned in lockstep.


### Verified HoneyDrunk dependencies

- HoneyDrunk.Kernel: 0.8.0 -> 0.8.1 (verified on NuGet.org).
- HoneyDrunk.Kernel.Abstractions: 0.8.0 -> 0.8.1 (verified on NuGet.org).
- HoneyDrunk.Standards: 0.2.9 -> 0.3.0 (verified on NuGet.org).
- HoneyDrunk.Standards.Tests: 0.2.9 -> 0.3.0 (verified on NuGet.org).

## [Unreleased]

## [0.8.0] - 2026-06-04

### Added

- `HoneyDrunk.Vault.Providers.AppConfiguration`: `AddAppConfiguration(IConfigurationManager)` overload so Azure Functions and generic-host consumers can bootstrap App Configuration without a workaround `IConfiguration` registration.

### Changed

- All Vault packages aligned to 0.8.0 in lockstep; only the AppConfiguration provider changes behavior, the rest are alignment bumps.

## [0.7.0] - 2026-05-27

### Changed

- Breaking: `ISecretProvider` now extends `ISecretStore`, exposing `FetchSecretAsync` / `TryFetchSecretAsync` / `ListVersionsAsync` as default interface methods; per-provider overrides removed. Callers must reach these methods through the interface.
- Introduced `DictionarySecretLookup` / `DictionaryConfigLookup` helpers that consolidate the dictionary-backed validate/lookup/throw pattern shared by the InMemory and File providers.
- `AddVaultCore` resolves `CompositeSecretStore` via a factory so optional telemetry binds to `null` in the standalone provider DI surface.

## [0.6.0] - 2026-05-26

### Changed

- Breaking: `ConfigSourceFacade.TryGetValueAsync<T>` parameter order updated to put `CancellationToken` last per .NET conventions; `VaultStartupHook.ExecuteAsync` drops its default `CancellationToken`.
- Onboarded Vault to SonarQube Cloud (ADR-0011) and triaged the initial findings across config sources, secret stores, health/readiness contributors, and telemetry.
- Re-enabled SDK-generated `AssemblyVersion` across all projects.

## [0.5.0] - 2026-05-18

### Changed

- Aligned Vault package versions and Kernel references with `HoneyDrunk.Kernel` / `HoneyDrunk.Kernel.Abstractions` v0.7.0.
- Centralized provider bootstrap configuration resolution, secret-store facade wrappers, and config-source value conversion to reduce duplicate provider helper logic.

## [0.4.0] - 2026-05-04

### Added

- `TenantScopedSecretResolver` in `HoneyDrunk.Vault` for `tenant-{tenantId}-{secretName}` lookup with standard-path fallback for internal tenants and missing tenant secrets (ADR-0026).
- Documented ADR-0026 tenant-scoped Vault naming in `docs/Tenancy.md`.

## [0.3.0] - 2026-04-11

### Added

- Bootstrap extension surface across provider packages for env-var-driven Key Vault and App Configuration discovery.
- `HoneyDrunk.Vault.Providers.AppConfiguration` package for Azure App Configuration integration.
- `ISecretCacheInvalidator` and explicit `SecretCache` invalidation support for rotated secrets (ADR-0006).
- Optional `HoneyDrunk.Vault.EventGrid` webhook helpers for subscription validation and `SecretNewVersionCreated` cache invalidation.

## [0.2.0] - 2026-01-25

### Added

- Architecture canary tests enforcing Kernel context ownership invariants.
- `CanaryInvariantException`, `KernelContextOwnershipInvariant`, `NoContextCreationInvariant`, `ProviderBoundaryInvariant`, and the `VaultArchitectureCanaryTests` suite.

## [0.1.0] - 2025-01-01

### Added

- Initial release of HoneyDrunk.Vault.
- `ISecretStore` abstraction for secret access and `IConfigProvider` for typed configuration access.
- Multiple provider support with priority-based selection: Azure Key Vault, AWS Secrets Manager, File, InMemory, Configuration.
- `VaultClient` orchestrator and `SecretCache` in-memory caching with configurable TTL.
- Health, telemetry, and lifecycle integration with HoneyDrunk.Kernel, plus retry and circuit breaker resilience options.
