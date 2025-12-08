using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HoneyDrunk.Vault.Telemetry;

/// <summary>
/// Provides telemetry integration for vault operations.
/// Creates activities and enriches logs with context information.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VaultTelemetry"/> class.
/// </remarks>
/// <param name="logger">The logger.</param>
/// <param name="gridContextAccessor">The optional grid context accessor.</param>
/// <param name="operationContextAccessor">The optional operation context accessor.</param>
public sealed class VaultTelemetry(
    ILogger<VaultTelemetry> logger,
    IGridContextAccessor? gridContextAccessor = null,
    IOperationContextAccessor? operationContextAccessor = null)
{
    /// <summary>
    /// The activity source name for vault operations.
    /// </summary>
    public const string ActivitySourceName = "HoneyDrunk.Vault";

    private static readonly ActivitySource VaultActivitySource = new(ActivitySourceName);

    private readonly IGridContextAccessor? _gridContextAccessor = gridContextAccessor;
    private readonly IOperationContextAccessor? _operationContextAccessor = operationContextAccessor;
    private readonly ILogger<VaultTelemetry> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Executes a vault operation with telemetry tracking.
    /// </summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="operationName">The operation name (e.g., "get_secret").</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="key">The secret/config key (not the value).</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The operation result.</returns>
    public async Task<T> ExecuteWithTelemetryAsync<T>(
        string operationName,
        string providerName,
        string key,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var activityName = $"vault.{operationName}";
        using var activity = StartActivity(activityName, providerName, key);
        using var logScope = CreateLogScope(providerName, key);

        var startTime = Stopwatch.GetTimestamp();
        var resultStatus = "success";
        var cacheStatus = "miss";

        try
        {
            var result = await operation(cancellationToken).ConfigureAwait(false);

            // Check if this was a cache hit (indicated by timing)
            var elapsed = Stopwatch.GetElapsedTime(startTime);
            if (elapsed.TotalMilliseconds < 1)
            {
                cacheStatus = "hit";
            }

            SetActivityTags(activity, resultStatus, cacheStatus);

            _logger.LogDebug(
                "Vault operation '{Operation}' completed for key '{Key}' from provider '{Provider}' ({Status}/{Cache})",
                operationName,
                key,
                providerName,
                resultStatus,
                cacheStatus);

            return result;
        }
        catch (Exception ex)
        {
            resultStatus = "error";
            SetActivityTags(activity, resultStatus, cacheStatus);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            _logger.LogError(
                ex,
                "Vault operation '{Operation}' failed for key '{Key}' from provider '{Provider}'",
                operationName,
                key,
                providerName);

            throw;
        }
    }

    /// <summary>
    /// Records a cache hit in telemetry.
    /// </summary>
    /// <param name="key">The cache key.</param>
    public void RecordCacheHit(string key)
    {
        _logger.LogDebug("Cache hit for key '{Key}'", key);
    }

    /// <summary>
    /// Records a cache miss in telemetry.
    /// </summary>
    /// <param name="key">The cache key.</param>
    public void RecordCacheMiss(string key)
    {
        _logger.LogDebug("Cache miss for key '{Key}'", key);
    }

    private static void SetActivityTags(Activity? activity, string resultStatus, string cacheStatus)
    {
        if (activity == null)
        {
            return;
        }

        activity.SetTag("vault.result", resultStatus);
        activity.SetTag("vault.cache", cacheStatus);
    }

    private Activity? StartActivity(string name, string providerName, string key)
    {
        var activity = VaultActivitySource.StartActivity(name, ActivityKind.Client);

        if (activity == null)
        {
            return null;
        }

        // Add base tags
        activity.SetTag("vault.provider", providerName);
        activity.SetTag("vault.key", key);

        // Never log secret values - only key names
        // Add context from Kernel if available
        EnrichWithContext(activity);

        return activity;
    }

    private void EnrichWithContext(Activity activity)
    {
        // Add grid context if available
        var gridContext = _gridContextAccessor?.GridContext;
        if (gridContext != null)
        {
            activity.SetTag("grid.node_id", gridContext.NodeId.ToString());
            activity.SetTag("grid.studio_id", gridContext.StudioId.ToString());

            if (gridContext.TenantId is { } tenantId)
            {
                activity.SetTag("grid.tenant_id", tenantId.ToString());
            }

            activity.SetTag("grid.correlation_id", gridContext.CorrelationId.ToString());

            if (gridContext.CausationId is { } causationId)
            {
                activity.SetTag("grid.causation_id", causationId.ToString());
            }
        }

        // Add operation context if available
        var operationContext = _operationContextAccessor?.Current;
        if (operationContext != null)
        {
            activity.SetTag("operation.id", operationContext.OperationId.ToString());
            activity.SetTag("operation.name", operationContext.OperationName);
        }
    }

    private IDisposable? CreateLogScope(string providerName, string key)
    {
        return _logger.BeginScope(new Dictionary<string, object?>
        {
            ["VaultProvider"] = providerName,
            ["VaultKey"] = key,
        });
    }
}
