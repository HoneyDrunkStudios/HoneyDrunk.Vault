# Changelog - HoneyDrunk.Vault.Providers.Aws

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.2] - 2026-11-05

### Changed

- Refresh NuGet dependencies and adopt upstream package releases in dependency order. Preserve target frameworks and public contracts.

| Dependency | Previous | Updated |
| --- | --- | --- |
| AWSSDK.SSO | 4.0.100.16 | 4.0.100.25 |
| AWSSDK.SSOOIDC | 4.0.100.15 | 4.0.100.24 |
| AWSSDK.SecretsManager | 4.0.100.14 | 4.0.100.24 |
| Azure.Security.KeyVault.Secrets | 4.11.1 | 4.12.0 |
| HoneyDrunk.Kernel | 0.8.1 | 0.8.2 |
| HoneyDrunk.Kernel.Abstractions | 0.8.1 | 0.8.2 |
| HoneyDrunk.Standards | 0.3.0 | 0.4.3 |
| HoneyDrunk.Standards.Tests | 0.3.0 | 0.4.3 |
| Microsoft.Azure.AppConfiguration.AspNetCore | 8.6.0 | 8.6.1 |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.401 | 10.0.403 |
| Microsoft.Extensions.Caching.Memory | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Configuration.Abstractions | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Configuration.AzureAppConfiguration | 8.6.0 | 8.6.1 |
| Microsoft.Extensions.Configuration.Binder | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Configuration.Json | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.DependencyInjection | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Options | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Resilience | 10.10.0 | 10.11.0 |

## [0.8.1] - 2026-09-26

### Changed

- Refresh dependency and shared build-tooling versions; preserve target frameworks and existing public contracts. See the [repository dependency changes](../../CHANGELOG.md).

## [Unreleased]

## [0.8.0] - 2026-06-04

### Changed
- Version alignment with the HoneyDrunk.Vault 0.8.0 release. No AWS provider-specific behavior change.

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
