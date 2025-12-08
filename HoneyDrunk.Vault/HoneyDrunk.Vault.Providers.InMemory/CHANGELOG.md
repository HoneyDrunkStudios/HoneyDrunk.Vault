# Changelog - HoneyDrunk.Vault.Providers.InMemory

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
