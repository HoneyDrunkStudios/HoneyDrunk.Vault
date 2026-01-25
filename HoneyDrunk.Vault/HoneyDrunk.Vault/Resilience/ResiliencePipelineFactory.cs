using HoneyDrunk.Vault.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Collections.Concurrent;

namespace HoneyDrunk.Vault.Resilience;

/// <summary>
/// Factory for creating resilience pipelines for vault providers.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ResiliencePipelineFactory"/> class.
/// </remarks>
/// <param name="options">The resilience options.</param>
/// <param name="logger">The logger.</param>
public sealed class ResiliencePipelineFactory(
    VaultResilienceOptions options,
    ILogger<ResiliencePipelineFactory> logger)
{
    private readonly VaultResilienceOptions _options = options ?? throw new ArgumentNullException(nameof(options));
    private readonly ILogger<ResiliencePipelineFactory> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ConcurrentDictionary<string, ResiliencePipeline> _pipelines = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, CircuitState> _circuitStates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or creates a resilience pipeline for the specified provider.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The resilience pipeline for the provider.</returns>
    public ResiliencePipeline GetPipeline(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        return _pipelines.GetOrAdd(providerName, CreatePipeline);
    }

    /// <summary>
    /// Gets the circuit breaker state for the specified provider.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The circuit state, or null if no circuit breaker exists for the provider.</returns>
    public CircuitState? GetCircuitState(string providerName)
    {
        if (_pipelines.TryGetValue(providerName, out _))
        {
            // Circuit breaker state is managed internally by Polly
            // We track it through the OnCircuitStateChange callback
            return _circuitStates.GetValueOrDefault(providerName);
        }

        return null;
    }

    private ResiliencePipeline CreatePipeline(string providerName)
    {
        var pipelineBuilder = new ResiliencePipelineBuilder();

        // Add retry strategy if enabled
        if (_options.RetryEnabled && _options.MaxRetryAttempts > 0)
        {
            pipelineBuilder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = _options.MaxRetryAttempts,
                Delay = _options.RetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                {
                    // Only retry transient failures
                    var classification = FailureClassifier.Classify(ex);
                    return classification == FailureClassification.Transient;
                }),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Retry attempt {AttemptNumber} for provider '{ProviderName}' after {Delay}ms",
                        args.AttemptNumber,
                        providerName,
                        args.RetryDelay.TotalMilliseconds);

                    return ValueTask.CompletedTask;
                },
            });
        }

        // Add circuit breaker if enabled
        if (_options.CircuitBreakerEnabled)
        {
            _circuitStates[providerName] = CircuitState.Closed;

            pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5, // Open circuit when 50% of requests fail
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = _options.FailureThreshold,
                BreakDuration = _options.CircuitBreakDuration,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(ex =>
                {
                    // Only count transient failures toward circuit breaker
                    var classification = FailureClassifier.Classify(ex);
                    return classification == FailureClassification.Transient;
                }),
                OnOpened = args =>
                {
                    _circuitStates[providerName] = CircuitState.Open;
                    _logger.LogWarning(
                        "Circuit breaker opened for provider '{ProviderName}' for {Duration}ms",
                        providerName,
                        args.BreakDuration.TotalMilliseconds);

                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _circuitStates[providerName] = CircuitState.Closed;
                    _logger.LogInformation(
                        "Circuit breaker closed for provider '{ProviderName}'",
                        providerName);

                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _circuitStates[providerName] = CircuitState.HalfOpen;
                    _logger.LogInformation(
                        "Circuit breaker half-open for provider '{ProviderName}'",
                        providerName);

                    return ValueTask.CompletedTask;
                },
            });
        }

        // Add timeout if configured
        if (_options.Timeout > TimeSpan.Zero)
        {
            pipelineBuilder.AddTimeout(_options.Timeout);
        }

        return pipelineBuilder.Build();
    }
}
