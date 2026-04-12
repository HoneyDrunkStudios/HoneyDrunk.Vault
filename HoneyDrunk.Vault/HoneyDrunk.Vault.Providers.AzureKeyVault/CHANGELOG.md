# Changelog - HoneyDrunk.Vault.Providers.AzureKeyVault

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-04-11

### Added
- Env-var bootstrap extension `AddVaultWithAzureKeyVaultBootstrap(IHoneyDrunkBuilder, Action<AzureKeyVaultBootstrapOptions>?)`.
- Bootstrap behavior: `AZURE_KEYVAULT_URI` + `DefaultAzureCredential`, Development fallback to `secrets/dev-secrets.json`, non-Development throw with clear guidance.

## [0.2.0] - 2026-01-25

### Changed
- Maintenance release aligned with core library version
- No functional changes

## [0.1.0] - 2025-01-01

### Added
- AzureKeyVaultSecretStore implementation
- Support for Managed Identity authentication
- Support for Service Principal authentication
- Secret version listing
- Secret retrieval with version specification
- Grid context support for distributed tracing
- Health check implementation
- Comprehensive error handling

### Features
- Azure SDK integration
- Credential chain support
- Automatic retry with exponential backoff
- Timeout configuration
- Logging integration with correlation IDs
- Batch secret operations
