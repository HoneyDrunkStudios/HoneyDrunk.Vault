namespace HoneyDrunk.Vault.Models;

/// <summary>
/// Represents a unique identifier for a secret in the vault.
/// </summary>
public sealed record SecretIdentifier
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretIdentifier"/> class.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    public SecretIdentifier(string name)
        : this(name, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretIdentifier"/> class.
    /// </summary>
    /// <param name="name">The name of the secret.</param>
    /// <param name="version">The optional version of the secret.</param>
    public SecretIdentifier(string name, string? version)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(name));
        }

        Name = name;
        Version = version;
    }

    /// <summary>
    /// Gets the name of the secret.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the version of the secret, if specified.
    /// </summary>
    public string? Version { get; }
}
