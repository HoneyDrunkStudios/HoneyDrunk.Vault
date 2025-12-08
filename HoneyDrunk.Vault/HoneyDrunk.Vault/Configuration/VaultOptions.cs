namespace HoneyDrunk.Vault.Configuration;

/// <summary>
/// Configuration options for the Vault system.
/// </summary>
public sealed class VaultOptions
{
    /// <summary>
    /// Gets the provider registrations by logical name.
    /// </summary>
    public Dictionary<string, ProviderRegistration> Providers { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the default provider name to use when no provider is specified.
    /// </summary>
    public string? DefaultProvider { get; set; }

    /// <summary>
    /// Gets or sets the caching options.
    /// </summary>
    public VaultCacheOptions Cache { get; set; } = new();

    /// <summary>
    /// Gets or sets the resilience options (circuit breaker, retry policies).
    /// </summary>
    public VaultResilienceOptions Resilience { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether to enable telemetry for vault operations.
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Gets the list of secret keys to warm the cache with on startup.
    /// </summary>
    public List<string> WarmupKeys { get; } = [];

    /// <summary>
    /// Gets or sets a test/health-check secret key used to verify provider connectivity.
    /// </summary>
    public string? HealthCheckSecretKey { get; set; }

    /// <summary>
    /// Adds a provider registration.
    /// </summary>
    /// <param name="name">The logical name (e.g., "file", "azure-keyvault").</param>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The options instance for chaining.</returns>
    public VaultOptions AddProvider(string name, Action<ProviderRegistration> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        var registration = new ProviderRegistration { Name = name };
        configure(registration);
        Providers[name] = registration;

        // Set first provider as default if not set
        DefaultProvider ??= name;

        return this;
    }

    /// <summary>
    /// Adds a file-based provider for local development.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The options instance for chaining.</returns>
    public VaultOptions AddFileProvider(Action<FileProviderOptions>? configure = null)
    {
        var fileOptions = new FileProviderOptions();
        configure?.Invoke(fileOptions);

        return AddProvider("file", reg =>
        {
            reg.ProviderType = ProviderType.File;
            reg.Settings["FilePath"] = fileOptions.FilePath;
            reg.Settings["EncryptionKeySource"] = fileOptions.EncryptionKeySource ?? string.Empty;
        });
    }

    /// <summary>
    /// Adds an Azure Key Vault provider.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The options instance for chaining.</returns>
    public VaultOptions AddAzureKeyVaultProvider(Action<AzureKeyVaultProviderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var azureOptions = new AzureKeyVaultProviderOptions();
        configure(azureOptions);

        return AddProvider("azure-keyvault", reg =>
        {
            reg.ProviderType = ProviderType.AzureKeyVault;
            if (azureOptions.VaultUri != null)
            {
                reg.Settings["VaultUri"] = azureOptions.VaultUri.ToString();
            }

            reg.Settings["UseManagedIdentity"] = azureOptions.UseManagedIdentity.ToString();
            reg.Settings["ClientId"] = azureOptions.ClientId ?? string.Empty;
            reg.Settings["TenantId"] = azureOptions.TenantId ?? string.Empty;
        });
    }

    /// <summary>
    /// Adds an AWS Secrets Manager provider.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The options instance for chaining.</returns>
    public VaultOptions AddAwsSecretsManagerProvider(Action<AwsSecretsManagerProviderOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var awsOptions = new AwsSecretsManagerProviderOptions();
        configure(awsOptions);

        return AddProvider("aws-secretsmanager", reg =>
        {
            reg.ProviderType = ProviderType.AwsSecretsManager;
            reg.Settings["Region"] = awsOptions.Region ?? string.Empty;
            reg.Settings["AccessKeyId"] = awsOptions.AccessKeyId ?? string.Empty;
            reg.Settings["UseInstanceProfile"] = awsOptions.UseInstanceProfile.ToString();
        });
    }

    /// <summary>
    /// Adds an in-memory provider for testing.
    /// </summary>
    /// <param name="configure">The configuration action.</param>
    /// <returns>The options instance for chaining.</returns>
    public VaultOptions AddInMemoryProvider(Action<InMemoryProviderOptions>? configure = null)
    {
        var memoryOptions = new InMemoryProviderOptions();
        configure?.Invoke(memoryOptions);

        return AddProvider("in-memory", reg =>
        {
            reg.ProviderType = ProviderType.InMemory;
            foreach (var (key, value) in memoryOptions.Secrets)
            {
                reg.Settings[$"Secret:{key}"] = value;
            }

            foreach (var (key, value) in memoryOptions.ConfigValues)
            {
                reg.Settings[$"Config:{key}"] = value;
            }
        });
    }
}
