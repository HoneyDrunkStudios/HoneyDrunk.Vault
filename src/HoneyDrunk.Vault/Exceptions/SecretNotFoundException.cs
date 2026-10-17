namespace HoneyDrunk.Vault.Exceptions;

/// <summary>
/// Exception thrown when a secret is not found in the vault.
/// </summary>
public sealed class SecretNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecretNotFoundException"/> class.
    /// </summary>
    /// <param name="secretName">The name of the secret that was not found.</param>
    public SecretNotFoundException(string secretName)
        : base($"Secret '{secretName}' was not found in the vault.")
    {
        SecretName = secretName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretNotFoundException"/> class.
    /// </summary>
    /// <param name="secretName">The name of the secret that was not found.</param>
    /// <param name="innerException">The inner exception.</param>
    public SecretNotFoundException(string secretName, Exception innerException)
        : base($"Secret '{secretName}' was not found in the vault.", innerException)
    {
        SecretName = secretName;
    }

    /// <summary>
    /// Gets the name of the secret that was not found.
    /// </summary>
    public string SecretName { get; }
}
