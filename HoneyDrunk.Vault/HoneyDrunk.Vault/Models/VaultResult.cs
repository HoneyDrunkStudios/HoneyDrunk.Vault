namespace HoneyDrunk.Vault.Models;

/// <summary>
/// Provides factory methods for creating vault results.
/// </summary>
public static class VaultResult
{
    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A successful result.</returns>
    public static VaultResult<T> Success<T>(T value)
    {
        return VaultResult<T>.Success(value);
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="errorMessage">The error message.</param>
    /// <returns>A failed result.</returns>
    public static VaultResult<T> Failure<T>(string errorMessage)
    {
        return VaultResult<T>.Failure(errorMessage);
    }
}
