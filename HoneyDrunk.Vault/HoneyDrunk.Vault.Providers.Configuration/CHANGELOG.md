# Changelog - HoneyDrunk.Vault.Providers.Configuration

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.7.0] - 2026-05-27

### Changed (breaking)
- `ConfigurationSecretStore` drops the redundant `TryGetSecretAsync` override now that `ISecretStore` supplies it as a default interface method delegating to `SecretStoreFacade.TryGetSecretAsync(identifier, GetSecretAsync, logger: null, …)`. The store remains `ISecretStore`-only (it is intentionally not part of the composite provider chain). Callers that previously invoked `TryGetSecretAsync` on the concrete class must now reach it through the `ISecretStore` interface.

## [0.6.0] - 2026-05-26

### Changed
- Version alignment with the Vault Sonar gate-cleanup (ADR-0011 D11) release.
- Restored SDK-generated `AssemblyVersion` (removed `GenerateAssemblyInfo=false` and `CA1016` `NoWarn`).
- Switched `ConfigurationConfigSource.GetConfigValueAsync<T>` to detect presence via `IConfigurationSection.Exists()` plus a `GetChildren().Any()` / `.Value` check, then `section.Get<T>()` for binding. The previous `value == null` check (Sonar's original complaint) was wrong for value types; a `default(T)` comparison would have over-rotated the other way and treated legitimate configured `0` / `false` / `DateTime.MinValue` as "not found"; relying on `Exists()` alone would reject complex/object sections whose `Value` is null but whose children bind. The current shape covers all three: value-type defaults, complex sections, and genuinely-missing keys. Tests cover the value-type-default cases.
- Reordered `GetConfigValueAsync` / `TryGetConfigValueAsync` overloads to be adjacent (Sonar S4136).
- Bumped `Microsoft.Extensions.Configuration.Abstractions` and `Microsoft.Extensions.Configuration.Binder` to `10.0.8`.

## [0.5.0] - 2026-05-18

### Changed
- Aligned Vault package versions and Kernel references with `HoneyDrunk.Kernel`/`HoneyDrunk.Kernel.Abstractions` v0.7.0.
- Centralized provider bootstrap configuration resolution, secret-store facade wrappers, and config-source value conversion/orchestration to reduce duplicate provider helper logic.

## [0.4.0] - 2026-05-04

### Changed
- Version alignment with the ADR-0026 Vault tenancy support release in the core package. No functional provider changes.
## [0.3.0] - 2026-04-11

### Changed
- Maintenance release aligned with core library version and provider packaging metadata
- No functional provider changes in this release

## [0.2.0] - 2026-01-25

### Changed
- Maintenance release aligned with core library version
- No functional changes

## [0.1.0] - 2025-01-01

### Added
- ConfigurationSecretStore implementation
- IConfiguration integration
- Support for multiple configuration sources
- Configuration prefix support
- Configuration reloading support
- Environment variable override support
- User secrets integration
- Grid context support for distributed tracing
- Health check implementation
- Comprehensive error handling

### Features
- Seamless .NET configuration integration
- Hierarchical configuration support
- Configuration validation
- Type conversion support
- Logging integration with correlation IDs
