# Changelog - HoneyDrunk.Vault.Providers.AppConfiguration

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-04-11

### Added
- Initial provider package.
- `AddAppConfiguration(IHoneyDrunkBuilder, Action<AppConfigurationOptions>?)` bootstrap extension.
- Azure App Configuration registration via `AZURE_APPCONFIG_ENDPOINT` and `HONEYDRUNK_NODE_ID`.
- Development fallback to `appsettings.Development.json` when endpoint is absent.
- Feature management (`IFeatureManager`) service registration.
