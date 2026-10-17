namespace HoneyDrunk.Vault.Models;

/// <summary>
/// Represents version information for a secret.
/// </summary>
public sealed record SecretVersion
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretVersion"/> class.
    /// </summary>
    /// <param name="version">The version identifier.</param>
    /// <param name="createdOn">The date and time when the version was created.</param>
    public SecretVersion(string version, DateTimeOffset createdOn)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Version cannot be null or whitespace.", nameof(version));
        }

        Version = version;
        CreatedOn = createdOn;
    }

    /// <summary>
    /// Gets the version identifier.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// Gets the date and time when the version was created.
    /// </summary>
    public DateTimeOffset CreatedOn { get; }
}
