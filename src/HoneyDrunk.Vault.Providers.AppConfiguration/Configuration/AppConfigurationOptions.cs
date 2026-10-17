using Azure.Core;

namespace HoneyDrunk.Vault.Providers.AppConfiguration.Configuration;

/// <summary>
/// Optional bootstrap settings for Azure App Configuration registration.
/// </summary>
public sealed class AppConfigurationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether unlabeled keys should also be loaded as shared defaults.
    /// </summary>
    public bool IncludeUnlabeledKeys { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the Azure App Configuration provider is optional.
    /// When <c>true</c>, startup continues if the provider fails to load (useful for local development and testing).
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Gets or sets the token credential used for Azure App Configuration and Key Vault reference authentication.
    /// When <c>null</c>, <see cref="Azure.Identity.DefaultAzureCredential"/> is used.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the maximum time allowed for the initial configuration load from Azure App Configuration.
    /// When <c>null</c>, the SDK default timeout is used.
    /// </summary>
    public TimeSpan? StartupTimeout { get; set; }
}
