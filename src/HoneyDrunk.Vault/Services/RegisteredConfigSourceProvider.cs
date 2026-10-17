using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Represents a configuration source provider with its registration metadata.
/// </summary>
/// <param name="Provider">The configuration source provider instance.</param>
/// <param name="Registration">The provider registration metadata.</param>
public sealed record RegisteredConfigSourceProvider(
    IConfigSourceProvider Provider,
    ProviderRegistration Registration);
