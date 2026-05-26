using HoneyDrunk.Kernel.Abstractions.Lifecycle;
using HoneyDrunk.Vault.Services;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Vault.Health;

/// <summary>
/// Readiness contributor for the vault system.
/// Vault is ready only if all required providers are reachable.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VaultReadinessContributor"/> class.
/// </remarks>
/// <param name="secretProviders">The registered secret providers.</param>
/// <param name="configProviders">The registered configuration providers.</param>
/// <param name="logger">The logger.</param>
public sealed class VaultReadinessContributor(
    IEnumerable<RegisteredSecretProvider> secretProviders,
    IEnumerable<RegisteredConfigSourceProvider> configProviders,
    ILogger<VaultReadinessContributor> logger) : IReadinessContributor
{
    private readonly IEnumerable<RegisteredSecretProvider> _secretProviders = secretProviders ?? throw new ArgumentNullException(nameof(secretProviders));
    private readonly IEnumerable<RegisteredConfigSourceProvider> _configProviders = configProviders ?? throw new ArgumentNullException(nameof(configProviders));
    private readonly ILogger<VaultReadinessContributor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc/>
    public string Name => "HoneyDrunk.Vault";

    /// <inheritdoc/>
    public int Priority => 100;

    /// <inheritdoc/>
    public bool IsRequired => true;

    /// <inheritdoc/>
    public async Task<(bool, string?)> CheckReadinessAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Performing vault readiness check");

        var readyProviders = new HashSet<string>(StringComparer.Ordinal);
        var notReadyProviders = new HashSet<string>(StringComparer.Ordinal);
        var requiredNotReadyProviders = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in _secretProviders.Where(p => p.Registration.IsEnabled))
        {
            await ProbeAsync("Secret", entry.Provider.ProviderName, entry.Registration.IsRequired, entry.Provider.CheckHealthAsync, readyProviders, notReadyProviders, requiredNotReadyProviders, cancellationToken).ConfigureAwait(false);
        }

        foreach (var entry in _configProviders.Where(p => p.Registration.IsEnabled))
        {
            await ProbeAsync("Config", entry.Provider.ProviderName, entry.Registration.IsRequired, entry.Provider.CheckHealthAsync, readyProviders, notReadyProviders, requiredNotReadyProviders, cancellationToken).ConfigureAwait(false);
        }

        return Summarize(readyProviders, notReadyProviders, requiredNotReadyProviders);
    }

    private async Task ProbeAsync(
        string providerKind,
        string providerName,
        bool isRequired,
        Func<CancellationToken, Task<bool>> probe,
        HashSet<string> ready,
        HashSet<string> notReady,
        HashSet<string> requiredNotReady,
        CancellationToken cancellationToken)
    {
        try
        {
            var isHealthy = await probe(cancellationToken).ConfigureAwait(false);
            if (isHealthy)
            {
                ready.Add(providerName);
                _logger.LogDebug("{Kind} provider '{ProviderName}' is ready", providerKind, providerName);
                return;
            }

            notReady.Add(providerName);
            if (isRequired)
            {
                requiredNotReady.Add(providerName);
            }

            _logger.LogWarning(
                "{Kind} provider '{ProviderName}' is not ready (required: {IsRequired})",
                providerKind,
                providerName,
                isRequired);
        }
        catch (Exception ex)
        {
            notReady.Add(providerName);
            if (isRequired)
            {
                requiredNotReady.Add(providerName);
            }

            _logger.LogWarning(
                ex,
                "{Kind} provider '{ProviderName}' readiness check failed (required: {IsRequired})",
                providerKind,
                providerName,
                isRequired);
        }
    }

    private (bool isReady, string? message) Summarize(
        IReadOnlyCollection<string> ready,
        IReadOnlyCollection<string> notReady,
        IReadOnlyCollection<string> requiredNotReady)
    {
        // Not ready if any required provider is not reachable
        if (requiredNotReady.Count > 0)
        {
            _logger.LogError(
                "Vault not ready: required providers unavailable: {Providers}",
                string.Join(", ", requiredNotReady));
            return (isReady: false, message: $"Required providers not ready: {string.Join(", ", requiredNotReady)}");
        }

        // Ready if no providers are configured (degenerate case)
        if (ready.Count == 0 && notReady.Count == 0)
        {
            _logger.LogWarning("Vault ready but no providers configured");
            return (isReady: true, message: "Vault ready (no providers configured)");
        }

        // Ready if at least one provider is available (and no required providers are down)
        if (ready.Count > 0)
        {
            var summary = notReady.Count > 0
                ? $"Ready: {string.Join(", ", ready)}; Unavailable: {string.Join(", ", notReady)}"
                : $"Ready: {string.Join(", ", ready)}";

            _logger.LogDebug("Vault readiness check passed: {Message}", summary);
            return (isReady: true, message: summary);
        }

        // No providers ready
        _logger.LogError("Vault not ready: no providers available");
        return (isReady: false, message: "No providers available");
    }
}
