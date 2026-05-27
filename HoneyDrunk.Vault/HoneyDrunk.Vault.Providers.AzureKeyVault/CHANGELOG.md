# Changelog - HoneyDrunk.Vault.Providers.AzureKeyVault

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.7.0] - 2026-05-27

### Changed (breaking)
- `AzureKeyVaultSecretStore` now implements `ISecretProvider` directly (which extends `ISecretStore` per the 0.7.0 abstractions change). The `TryGetSecretAsync`, `FetchSecretAsync`, `TryFetchSecretAsync`, and `ListVersionsAsync` methods are now supplied as default interface methods on `ISecretStore` / `ISecretProvider` delegating to `SecretStoreFacade` — the redundant per-provider overrides have been removed. Callers that previously invoked these on the concrete store class must now reach them through the interface (cast or DI).

### Internal
- First test coverage added (`AzureKeyVaultConfigSourceTests`). Uses NSubstitute against `ISecretStore` and covers happy-path `GetConfigValueAsync`, key normalisation (`:`/`__`/`.` → `-`), `SecretNotFoundException` → `ConfigurationNotFoundException` wrapping, null/whitespace key guard, `TryGetConfigValueAsync` success/failure/swallow-exception paths, and ctor null-arg guards.

## [0.6.0] - 2026-05-26

### Changed
- Version alignment with the Vault Sonar gate-cleanup (ADR-0011 D11) release.
- Restored SDK-generated `AssemblyVersion` (removed `GenerateAssemblyInfo=false` and `CA1016` `NoWarn`).
- Routed `ex` through `LogWarning` in `AzureKeyVaultSecretStore.GetSecretAsync` + `ListSecretVersionsAsync` 404 paths and in `AzureKeyVaultConfigSource.GetConfigValueAsync` not-found path (Sonar S2486).
- Replaced the single-iteration `await foreach` in `AzureKeyVaultSecretStore.CheckHealthAsync` with an explicit one-step `IAsyncEnumerator` advance (Sonar bug).
- Reordered `GetConfigValueAsync` / `TryGetConfigValueAsync` overloads in `AzureKeyVaultConfigSource` and the nested `DelegatingConfigSourceProvider` in `AzureKeyVaultHoneyDrunkBuilderExtensions` (Sonar S4136).
- Bumped `Azure.Security.KeyVault.Secrets` to `4.11.0`.

## [0.5.0] - 2026-05-18

### Changed
- Aligned Vault package versions and Kernel references with `HoneyDrunk.Kernel`/`HoneyDrunk.Kernel.Abstractions` v0.7.0.
- Centralized provider bootstrap configuration resolution, secret-store facade wrappers, and config-source value conversion/orchestration to reduce duplicate provider helper logic.

## [0.4.0] - 2026-05-04

### Changed
- Version alignment with the ADR-0026 Vault tenancy support release in the core package. No functional provider changes.
## [0.3.0] - 2026-04-11

### Added
- Env-var bootstrap extension `AddVaultWithAzureKeyVaultBootstrap(IHoneyDrunkBuilder, Action<AzureKeyVaultBootstrapOptions>?)`.
- Bootstrap behavior: `AZURE_KEYVAULT_URI` + `DefaultAzureCredential`, Development fallback to `secrets/dev-secrets.json`, non-Development throw with clear guidance.

## [0.2.0] - 2026-01-25

### Changed
- Maintenance release aligned with core library version
- No functional changes

## [0.1.0] - 2025-01-01

### Added
- AzureKeyVaultSecretStore implementation
- Support for Managed Identity authentication
- Support for Service Principal authentication
- Secret version listing
- Secret retrieval with version specification
- Grid context support for distributed tracing
- Health check implementation
- Comprehensive error handling

### Features
- Azure SDK integration
- Credential chain support
- Automatic retry with exponential backoff
- Timeout configuration
- Logging integration with correlation IDs
- Batch secret operations
