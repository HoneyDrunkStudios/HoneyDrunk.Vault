namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// Options for the in-memory provider.
/// </summary>
public sealed class InMemoryProviderOptions
{
    /// <summary>
    /// Gets the secrets dictionary.
    /// </summary>
    public Dictionary<string, string> Secrets { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the configuration values dictionary.
    /// </summary>
    public Dictionary<string, string> ConfigValues { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds a secret.
    /// </summary>
    /// <param name="key">The secret key.</param>
    /// <param name="value">The secret value.</param>
    /// <returns>The options instance for chaining.</returns>
    public InMemoryProviderOptions AddSecret(string key, string value)
    {
        Secrets[key] = value;
        return this;
    }

    /// <summary>
    /// Adds a configuration value.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>The options instance for chaining.</returns>
    public InMemoryProviderOptions AddConfigValue(string key, string value)
    {
        ConfigValues[key] = value;
        return this;
    }
}
