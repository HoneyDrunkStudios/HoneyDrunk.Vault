using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.Aws.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Vault.Providers.Aws.Services;

/// <summary>
/// AWS Secrets Manager implementation of the secret store.
/// </summary>
public sealed class AwsSecretsManagerSecretStore : ISecretStore, ISecretProvider, IDisposable
{
    private readonly AwsSecretsManagerOptions _options;
    private readonly IAmazonSecretsManager _client;
    private readonly ILogger<AwsSecretsManagerSecretStore> _logger;
    private readonly bool _ownsClient;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsSecretsManagerSecretStore"/> class.
    /// </summary>
    /// <param name="options">The AWS Secrets Manager options.</param>
    /// <param name="logger">The logger.</param>
    public AwsSecretsManagerSecretStore(
        IOptions<AwsSecretsManagerOptions> options,
        ILogger<AwsSecretsManagerSecretStore> logger)
        : this(options, null, logger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AwsSecretsManagerSecretStore"/> class.
    /// </summary>
    /// <param name="options">The AWS Secrets Manager options.</param>
    /// <param name="client">The AWS Secrets Manager client (optional).</param>
    /// <param name="logger">The logger.</param>
    public AwsSecretsManagerSecretStore(
        IOptions<AwsSecretsManagerOptions> options,
        IAmazonSecretsManager? client,
        ILogger<AwsSecretsManagerSecretStore> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (client != null)
        {
            _client = client;
            _ownsClient = false;
        }
        else
        {
            _client = CreateClient(_options);
            _ownsClient = true;
        }
    }

    /// <inheritdoc/>
    public string ProviderName => "aws-secrets-manager";

    /// <inheritdoc/>
    public bool IsAvailable => true; // Assume available if configured

    /// <inheritdoc/>
    public async Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        var secretId = BuildSecretId(identifier.Name);
        _logger.LogDebug("Getting secret '{SecretId}' from AWS Secrets Manager", secretId);

        try
        {
            var request = new GetSecretValueRequest
            {
                SecretId = secretId,
                VersionStage = _options.VersionStage,
            };

            if (!string.IsNullOrEmpty(identifier.Version))
            {
                request.VersionId = identifier.Version;
            }

            var response = await _client.GetSecretValueAsync(request, cancellationToken).ConfigureAwait(false);

            var value = response.SecretString ?? Convert.ToBase64String(response.SecretBinary.ToArray());
            var version = _options.UseVersionId ? response.VersionId : response.VersionStages?.FirstOrDefault() ?? "unknown";

            _logger.LogDebug("Successfully retrieved secret '{SecretName}' from AWS Secrets Manager", identifier.Name);

            return new SecretValue(identifier, value, version);
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning(ex, "Secret '{SecretId}' not found in AWS Secrets Manager", secretId);
            throw new SecretNotFoundException(identifier.Name);
        }
        catch (AmazonSecretsManagerException ex)
        {
            _logger.LogError(ex, "Error retrieving secret '{SecretId}' from AWS Secrets Manager", secretId);
            throw new VaultOperationException($"Failed to retrieve secret '{identifier.Name}' from AWS Secrets Manager", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryGetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identifier);

        try
        {
            var result = await GetSecretAsync(identifier, cancellationToken).ConfigureAwait(false);
            return VaultResult.Success(result);
        }
        catch (SecretNotFoundException)
        {
            return VaultResult.Failure<SecretValue>($"Secret '{identifier.Name}' not found");
        }
        catch (VaultOperationException ex)
        {
            return VaultResult.Failure<SecretValue>(ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or whitespace.", nameof(secretName));
        }

        var secretId = BuildSecretId(secretName);
        _logger.LogDebug("Listing versions for secret '{SecretId}' in AWS Secrets Manager", secretId);

        try
        {
            var versions = new List<SecretVersion>();
            string? nextToken = null;

            do
            {
                var request = new ListSecretVersionIdsRequest
                {
                    SecretId = secretId,
                    NextToken = nextToken,
                };

                var response = await _client.ListSecretVersionIdsAsync(request, cancellationToken).ConfigureAwait(false);

                foreach (var version in response.Versions)
                {
                    var versionId = _options.UseVersionId ? version.VersionId : version.VersionStages?.FirstOrDefault() ?? "unknown";
                    var createdDate = version.CreatedDate.HasValue
                        ? new DateTimeOffset(version.CreatedDate.Value, TimeSpan.Zero)
                        : DateTimeOffset.UtcNow;
                    versions.Add(new SecretVersion(versionId, createdDate));
                }

                nextToken = response.NextToken;
            }
            while (!string.IsNullOrEmpty(nextToken));

            _logger.LogDebug("Found {Count} versions for secret '{SecretName}'", versions.Count, secretName);

            return versions;
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning(ex, "Secret '{SecretId}' not found in AWS Secrets Manager", secretId);
            throw new SecretNotFoundException(secretName);
        }
        catch (AmazonSecretsManagerException ex)
        {
            _logger.LogError(ex, "Error listing versions for secret '{SecretId}' in AWS Secrets Manager", secretId);
            throw new VaultOperationException($"Failed to list versions for secret '{secretName}' from AWS Secrets Manager", ex);
        }
    }

    /// <inheritdoc/>
    public Task<SecretValue> FetchSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        return GetSecretAsync(new SecretIdentifier(key, version), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<VaultResult<SecretValue>> TryFetchSecretAsync(string key, string? version = null, CancellationToken cancellationToken = default)
    {
        return await TryGetSecretAsync(new SecretIdentifier(key, version), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<SecretVersion>> ListVersionsAsync(string key, CancellationToken cancellationToken = default)
    {
        return ListSecretVersionsAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to list secrets (with very limited results) to verify connectivity
            var request = new ListSecretsRequest
            {
                MaxResults = 1,
            };

            await _client.ListSecretsAsync(request, cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AWS Secrets Manager health check failed");
            return false;
        }
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_ownsClient)
        {
            _client.Dispose();
        }

        _disposed = true;
    }

    private static AmazonSecretsManagerClient CreateClient(AwsSecretsManagerOptions options)
    {
        var config = new AmazonSecretsManagerConfig();

        if (!string.IsNullOrEmpty(options.Region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        if (!string.IsNullOrEmpty(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
        }

        return new AmazonSecretsManagerClient(config);
    }

    private string BuildSecretId(string secretName)
    {
        return string.IsNullOrEmpty(_options.SecretPrefix)
            ? secretName
            : $"{_options.SecretPrefix}{secretName}";
    }
}
