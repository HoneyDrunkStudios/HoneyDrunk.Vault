# Changelog - HoneyDrunk.Vault

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-01-25

### Added
- Added ADR-0005 bootstrap extension surface across provider packages for env-var-driven Key Vault and App Configuration discovery.
- Added `HoneyDrunk.Vault.Providers.AppConfiguration` package integration to the solution and tests.
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
