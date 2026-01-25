using HoneyDrunk.Vault.Exceptions;

namespace HoneyDrunk.Vault.Resilience;

/// <summary>
/// Classifies exceptions into failure categories for resilience handling.
/// </summary>
public static class FailureClassifier
{
    /// <summary>
    /// Classifies an exception into a failure category.
    /// </summary>
    /// <param name="exception">The exception to classify.</param>
    /// <returns>The failure classification.</returns>
    public static FailureClassification Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            // Not found exceptions are explicit signals to try the next provider
            SecretNotFoundException => FailureClassification.NotFound,
            ConfigurationNotFoundException => FailureClassification.NotFound,

            // Fatal configuration errors should fail fast
            InvalidOperationException when IsConfigurationError(exception.Message) => FailureClassification.FatalConfiguration,
            ArgumentException => FailureClassification.FatalConfiguration,
            UnauthorizedAccessException => FailureClassification.FatalConfiguration,

            // All other exceptions are considered transient
            TimeoutException => FailureClassification.Transient,
            HttpRequestException => FailureClassification.Transient,
            OperationCanceledException => FailureClassification.Transient,
            VaultOperationException => FailureClassification.Transient,

            // Default to transient for unknown exceptions
            _ => FailureClassification.Transient,
        };
    }

    /// <summary>
    /// Determines if an exception represents a transient failure that can be retried.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the failure is transient; otherwise false.</returns>
    public static bool IsTransient(Exception exception)
    {
        return Classify(exception) == FailureClassification.Transient;
    }

    /// <summary>
    /// Determines if an exception represents a not-found condition.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the item was not found; otherwise false.</returns>
    public static bool IsNotFound(Exception exception)
    {
        return Classify(exception) == FailureClassification.NotFound;
    }

    /// <summary>
    /// Determines if an exception represents a fatal configuration error.
    /// </summary>
    /// <param name="exception">The exception to check.</param>
    /// <returns>True if the error is a fatal configuration error; otherwise false.</returns>
    public static bool IsFatalConfiguration(Exception exception)
    {
        return Classify(exception) == FailureClassification.FatalConfiguration;
    }

    private static bool IsConfigurationError(string message)
    {
        var lowerMessage = message.ToLowerInvariant();
        return lowerMessage.Contains("configuration")
            || lowerMessage.Contains("credential")
            || lowerMessage.Contains("authentication")
            || lowerMessage.Contains("not configured")
            || lowerMessage.Contains("missing required");
    }
}
