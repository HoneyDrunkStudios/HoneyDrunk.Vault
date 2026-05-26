using HoneyDrunk.Vault.Exceptions;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Shared helper methods for configuration source implementations.
/// </summary>
public static class ConfigSourceFacade
{
    /// <summary>
    /// Validates a configuration key.
    /// </summary>
    /// <param name="key">The key to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the key is null or whitespace.</exception>
    public static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }
    }

    /// <summary>
    /// Converts a string configuration value to the requested type.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <param name="key">The configuration key, used in error messages.</param>
    /// <returns>The converted value.</returns>
    public static T ConvertValue<T>(string value, string key)
    {
        try
        {
            var targetType = typeof(T);
            var effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (effectiveType == typeof(string))
            {
                return (T)(object)value;
            }

            if (effectiveType.IsEnum)
            {
                return (T)Enum.Parse(effectiveType, value, ignoreCase: true);
            }

            var converter = TypeDescriptor.GetConverter(effectiveType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                var result = converter.ConvertFrom(null, CultureInfo.InvariantCulture, value);
                if (result != null)
                {
                    return (T)result;
                }
            }

            return JsonSerializer.Deserialize<T>(value)
                ?? throw new InvalidOperationException($"Failed to deserialize configuration value for key '{key}'");
        }
        catch (Exception ex)
        {
            throw new VaultOperationException($"Failed to convert configuration value for key '{key}' to type '{typeof(T).Name}'", ex);
        }
    }

    /// <summary>
    /// Gets and converts a typed configuration value.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="getValueAsync">The string-value getter.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The converted value.</returns>
    public static async Task<T> GetValueAsync<T>(
        Func<string, CancellationToken, Task<string>> getValueAsync,
        string key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(getValueAsync);

        var value = await getValueAsync(key, cancellationToken).ConfigureAwait(false);
        return ConvertValue<T>(value, key);
    }

    /// <summary>
    /// Tries to get and convert a typed configuration value.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="tryGetValueAsync">The nullable string-value getter.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The fallback value.</param>
    /// <param name="logger">The optional logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The converted value, or <paramref name="defaultValue"/> when missing or invalid.</returns>
    public static async Task<T> TryGetValueAsync<T>(
        Func<string, CancellationToken, Task<string?>> tryGetValueAsync,
        string key,
        T defaultValue,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tryGetValueAsync);

        try
        {
            var value = await tryGetValueAsync(key, cancellationToken).ConfigureAwait(false);
            return value == null ? defaultValue : ConvertValue<T>(value, key);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to convert configuration value for key '{Key}' to type '{Type}', returning default", key, typeof(T).Name);
            return defaultValue;
        }
    }
}
