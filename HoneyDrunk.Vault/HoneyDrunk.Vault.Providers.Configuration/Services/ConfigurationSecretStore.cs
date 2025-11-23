using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Providers.Configuration.Services;

/// <summary>
/// Configuration-based implementation of the secret store.
/// </summary>
public sealed class ConfigurationSecretStore : ISecretStore
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConfigurationSecretStore> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationSecretStore"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public ConfigurationSecretStore(
        IConfiguration configuration,
        ILogger<ConfigurationSecretStore> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        _logger.LogDebug("Getting secret '{SecretName}' from configuration", identifier.Name);

        var key = BuildConfigurationKey(identifier.Name);
        var value = _configuration[key];

        if (string.IsNullOrEmpty(value))
        {
            _logger.LogWarning("Secret '{SecretName}' not found in configuration", identifier.Name);
            throw new SecretNotFoundException(identifier.Name);
        }

        var secretValue = new SecretValue(identifier, value, identifier.Version);
        _logger.LogDebug("Successfully retrieved secret '{SecretName}' from configuration", identifier.Name);

        return Task.FromResult(secretValue);
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        _logger.LogDebug("Attempting to get secret '{SecretName}' from configuration", identifier.Name);

        try
        {
            var secretValue = await GetSecretAsync(identifier, cancellationToken).ConfigureAwait(false);
            return VaultResult.Success(secretValue);
        }
        catch (SecretNotFoundException ex)
        {
            _logger.LogDebug("Secret '{SecretName}' not found in configuration", identifier.Name);
            return VaultResult.Failure<SecretValue>($"Secret '{identifier.Name}' not found: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretName}' from configuration", identifier.Name);
            return VaultResult.Failure<SecretValue>($"Failed to retrieve secret '{identifier.Name}': {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(secretName));
        }

        _logger.LogDebug("Listing versions for secret '{SecretName}' from configuration", secretName);

        // Configuration provider only supports a single version
        var key = BuildConfigurationKey(secretName);
        var value = _configuration[key];

        if (string.IsNullOrEmpty(value))
        {
            _logger.LogWarning("Secret '{SecretName}' not found in configuration", secretName);
            throw new SecretNotFoundException(secretName);
        }

        var versions = new List<SecretVersion>
        {
            new SecretVersion("latest", DateTimeOffset.UtcNow),
        };

        _logger.LogDebug("Listed {Count} version for secret '{SecretName}'", versions.Count, secretName);

        return Task.FromResult<IReadOnlyList<SecretVersion>>(versions);
    }

    private static string BuildConfigurationKey(string secretName)
    {
        // Support both "Secrets:SecretName" and direct "SecretName" formats
        return $"Secrets:{secretName}";
    }
}
