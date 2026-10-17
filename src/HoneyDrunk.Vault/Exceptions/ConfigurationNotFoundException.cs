namespace HoneyDrunk.Vault.Exceptions;

/// <summary>
/// Exception thrown when a configuration value is not found.
/// </summary>
public sealed class ConfigurationNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationNotFoundException"/> class.
    /// </summary>
    /// <param name="key">The configuration key that was not found.</param>
    public ConfigurationNotFoundException(string key)
        : base($"Configuration key '{key}' was not found.")
    {
        Key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationNotFoundException"/> class.
    /// </summary>
    /// <param name="key">The configuration key that was not found.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConfigurationNotFoundException(string key, Exception innerException)
        : base($"Configuration key '{key}' was not found.", innerException)
    {
        Key = key;
    }

    /// <summary>
    /// Gets the configuration key that was not found.
    /// </summary>
    public string Key { get; }
}
