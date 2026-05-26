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
- [HoneyDrunk.Vault.EventGrid CHANGELOG](HoneyDrunk.Vault.EventGrid/CHANGELOG.md)

---

## [Unreleased]

## [0.6.0] - 2026-05-26

### Changed (breaking)

- **`ConfigSourceFacade.TryGetValueAsync<T>` parameter order** updated to put `CancellationToken cancellationToken = default` last (after `ILogger? logger = null`) per .NET conventions and Roslyn `CA1068`. **Migration:** any direct caller using positional args `(tryGetValueAsync, key, defaultValue, cancellationToken, logger)` must swap the last two positions; in-repo provider callers (`AzureKeyVaultConfigSource`, `ConfigurationConfigSource`, `FileConfigSource`, `InMemoryConfigSource`, `CompositeConfigSource`) have been updated.
- **`VaultStartupHook.ExecuteAsync`** drops the default value on its `CancellationToken` parameter to match the `IStartupHook` interface (Sonar S1006). **Migration:** callers must pass `CancellationToken.None` (or any token) explicitly.
- **`InMemoryConfigSource`** inheritance list trimmed to `IConfigSourceProvider` only; `IConfigSourceProvider` already extends `IConfigSource` (Sonar). No runtime behavior change.
- **Package versions bumped** to `HoneyDrunk.Vault* 0.6.0` per pre-1.0 semver (`0.x.0 → 0.(x+1).0` for breaks).

### Internal

