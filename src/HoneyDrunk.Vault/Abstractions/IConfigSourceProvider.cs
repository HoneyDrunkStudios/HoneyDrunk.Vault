namespace HoneyDrunk.Vault.Abstractions;

/// <summary>
/// Defines the contract for a backend-specific configuration provider.
/// Implementations provide access to configuration from a specific backend (file, Azure App Configuration, etc.).
/// This interface extends IConfigSource to support the composite pattern in Vault core.
/// </summary>
public interface IConfigSourceProvider : IConfigSource
{
    /// <summary>
    /// Gets the logical name of this provider (e.g., "file", "configuration", "in-memory").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets a value indicating whether this provider is available and properly configured.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Checks if the provider is healthy and can communicate with its backend.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True if healthy, false otherwise.</returns>
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);
}
