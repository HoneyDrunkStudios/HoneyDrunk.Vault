using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Vault.Tests.Canary.Invariants;

/// <summary>
/// Enforces that Vault consumes Kernel contexts via DI/accessors and respects initialization state.
/// Vault must not create its own contexts and must not access context properties before initialization.
/// </summary>
public static class KernelContextOwnershipInvariant
{
    /// <summary>
    /// The invariant name for diagnostic messages.
    /// </summary>
    public const string InvariantName = "KernelContextOwnership";

    /// <summary>
    /// Validates context ownership by executing Vault operations and verifying context stability.
    /// </summary>
    /// <param name="scope">The DI scope containing Vault and Kernel services.</param>
    /// <param name="executeVaultOperations">A delegate that executes representative Vault operations.</param>
    /// <returns>A task representing the asynchronous validation operation.</returns>
    /// <exception cref="CanaryInvariantException">Thrown when context ownership invariants are violated.</exception>
    public static async Task ValidateAsync(
        IServiceScope scope,
        Func<IServiceProvider, Task> executeVaultOperations)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(executeVaultOperations);

        var services = scope.ServiceProvider;

        // Capture context accessors before operations
        var gridContextAccessor = services.GetService<IGridContextAccessor>();
        var operationContextAccessor = services.GetService<IOperationContextAccessor>();

        // Capture context identity before operations (using actual IDs, not hash codes)
        string? correlationIdBefore = null;
        string? operationIdBefore = null;
        bool gridContextInitializedBefore = false;

        if (gridContextAccessor != null)
        {
            var gridContext = gridContextAccessor.GridContext;
            gridContextInitializedBefore = gridContext.IsInitialized;
            if (gridContext.IsInitialized)
            {
                // Capture the actual correlation ID for comparison
                correlationIdBefore = gridContext.CorrelationId;
            }
        }

        if (operationContextAccessor != null)
        {
            var opContext = operationContextAccessor.Current;
            if (opContext != null)
            {
                operationIdBefore = opContext.OperationId;
            }
        }

        // Execute Vault operations
        Exception? vaultException = null;
        try
        {
            await executeVaultOperations(services).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Capture but don't rethrow yet - we need to validate invariants
            vaultException = ex;
        }

        // Capture context identity after operations
        string? correlationIdAfter = null;
        string? operationIdAfter = null;
        bool gridContextInitializedAfter = false;

        if (gridContextAccessor != null)
        {
            var gridContext = gridContextAccessor.GridContext;
            gridContextInitializedAfter = gridContext.IsInitialized;
            if (gridContext.IsInitialized)
            {
                correlationIdAfter = gridContext.CorrelationId;
            }
        }

        if (operationContextAccessor != null)
        {
            var opContext = operationContextAccessor.Current;
            if (opContext != null)
            {
                operationIdAfter = opContext.OperationId;
            }
        }

        // Validate: If grid context was initialized before, it should have the same identity after
        if (gridContextInitializedBefore && gridContextInitializedAfter &&
            !string.Equals(correlationIdBefore, correlationIdAfter, StringComparison.Ordinal))
        {
            var message = $"GridContext identity changed during Vault operations. Vault must not replace or recreate Kernel contexts. CorrelationId before: {correlationIdBefore}, after: {correlationIdAfter}";

            throw new CanaryInvariantException(InvariantName, message);
        }

        // Validate: Grid context should not transition from uninitialized to initialized by Vault
        if (!gridContextInitializedBefore && gridContextInitializedAfter)
        {
            var message = "GridContext was initialized by Vault operations. Vault must not initialize Kernel contexts - this is Kernel's responsibility.";

            throw new CanaryInvariantException(InvariantName, message);
        }

        // Validate: If operation context was present before, it should have the same identity after
        if (operationIdBefore != null && operationIdAfter != null &&
            !string.Equals(operationIdBefore, operationIdAfter, StringComparison.Ordinal))
        {
            var message = $"OperationContext identity changed during Vault operations. Vault must not replace or recreate Kernel contexts. OperationId before: {operationIdBefore}, after: {operationIdAfter}";

            throw new CanaryInvariantException(InvariantName, message);
        }

        // If Vault threw an exception related to uninitialized context access, fail the invariant
        if (vaultException != null)
        {
            if (IsUninitializedContextAccessException(vaultException))
            {
                var message = $"Vault accessed uninitialized Kernel context: {vaultException.Message}";

                throw new CanaryInvariantException(InvariantName, message);
            }

            // Re-throw other exceptions
            throw vaultException;
        }
    }

    /// <summary>
    /// Determines whether the exception indicates an attempt to access an uninitialized context.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns><see langword="true"/> if the exception indicates uninitialized context access; otherwise, <see langword="false"/>.</returns>
    private static bool IsUninitializedContextAccessException(Exception ex)
    {
        // Check if this is an InvalidOperationException from accessing uninitialized context
        if (ex is InvalidOperationException ioe)
        {
            var message = ioe.Message;
            if (message.Contains("not initialized", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("IsInitialized", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check inner exceptions
        if (ex.InnerException != null)
        {
            return IsUninitializedContextAccessException(ex.InnerException);
        }

        // Check aggregate exceptions
        if (ex is AggregateException ae)
        {
            return ae.InnerExceptions.Any(IsUninitializedContextAccessException);
        }

        return false;
    }
}
