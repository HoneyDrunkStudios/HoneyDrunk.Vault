namespace HoneyDrunk.Vault.Providers.AppConfiguration.Configuration;

/// <summary>
/// Optional bootstrap settings for Azure App Configuration registration.
/// </summary>
public sealed class AppConfigurationOptions
{
    /// <summary>
    /// Gets or sets whether unlabeled keys should also be loaded as shared defaults.
    /// </summary>
    public bool IncludeUnlabeledKeys { get; set; } = true;
}
