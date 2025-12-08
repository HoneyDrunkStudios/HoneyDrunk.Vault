namespace HoneyDrunk.Vault.Models;

/// <summary>
/// Represents the value of a secret retrieved from the vault.
/// </summary>
public sealed record SecretValue
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretValue"/> class.
    /// </summary>
    /// <param name="identifier">The identifier of the secret.</param>
    /// <param name="value">The secret value.</param>
    /// <param name="version">The version of the secret.</param>
    public SecretValue(SecretIdentifier identifier, string value, string? version)
    {
        Identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        Version = version;
    }

    /// <summary>
    /// Gets the identifier of the secret.
    /// </summary>
    public SecretIdentifier Identifier { get; }

    /// <summary>
    /// Gets the secret value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the version of the secret.
    /// </summary>
    public string? Version { get; }
}
