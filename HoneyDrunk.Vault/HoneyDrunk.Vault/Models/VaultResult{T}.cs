namespace HoneyDrunk.Vault.Models;

/// <summary>
/// Represents a result from a vault operation that may or may not contain a value.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public sealed record VaultResult<T>
{
    private VaultResult(T? value, bool isSuccess, string? errorMessage)
    {
        Value = value;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the value if the operation was successful.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>A successful result.</returns>
    internal static VaultResult<T> Success(T value)
    {
        return new VaultResult<T>(value, true, null);
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result.</returns>
    internal static VaultResult<T> Failure(string errorMessage)
    {
        return new VaultResult<T>(default, false, errorMessage);
    }
}
