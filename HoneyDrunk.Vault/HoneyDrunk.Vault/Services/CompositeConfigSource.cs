using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Resilience;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Composite configuration source that orchestrates multiple providers with priority-based fallback.
/// </summary>
public sealed class CompositeConfigSource : IConfigSource, IConfigProvider
{
    private readonly ResiliencePipelineFactory _resilienceFactory;
    private readonly ILogger<CompositeConfigSource> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeConfigSource"/> class.
    /// </summary>
    /// <param name="providers">The registered configuration source providers.</param>
    /// <param name="resilienceFactory">The resilience pipeline factory.</param>
    /// <param name="logger">The logger.</param>
    public CompositeConfigSource(
        IEnumerable<RegisteredConfigSourceProvider> providers,
        ResiliencePipelineFactory resilienceFactory,
        ILogger<CompositeConfigSource> logger)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(resilienceFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _resilienceFactory = resilienceFactory;
        _logger = logger;

        // Order providers by priority (ascending - lower number = higher priority)
        Providers = [.. providers
            .Where(p => p.Registration.IsEnabled && p.Provider.IsAvailable)
            .OrderBy(p => p.Registration.Priority)];

        if (Providers.Count == 0)
        {
            _logger.LogWarning("No enabled and available configuration providers registered");
        }
        else
        {
            _logger.LogInformation(
                "Composite config source initialized with {Count} providers: {Providers}",
                Providers.Count,
                string.Join(", ", Providers.Select(p => $"{p.Provider.ProviderName} (priority: {p.Registration.Priority})")));
        }
    }

    /// <summary>
    /// Gets the registered providers in priority order.
    /// </summary>
    public IReadOnlyList<RegisteredConfigSourceProvider> Providers { get; }

    /// <inheritdoc/>
    public async Task<string> GetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var result = await TryGetConfigValueInternalAsync(key, cancellationToken).ConfigureAwait(false);

        if (result.Value != null)
        {
            return result.Value;
        }

        // Distinguish between "not found" and "error occurred"
        if (result.LastException != null && !result.IsNotFoundOnly)
        {
            throw new VaultOperationException(
                $"Failed to retrieve configuration key '{key}': {result.LastException.Message}",
                result.LastException);
        }

