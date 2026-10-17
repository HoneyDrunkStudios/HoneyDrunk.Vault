namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// Configuration options for the Vault system.
/// </summary>
public sealed class VaultOptions
{
    /// <summary>
    /// Gets the provider registrations by logical name.
    /// </summary>
    public Dictionary<string, ProviderRegistration> Providers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the default provider name to use when no provider is specified.
    /// </summary>
    public string? DefaultProvider { get; set; }

    /// <summary>
    /// Gets or sets the caching options.
    /// </summary>
    public VaultCacheOptions Cache { get; set; } = new();

    /// <summary>
    /// Gets or sets the resilience options (circuit breaker, retry policies).
    /// </summary>
    public VaultResilienceOptions Resilience { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to enable telemetry for vault operations.
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Gets the list of secret keys to warm the cache with on startup.
    /// </summary>
    public List<string> WarmupKeys { get; } = [];

    /// <summary>
    /// Gets or sets a test/health-check secret key used to verify provider connectivity.
    /// </summary>
    public string? HealthCheckSecretKey { get; set; }

    /// <summary>
    /// Adds a provider registration.
    /// </summary>
    /// <param name="name">The logical name (e.g., "file", "azure-keyvault").</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The options instance for chaining.</returns>
    public VaultOptions AddProvider(string name, Action<ProviderRegistration> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        var registration = new ProviderRegistration { Name = name };
        configure(registration);
        Providers[name] = registration;

        // Set first provider as default if not set
        DefaultProvider ??= name;

        return this;
    }
}
