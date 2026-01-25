using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Configuration;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Represents a secret provider with its registration metadata.
/// </summary>
/// <param name="Provider">The secret provider instance.</param>
/// <param name="Registration">The provider registration metadata.</param>
public sealed record RegisteredSecretProvider(
    ISecretProvider Provider,
    ProviderRegistration Registration);
