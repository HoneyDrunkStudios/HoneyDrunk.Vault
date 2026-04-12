# HoneyDrunk.Vault - Repository Changelog

All notable changes to the HoneyDrunk.Vault repository will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

**Note:** See individual package CHANGELOGs for detailed changes:
- [HoneyDrunk.Vault CHANGELOG](HoneyDrunk.Vault/CHANGELOG.md)
- [HoneyDrunk.Vault.Providers.AzureKeyVault CHANGELOG](HoneyDrunk.Vault.Providers.AzureKeyVault/CHANGELOG.md)
- [HoneyDrunk.Vault.Providers.Aws CHANGELOG](HoneyDrunk.Vault.Providers.Aws/CHANGELOG.md)
- [HoneyDrunk.Vault.Providers.Configuration CHANGELOG](HoneyDrunk.Vault.Providers.Configuration/CHANGELOG.md)
- [HoneyDrunk.Vault.Providers.File CHANGELOG](HoneyDrunk.Vault.Providers.File/CHANGELOG.md)
- [HoneyDrunk.Vault.Providers.InMemory CHANGELOG](HoneyDrunk.Vault.Providers.InMemory/CHANGELOG.md)
- [HoneyDrunk.Vault.Providers.AppConfiguration CHANGELOG](HoneyDrunk.Vault.Providers.AppConfiguration/CHANGELOG.md)

---

## [0.3.0] - 2026-04-11

### Added

- Bootstrap extension surface across provider packages for env-var-driven Key Vault and App Configuration discovery
- `HoneyDrunk.Vault.Providers.AppConfiguration` package for Azure App Configuration integration
- `ISecretCacheInvalidator` and explicit `SecretCache` invalidation support for rotated secrets (ADR-0006 Tier 3)
- Optional `HoneyDrunk.Vault.EventGrid` webhook helpers for subscription validation and `SecretNewVersionCreated` cache invalidation

## [0.2.0] - 2026-01-25

### Added

- Architecture canary tests for enforcing Kernel context ownership invariants
- `CanaryInvariantException`, `KernelContextOwnershipInvariant`, `NoContextCreationInvariant`, `ProviderBoundaryInvariant`
- `VaultArchitectureCanaryTests` comprehensive test suite

### Changed

- Tests now use full composite stack with proper provider registration
- Improved documentation for provider slot architecture

## [0.1.0] - 2025-01-01

### Added

- Initial release of HoneyDrunk.Vault
- `ISecretStore` abstraction for secret access
- `IConfigProvider` abstraction for typed configuration access
- Multiple provider support with priority-based selection
- `VaultClient` for orchestrating secret retrieval across providers
- `SecretCache` for in-memory caching with configurable TTL
- Provider slot pattern: Azure Key Vault, AWS Secrets Manager, File, InMemory, Configuration providers
- Health, telemetry, and lifecycle integration with HoneyDrunk.Kernel
- Resilience options (retry and circuit breaker)

[0.3.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault/releases/tag/v0.3.0
[0.2.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault/releases/tag/v0.2.0
[0.1.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault/releases/tag/v0.1.0
