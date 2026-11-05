# Changelog - HoneyDrunk.Vault.EventGrid

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
- Version alignment with the HoneyDrunk.Vault 0.8.0 release. No Event Grid-specific behavior change.

## [0.7.0] - 2026-05-27

### Changed
- Version alignment with the HoneyDrunk.Vault 0.7.0 dedupe/coverage release (ADR-0011 D11).

### Internal
- First test coverage added for `VaultInvalidationFunctionHandler` (null-handler ctor guard, null-headers ctor guard, and a forwarding smoke test that proves the `IReadOnlyDictionary<string, string?>` headers bridge into the webhook handler).
- First test coverage added for `VaultEventGridServiceCollectionExtensions.AddVaultEventGridInvalidation` (resolves both handlers + null-services guard).

## [0.6.0] - 2026-05-26

### Changed
- Version alignment with the Vault Sonar gate-cleanup (ADR-0011 D11) release.
- Replaced the manual case-insensitive header `foreach` in `VaultInvalidationWebhookHandler.TryGetHeader` with `headers.FirstOrDefault(...)` — satisfies Sonar S3267 (use LINQ) and S1751 (the previous `foreach + return-on-first-match` was a single-iteration loop). The `KeyValuePair<string, string?>` default struct (`Key == null`) acts as the not-found sentinel since dictionary keys cannot legitimately be null.
- Restored SDK-generated `AssemblyVersion` (removed `GenerateAssemblyInfo=false` and `CA1016` `NoWarn`).

## [0.5.0] - 2026-05-18

### Changed
- Version alignment with the Vault Kernel v0.7.0 adoption and provider-helper consolidation release. No functional EventGrid changes.

## [0.4.0] - 2026-05-04

### Changed
- Version alignment with the ADR-0026 Vault tenancy support release in the core package. No functional provider changes.
## [0.3.0] - 2026-04-12

### Added
- Initial `HoneyDrunk.Vault.EventGrid` package for ADR-0006 Tier 3 rotation propagation.
- `VaultInvalidationWebhookHandler` for Event Grid subscription validation, webhook authentication, and cache invalidation.
- `VaultInvalidationFunctionHandler` wrapper for Function App hosts.
- `AddVaultEventGridInvalidation(IServiceCollection)` registration helper.
- `MapVaultInvalidationWebhook(IEndpointRouteBuilder, string)` endpoint helper for ASP.NET Core hosts.
- Support for Event Grid schema and CloudEvents schema payloads for `Microsoft.KeyVault.SecretNewVersionCreated`.
