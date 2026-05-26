# Changelog - HoneyDrunk.Vault.Providers.File

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.6.0] - 2026-05-26

### Changed
- Version alignment with the Vault Sonar gate-cleanup (ADR-0011 D11) release.
- Restored SDK-generated `AssemblyVersion` (removed `GenerateAssemblyInfo=false` and `CA1016` `NoWarn`).
- Reordered `GetConfigValueAsync` / `TryGetConfigValueAsync` overloads in `FileConfigSource` to be adjacent (Sonar S4136).

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
- FileSecretStore implementation for file-based secrets
- FileConfigSource implementation for file-based configuration
- Support for automatic file watching and reloading
- File encryption support via environment variables or file paths
- Automatic file creation option
- Grid context support for distributed tracing
- Health check implementation
- Logging integration with correlation IDs

### Features
- JSON file parsing for secrets and configuration
- Case-insensitive key lookup
- Type conversion for configuration values
- File system monitoring with debouncing
- Development-focused error messages
- Extension methods for dependency injection
