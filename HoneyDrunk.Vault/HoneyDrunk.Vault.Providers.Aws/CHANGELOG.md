# Changelog - HoneyDrunk.Vault.Providers.Aws

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.7.0] - 2026-05-27

### Changed (breaking)
- `AwsSecretsManagerSecretStore` now implements `ISecretProvider` directly (which extends `ISecretStore` per the 0.7.0 abstractions change). The `TryGetSecretAsync`, `FetchSecretAsync`, `TryFetchSecretAsync`, and `ListVersionsAsync` methods are now supplied as default interface methods on `ISecretStore` / `ISecretProvider` delegating to `SecretStoreFacade` — the redundant per-provider overrides have been removed. Callers that previously invoked these on the concrete store class must now reach them through the interface (cast or DI).

### Internal
- First test coverage added (`AwsSecretsManagerSecretStoreTests`). Uses NSubstitute against `IAmazonSecretsManager` and covers happy-path `GetSecretAsync`, `UseVersionId` toggle, `ResourceNotFoundException` → `SecretNotFoundException` wrapping, `AmazonSecretsManagerException` → `VaultOperationException` wrapping, secret-prefix application, `ListSecretVersionsAsync` pagination shape and 404 wrap, `CheckHealthAsync` success/failure, ctor null-options guard, and `Dispose` idempotence.

## [0.6.0] - 2026-05-26

### Changed
- Version alignment with the Vault Sonar gate-cleanup (ADR-0011 D11) release.
- Restored SDK-generated `AssemblyVersion` (removed `GenerateAssemblyInfo=false` and `CA1016` `NoWarn`).
- Bumped `AWSSDK.SecretsManager` to `4.0.4.24`, `AWSSDK.SSO` to `4.0.2.31`, and `AWSSDK.SSOOIDC` to `4.0.4`.

## [0.5.0] - 2026-05-18

### Changed
- Aligned Vault package versions and Kernel references with `HoneyDrunk.Kernel`/`HoneyDrunk.Kernel.Abstractions` v0.7.0.
- Centralized provider bootstrap configuration resolution, secret-store facade wrappers, and config-source value conversion/orchestration to reduce duplicate provider helper logic.

## [0.4.0] - 2026-05-04

### Changed
- Version alignment with the ADR-0026 Vault tenancy support release in the core package. No functional provider changes.
## [0.3.0] - 2026-04-11

### Changed
- Maintenance release aligned with core library version
- No functional provider changes in this release

## [0.2.0] - 2026-01-25

### Changed
- Maintenance release aligned with core library version
- No functional changes

## [0.1.0] - 2025-01-01

### Added
- AwsSecretsManagerSecretStore implementation
- Support for IAM role-based authentication
- Support for Access Key authentication
- Support for EC2 instance profile authentication
- Secret version listing and retrieval
- Secret prefix support for multi-tenant deployments
- Custom service URL support for private endpoints
- Grid context support for distributed tracing
- Health check implementation
- Comprehensive error handling and retry logic

### Features
- AWS SDK integration
- Credential chain support (IAM roles, access keys, instance profiles)
- Automatic retry with exponential backoff
- Timeout configuration
- Logging integration with correlation IDs
- Batch secret operations
