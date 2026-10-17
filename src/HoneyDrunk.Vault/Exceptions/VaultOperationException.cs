namespace HoneyDrunk.Vault.Exceptions;

/// <summary>
/// Exception thrown when an operation on the vault fails.
/// </summary>
public sealed class VaultOperationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public VaultOperationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultOperationException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public VaultOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
