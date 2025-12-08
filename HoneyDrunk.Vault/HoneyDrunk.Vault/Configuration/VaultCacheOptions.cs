namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// Caching options for the vault.
/// </summary>
public sealed class VaultCacheOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether caching is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the default time-to-live for cached secrets.
    /// </summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum number of secrets to cache.
    /// </summary>
    public int MaxSize { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the sliding expiration time.
    /// </summary>
    public TimeSpan? SlidingExpiration { get; set; }
}
