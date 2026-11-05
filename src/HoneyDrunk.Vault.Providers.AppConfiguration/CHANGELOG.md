# Changelog - HoneyDrunk.Vault.Providers.AppConfiguration

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

### Added
- `AddAppConfiguration(IHoneyDrunkBuilder, IConfigurationManager, Action<AppConfigurationOptions>?)` overload that takes the host builder's `IConfigurationManager` directly. Hosts that do not register their `ConfigurationManager` as an `IConfiguration` instance on the service collection — Azure Functions (`FunctionsApplication.CreateBuilder`) and the generic `Host.CreateApplicationBuilder` — must use this overload (pass `builder.Configuration`); the parameterless overload's service-collection lookup falls back to an immutable env-vars-only configuration on those hosts and cannot append the App Configuration source, aborting the worker at startup.

### Changed
- The parameterless `AddAppConfiguration` overload now throws an actionable error naming the new overload when no mutable `IConfigurationManager` is registered. `WebApplication.CreateBuilder` consumers are unaffected.

## [0.7.0] - 2026-05-27

### Changed
- Version alignment with the HoneyDrunk.Vault 0.7.0 dedupe/coverage release (ADR-0011 D11). No App Configuration-specific behavior change.

## [0.6.0] - 2026-05-26

### Changed
- Version alignment with the Vault Sonar gate-cleanup (ADR-0011 D11) release.
- Restored SDK-generated `AssemblyVersion` (removed `GenerateAssemblyInfo=false` and `CA1016` `NoWarn`).
- Bumped `Microsoft.Extensions.Configuration.Json` to `10.0.8` and `Microsoft.FeatureManagement.AspNetCore` to `4.5.0`.

## [0.5.0] - 2026-05-18

### Changed
- Aligned Vault package versions and Kernel references with `HoneyDrunk.Kernel`/`HoneyDrunk.Kernel.Abstractions` v0.7.0.
- Centralized provider bootstrap configuration resolution, secret-store facade wrappers, and config-source value conversion/orchestration to reduce duplicate provider helper logic.

## [0.4.0] - 2026-05-04

### Changed
- Version alignment with the ADR-0026 Vault tenancy support release in the core package. No functional provider changes.
## [0.3.0] - 2026-04-11

### Added
- Initial provider package.
- `AddAppConfiguration(IHoneyDrunkBuilder, Action<AppConfigurationOptions>?)` bootstrap extension.
- Azure App Configuration registration via `AZURE_APPCONFIG_ENDPOINT` and `HONEYDRUNK_NODE_ID`.
- Development fallback to `appsettings.Development.json` when endpoint is absent.
- Feature management (`IFeatureManager`) service registration.
