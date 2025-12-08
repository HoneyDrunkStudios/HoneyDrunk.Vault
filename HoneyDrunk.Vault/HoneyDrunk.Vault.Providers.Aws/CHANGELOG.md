# Changelog - HoneyDrunk.Vault.Providers.Aws

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
