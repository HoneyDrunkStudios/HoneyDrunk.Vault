# Changelog - HoneyDrunk.Vault.Providers.File

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
