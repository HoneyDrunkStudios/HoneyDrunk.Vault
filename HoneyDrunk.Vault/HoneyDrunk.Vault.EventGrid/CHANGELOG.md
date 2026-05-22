# Changelog - HoneyDrunk.Vault.EventGrid

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

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