- Triaged the initial SonarQube Cloud findings against Vault (ADR-0011 D11 gate-cleanup). Re-enabled SDK-generated `AssemblyVersion` on all eight projects (removed `<GenerateAssemblyInfo>false</GenerateAssemblyInfo>` and the `CA1016` `NoWarn` entry) so assemblies now report a version derived from `<Version>`. Reordered `GetConfigValueAsync` / `TryGetConfigValueAsync` overloads to be adjacent (Sonar S4136) across `IConfigSource`, `IConfigProvider`, `IVaultClient`, `VaultClient`, `CompositeConfigSource`, `AzureKeyVaultConfigSource`, `ConfigurationConfigSource`, `FileConfigSource`, `InMemoryConfigSource`, and the `DelegatingConfigSourceProvider` nested type in the Azure Key Vault bootstrap extensions. Routed `ex` through `LogWarning`/`LogDebug` in catch blocks across `AzureKeyVaultConfigSource`, `AzureKeyVaultSecretStore` (404 paths in `GetSecretAsync` + `ListSecretVersionsAsync`), and `SecretStoreFacade.TryGetSecretAsync` (Sonar S2486). Refactored `VaultHealthContributor.CheckHealthAsync` and `VaultReadinessContributor.CheckReadinessAsync` to extract per-provider probe + result-summary helpers, projecting via `.Select(registered => registered.Provider)` and dropping cognitive complexity from 25/41 to under 15 (Sonar). Converted the AKV health check from a single-iteration `await foreach` to an explicit one-step `IAsyncEnumerator` advance (Sonar bug-finding). Switched `ConfigurationConfigSource.GetConfigValueAsync<T>` presence detection to `IConfigurationSection.Exists()` + `GetChildren().Any()` / `.Value` + `section.Get<T>()` so value-type defaults (`0` / `false` / `DateTime.MinValue`) and complex/object sections both bind correctly while genuinely-missing keys still throw (Sonar — value-type generics). Extracted the `"vault.result"` literal in `CompositeSecretStore` to a `ResultTag` const (Sonar S1192). Converted `TenantScopedSecretResolver` to a primary constructor (Sonar). Replaced the manual case-insensitive header foreach in `VaultInvalidationWebhookHandler` with `headers.FirstOrDefault(...)` (Sonar S3267 + S1751 — the original `foreach + return-on-first-match` was both a non-LINQ scan and a single-iteration loop). Dropped the dead `LogError` in `VaultTelemetry.ExecuteWithTelemetryAsync` catch block — the activity is still tagged with `Error` status and the original exception is rethrown for the caller to log (Sonar S2139). Pinned `static readonly string[] HitOrMiss` instead of passing a fresh array literal to `Assert.Contains` per call in `VaultTelemetryBehaviorTests`. Added explicit `Record.ExceptionAsync` assertions to the two previously assertion-free `VaultStartupHook` tests.
- Promoted the generic `IConfigSource.GetConfigValueAsync<T>` and `TryGetConfigValueAsync<T>` overloads to default interface methods that delegate to `ConfigSourceFacade` (with `logger: null` — the previous per-class overrides passed each provider's `_logger` for the warning on TryGetValue failures; this is the only behavioral nit). Removed the now-redundant overrides from `FileConfigSource`, `InMemoryConfigSource`, `CompositeConfigSource`, and `AzureKeyVaultConfigSource`. `ConfigurationConfigSource` keeps its overrides since it doesn't use the facade pattern. Drops the "Duplicated Lines on New Code" Sonar gate finding below the 3% threshold.
- Sort `HashSet<string>` provider buckets ordinally before `string.Join` in `VaultHealthContributor.Summarize` and `VaultReadinessContributor.Summarize` so health/readiness messages and log output are stable across runs.
- Bumped `HoneyDrunk.Kernel` / `HoneyDrunk.Kernel.Abstractions` `0.7.0 → 0.8.0` (Vault doesn't consume the breaking surface — static-class conversions on `AgentResultSerializer`/`GridContextSerializer`/`HttpContextMapper`/`JobContextMapper` and `GridContextSnapshot` ctor reorder). Refreshed `Microsoft.Extensions.Caching.Memory` / `Microsoft.Extensions.Options` / `Microsoft.Extensions.DependencyInjection` / `Microsoft.Extensions.Configuration.{Json,Abstractions,Binder}` to 10.0.8, `Microsoft.Extensions.Resilience` to 10.6.0, `Microsoft.FeatureManagement.AspNetCore` to 4.5.0, `Azure.Security.KeyVault.Secrets` to 4.11.0, and the AWS SDK packages (`AWSSDK.SecretsManager` 4.0.4.24, `AWSSDK.SSO` 4.0.2.31, `AWSSDK.SSOOIDC` 4.0.4).
- Onboarded Vault to SonarQube Cloud (ADR-0011 D11). Wired a `sonarcloud` job in `pr.yml` that calls `HoneyDrunkStudios/HoneyDrunk.Actions/.github/workflows/job-sonarcloud.yml` on both `pull_request` (after `pr-core` succeeds) and `push` to `main` (standalone). PR analysis gates the merge on new-code findings; main-branch analysis populates the SonarCloud Overview dashboard and the leak-period baseline. Per-project source/test classification is discovered automatically from MSBuild `IsTestProject` properties; per-repo Sonar overrides can be added later via `Directory.Build.props` `<SonarQubeSetting>` items or as new inputs to `job-sonarcloud.yml`. Branch-protection requirement added separately after the first successful run lands.
- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for Vault PRs.
- Migrated Vault tests from Moq to NSubstitute, adopted HoneyDrunk.Standards.Tests 0.2.9, and refreshed HoneyDrunk.Standards to 0.2.9 across package projects for ADR-0047 testing alignment.
- Backfilled Vault test coverage above the Grid PR coverage gate floor and seeded the coverage baseline ratchet artifact.

## [0.5.0] - 2026-05-18

### Changed
- Aligned Vault package versions and Kernel references with `HoneyDrunk.Kernel`/`HoneyDrunk.Kernel.Abstractions` v0.7.0.
- Centralized provider bootstrap configuration resolution, secret-store facade wrappers, and config-source value conversion/orchestration to reduce duplicate provider helper logic.

## [0.4.0] - 2026-05-04

### Added

- Documented ADR-0026 tenant-scoped Vault naming in `docs/Tenancy.md`.
- Added `TenantScopedSecretResolver` in `HoneyDrunk.Vault` for `tenant-{tenantId}-{secretName}` lookup with standard-path fallback for internal tenants and missing tenant secrets.
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

[0.5.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault/releases/tag/v0.5.0
[0.4.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault/releases/tag/v0.4.0
[0.3.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault/releases/tag/v0.3.0
[0.2.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault/releases/tag/v0.2.0
[0.1.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Vault/releases/tag/v0.1.0
