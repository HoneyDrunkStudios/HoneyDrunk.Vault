# Changelog - HoneyDrunk.Vault.Providers.InMemory

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.7.0] - 2026-05-27

### Changed (breaking)
- `InMemorySecretStore` now implements `ISecretProvider` directly (which extends `ISecretStore` per the 0.7.0 abstractions change). The `TryGetSecretAsync`, `FetchSecretAsync`, `TryFetchSecretAsync`, and `ListVersionsAsync` methods are now supplied as default interface methods on `ISecretStore` / `ISecretProvider` delegating to `SecretStoreFacade` — the redundant per-provider overrides have been removed. Callers that previously invoked these on the concrete class must now reach them through the interface (cast or DI).

### Changed
- `InMemorySecretStore.GetSecretAsync` / `ListSecretVersionsAsync` and `InMemoryConfigSource.GetConfigValueAsync` / `TryGetConfigValueAsync` now delegate to the new `DictionarySecretLookup` and `DictionaryConfigLookup` helpers in `HoneyDrunk.Vault`. Same observable behavior; same log shape; dramatically less duplication.

## [0.6.0] - 2026-05-26

### Changed
- Version alignment with the Vault Sonar gate-cleanup (ADR-0011 D11) release.
- Restored SDK-generated `AssemblyVersion` (removed `GenerateAssemblyInfo=false` and `CA1016` `NoWarn`).
- Trimmed `InMemoryConfigSource` inheritance list to `IConfigSourceProvider` only; that interface already extends `IConfigSource` (Sonar).
- Reordered `GetConfigValueAsync` / `TryGetConfigValueAsync` overloads to be adjacent (Sonar S4136).

## [0.5.0] - 2026-05-18

### Changed
- Aligned Vault package versions and Kernel references with `HoneyDrunk.Kernel`/`HoneyDrunk.Kernel.Abstractions` v0.7.0.
- Centralized provider bootstrap configuration resolution, secret-store facade wrappers, and config-source value conversion/orchestration to reduce duplicate provider helper logic.

## [0.4.0] - 2026-05-04

### Changed
- Version alignment with the ADR-0026 Vault tenancy support release in the core package. No functional provider changes.
## [0.3.0] - 2026-04-11

### Changed
- Package metadata and release documentation alignment update
- No functional provider changes in this release

## [0.2.0] - 2026-01-25

### Added
- `InMemoryConfigSource` now implements `IConfigSourceProvider` for full composite stack support
- `ProviderName` property returns `"in-memory"`
- `IsAvailable` property returns `true` (always available)
- `CheckHealthAsync()` method returns `true` (always healthy)

### Changed
- `InMemoryConfigSource` can now be registered via `AddConfigSourceProvider()` for use with composite stores and health contributors

## [0.1.0] - 2025-01-01

### Added
- InMemorySecretStore implementation
- InMemoryConfigSource implementation
- Support for runtime secret/config management
- Set, remove, and clear operations
- Grid context support for distributed tracing
- Health check implementation
- Thread-safe concurrent dictionary operations
- Comprehensive error handling

### Features
- In-memory storage with O(1) lookup
- Type conversion support
- Fluent configuration API
- Logging integration with correlation IDs
- Support for both secrets and configuration
