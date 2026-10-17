namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// Represents a provider registration.
/// </summary>
public sealed class ProviderRegistration
{
    /// <summary>
    /// Gets or sets the logical name of the provider.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider type.
    /// </summary>
    public ProviderType ProviderType { get; set; }

    /// <summary>
    /// Gets the provider-specific settings.
    /// </summary>
    public Dictionary<string, string> Settings { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets a value indicating whether this provider is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the priority order (lower is higher priority).
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this provider is required.
    /// When true, failures from this provider will fail fast instead of falling back.
    /// </summary>
    public bool IsRequired { get; set; }
}