        throw new ConfigurationNotFoundException(key);
    }

    /// <inheritdoc/>
    public async Task<string?> TryGetConfigValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var result = await TryGetConfigValueInternalAsync(key, cancellationToken).ConfigureAwait(false);
        return result.Value;
    }

    /// <inheritdoc/>
    public async Task<T> GetConfigValueAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var stringValue = await GetConfigValueAsync(key, cancellationToken).ConfigureAwait(false);
        return ConvertValue<T>(stringValue, key);
    }

    /// <inheritdoc/>
    public async Task<T> TryGetConfigValueAsync<T>(string key, T defaultValue, CancellationToken cancellationToken = default)
    {
        var stringValue = await TryGetConfigValueAsync(key, cancellationToken).ConfigureAwait(false);

        if (stringValue == null)
        {
            return defaultValue;
        }

        try
        {
            return ConvertValue<T>(stringValue, key);
        }
        catch
        {
            _logger.LogWarning(
                "Failed to convert configuration value for key '{Key}' to type '{Type}', returning default",
                key,
                typeof(T).Name);
            return defaultValue;
        }
    }

    // IConfigProvider implementation - delegates to IConfigSource methods

    /// <inheritdoc/>
    Task<string> IConfigProvider.GetValueAsync(string key, CancellationToken cancellationToken)
    {
        return GetConfigValueAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    Task<T> IConfigProvider.GetValueAsync<T>(string path, T defaultValue, CancellationToken cancellationToken)
    {
        return TryGetConfigValueAsync(path, defaultValue, cancellationToken);
    }

    /// <inheritdoc/>
    Task<string?> IConfigProvider.TryGetValueAsync(string key, CancellationToken cancellationToken)
    {
        return TryGetConfigValueAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    Task<T> IConfigProvider.GetValueAsync<T>(string key, CancellationToken cancellationToken)
    {
        return GetConfigValueAsync<T>(key, cancellationToken);
    }

    private static T ConvertValue<T>(string value, string key)
    {
        try
        {
            var targetType = typeof(T);

            if (targetType == typeof(string))
            {
                return (T)(object)value;
            }

            if (targetType == typeof(int))
            {
                return (T)(object)int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(long))
            {
                return (T)(object)long.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(double))
            {
                return (T)(object)double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(decimal))
            {
                return (T)(object)decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(bool))
            {
                return (T)(object)bool.Parse(value);
            }

            if (targetType == typeof(Guid))
            {
                return (T)(object)Guid.Parse(value);
            }

            if (targetType == typeof(TimeSpan))
            {
                return (T)(object)TimeSpan.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(DateTime))
            {
                return (T)(object)DateTime.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(DateTimeOffset))
            {
                return (T)(object)DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            }

            if (targetType == typeof(Uri))
            {
                return (T)(object)new Uri(value);
            }

            // Try using the type converter as a fallback (invariant culture for consistency)
            var converter = System.ComponentModel.TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return (T)converter.ConvertFromInvariantString(value)!;
            }

            throw new InvalidOperationException($"Cannot convert string to type {targetType.Name}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new VaultOperationException(
                $"Failed to convert configuration value for key '{key}' to type {typeof(T).Name}",
                ex);
        }
    }

    /// <summary>
    /// Internal method that returns both the value and error information.
    /// </summary>
    private async Task<ConfigLookupResult> TryGetConfigValueInternalAsync(string key, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (Providers.Count == 0)
        {
            _logger.LogWarning("No providers available to retrieve configuration key '{Key}'", key);
            return ConfigLookupResult.NotFound();
        }

        Exception? lastException = null;
        var isNotFoundOnly = true;
        var attemptedProviders = new List<string>();

        foreach (var registered in Providers)
        {
            var providerName = registered.Provider.ProviderName;
            attemptedProviders.Add(providerName);

            try
            {
                var pipeline = _resilienceFactory.GetPipeline(providerName);

                var value = await pipeline.ExecuteAsync(
                    async ct =>
                    {
                        _logger.LogDebug(
                            "Attempting to fetch configuration key '{Key}' from provider '{ProviderName}'",
                            key,
                            providerName);

                        return await registered.Provider.TryGetConfigValueAsync(key, ct).ConfigureAwait(false);
                    },
                    cancellationToken).ConfigureAwait(false);

                if (value != null)
                {
                    _logger.LogDebug(
                        "Successfully retrieved configuration key '{Key}' from provider '{ProviderName}'",
                        key,
                        providerName);

                    return ConfigLookupResult.Success(value);
                }

                // Value not found in this provider - continue to next
                _logger.LogDebug(
                    "Configuration key '{Key}' not found in provider '{ProviderName}', trying next provider",
                    key,
                    providerName);
            }
            catch (Exception ex)
            {
                var classification = FailureClassifier.Classify(ex);

                _logger.LogDebug(
                    ex,
                    "Provider '{ProviderName}' failed for configuration key '{Key}' with classification '{Classification}'",
                    providerName,
                    key,
                    classification);

                switch (classification)
                {
                    case FailureClassification.NotFound:
                        // Continue to next provider (still "not found only")
                        lastException = ex;
                        continue;

                    case FailureClassification.FatalConfiguration:
                        // Fatal error - fail fast with error tracking
                        isNotFoundOnly = false;
                        _logger.LogError(
                            ex,
                            "Fatal configuration error from provider '{ProviderName}' for key '{Key}'",
                            providerName,
                            key);
                        return ConfigLookupResult.Error(ex, isNotFoundOnly: false);

                    case FailureClassification.Transient:
                        isNotFoundOnly = false;
                        if (registered.Registration.IsRequired)
                        {
                            // Required provider failed - fail fast with error tracking
                            _logger.LogError(
                                ex,
                                "Required provider '{ProviderName}' failed transiently for key '{Key}'",
                                providerName,
                                key);
                            return ConfigLookupResult.Error(ex, isNotFoundOnly: false);
                        }

                        // Optional provider - continue to next
                        _logger.LogWarning(
                            ex,
                            "Optional provider '{ProviderName}' failed transiently for key '{Key}', trying next provider",
                            providerName,
                            key);
                        lastException = ex;
                        continue;
                }
            }
        }

        // All providers exhausted
        _logger.LogDebug(
            "Configuration key '{Key}' not found in any provider. Attempted: {Providers}",
            key,
            string.Join(", ", attemptedProviders));

        return ConfigLookupResult.NotFoundOrError(lastException, isNotFoundOnly);
    }

    /// <summary>
    /// Internal result type that tracks both the value and error information.
    /// </summary>
    private readonly struct ConfigLookupResult
    {
        private ConfigLookupResult(string? value, Exception? lastException, bool isNotFoundOnly)
        {
            Value = value;
            LastException = lastException;
            IsNotFoundOnly = isNotFoundOnly;
        }

        public string? Value { get; }

        public Exception? LastException { get; }

        public bool IsNotFoundOnly { get; }

        public static ConfigLookupResult Success(string value) => new(value, null, true);

        public static ConfigLookupResult NotFound() => new(null, null, true);

        public static ConfigLookupResult Error(Exception exception, bool isNotFoundOnly) => new(null, exception, isNotFoundOnly);

        public static ConfigLookupResult NotFoundOrError(Exception? exception, bool isNotFoundOnly) => new(null, exception, isNotFoundOnly);
    }
}
