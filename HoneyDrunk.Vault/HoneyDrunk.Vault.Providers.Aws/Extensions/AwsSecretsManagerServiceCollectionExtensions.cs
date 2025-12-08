using Amazon.SecretsManager;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Extensions;
using HoneyDrunk.Vault.Providers.Aws.Configuration;
using HoneyDrunk.Vault.Providers.Aws.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Vault.Providers.Aws.Extensions;

/// <summary>
/// Extension methods for configuring AWS Secrets Manager vault provider.
/// </summary>
public static class AwsSecretsManagerServiceCollectionExtensions
{
    /// <summary>
    /// Adds AWS Secrets Manager vault provider.
    /// This automatically registers the vault core services and AWS Secrets Manager implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultWithAwsSecretsManager(this IServiceCollection services)
    {
        return services.AddVaultWithAwsSecretsManager(_ => { });
    }

    /// <summary>
    /// Adds AWS Secrets Manager vault provider with the specified options.
    /// This automatically registers the vault core services and AWS Secrets Manager implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The configuration action for AWS Secrets Manager options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultWithAwsSecretsManager(
        this IServiceCollection services,
        Action<AwsSecretsManagerOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new AwsSecretsManagerOptions();
        configure(options);

        // Register options
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        // Register core vault services
        services.AddVaultCore();

        // Register AWS Secrets Manager implementations
        services.TryAddSingleton<ISecretStore, AwsSecretsManagerSecretStore>();
        services.TryAddSingleton<ISecretProvider, AwsSecretsManagerSecretStore>();

        return services;
    }

    /// <summary>
    /// Adds AWS Secrets Manager vault provider with a custom IAmazonSecretsManager client.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientFactory">Factory function to create the Secrets Manager client.</param>
    /// <param name="configure">The configuration action for AWS Secrets Manager options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddVaultWithAwsSecretsManager(
        this IServiceCollection services,
        Func<IServiceProvider, IAmazonSecretsManager> clientFactory,
        Action<AwsSecretsManagerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(clientFactory);

        var options = new AwsSecretsManagerOptions();
        configure?.Invoke(options);

        // Register options
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));

        // Register custom client
        services.TryAddSingleton(clientFactory);

        // Register core vault services
        services.AddVaultCore();

        // Register AWS Secrets Manager implementations
        services.TryAddSingleton<ISecretStore, AwsSecretsManagerSecretStore>();
        services.TryAddSingleton<ISecretProvider, AwsSecretsManagerSecretStore>();

        return services;
    }
}
