# Changelog - HoneyDrunk.Vault.Providers.InMemory

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-04-11

### Changed
- Package metadata and release documentation alignment update
- No functional provider changes in this release

## [0.2.0] - 2026-01-25

### Added
- `InMemoryConfigSource` now implements `IConfigSourceProvider` for full composite stack support
- `ProviderName` property returns `"in-memory"`
- `IsAvailable` property returns `true` (always available)
- `CheckHealthAsync()` method returns `true` (always healthy)

### Changed
- `InMemoryConfigSource` can now be registered via `AddConfigSourceProvider()` for use with composite stores and health contributors

## [0.1.0] - 2025-01-01

### Added
- InMemorySecretStore implementation
- InMemoryConfigSource implementation
- Support for runtime secret/config management
- Set, remove, and clear operations
- Grid context support for distributed tracing
- Health check implementation
- Thread-safe concurrent dictionary operations
- Comprehensive error handling

### Features
- In-memory storage with O(1) lookup
- Type conversion support
- Fluent configuration API
- Logging integration with correlation IDs
- Support for both secrets and configuration
