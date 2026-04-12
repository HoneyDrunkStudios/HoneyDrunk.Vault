namespace HoneyDrunk.Vault.Providers.InMemory.Configuration;

/// <summary>
/// Configuration options for the in-memory vault provider.
/// </summary>
public sealed class InMemoryVaultOptions
{
    /// <summary>
    /// Gets the dictionary of secrets. Key is the secret name, value is the secret value.
    /// </summary>
    public Dictionary<string, string> Secrets { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the dictionary of configuration values. Key is the configuration key, value is the configuration value.
    /// </summary>
    public Dictionary<string, string> ConfigurationValues { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a secret to the in-memory store.
    /// </summary>
    /// <param name="name">The secret name.</param>
    /// <param name="value">The secret value.</param>
    /// <returns>The options instance for chaining.</returns>
    public InMemoryVaultOptions AddSecret(string name, string value)
    {
        Secrets[name] = value;
        return this;
    }

    /// <summary>
    /// Adds a configuration value to the in-memory store.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>The options instance for chaining.</returns>
    public InMemoryVaultOptions AddConfigValue(string key, string value)
    {
        ConfigurationValues[key] = value;
        return this;
    }
}
