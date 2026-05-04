using HoneyDrunk.Kernel.Abstractions.Identity;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Exceptions;
using HoneyDrunk.Vault.Models;

namespace HoneyDrunk.Vault.Services;

/// <summary>
/// Resolves secrets using the ADR-0026 tenant-scoped naming convention.
/// </summary>
/// <remarks>
/// Tenant-scoped secrets are looked up as <c>tenant-{tenantId}-{secretName}</c> first.
/// Internal tenants and tenant misses fall back to the standard node-level secret name.
/// </remarks>
public sealed class TenantScopedSecretResolver
{
    private const string TenantSecretPrefix = "tenant-";
    private readonly ISecretStore _secretStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantScopedSecretResolver"/> class.
    /// </summary>
    /// <param name="secretStore">The secret store to resolve from.</param>
    public TenantScopedSecretResolver(ISecretStore secretStore)
    {
        _secretStore = secretStore ?? throw new ArgumentNullException(nameof(secretStore));
    }

    /// <summary>
    /// Formats the ADR-0026 tenant-scoped secret name for a node-level secret name.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="secretName">The node-level secret name.</param>
    /// <returns>The tenant-scoped secret name.</returns>
    public static string FormatTenantScopedName(TenantId tenantId, string secretName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        return $"{TenantSecretPrefix}{tenantId}-{secretName}";
    }

    /// <summary>
    /// Resolves a secret for the supplied tenant, falling back to the standard node-level secret when appropriate.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="secretName">The node-level secret name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The resolved secret value.</returns>
    public async Task<SecretValue> ResolveAsync(
        TenantId tenantId,
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        if (tenantId.IsInternal)
        {
            return await _secretStore.GetSecretAsync(new SecretIdentifier(secretName), cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await _secretStore.GetSecretAsync(
                new SecretIdentifier(FormatTenantScopedName(tenantId, secretName)),
                cancellationToken).ConfigureAwait(false);
        }
        catch (SecretNotFoundException)
        {
            return await _secretStore.GetSecretAsync(new SecretIdentifier(secretName), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Attempts to resolve a secret for the supplied tenant, falling back to the standard node-level secret when appropriate.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="secretName">The node-level secret name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the resolved secret value if found.</returns>
    public async Task<VaultResult<SecretValue>> TryResolveAsync(
        TenantId tenantId,
        string secretName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretName);

        if (tenantId.IsInternal)
        {
            return await _secretStore.TryGetSecretAsync(new SecretIdentifier(secretName), cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var tenantScoped = await _secretStore.GetSecretAsync(
                new SecretIdentifier(FormatTenantScopedName(tenantId, secretName)),
                cancellationToken).ConfigureAwait(false);

            return VaultResult.Success(tenantScoped);
        }
        catch (SecretNotFoundException)
        {
            return await _secretStore.TryGetSecretAsync(new SecretIdentifier(secretName), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return VaultResult.Failure<SecretValue>($"Failed to retrieve tenant-scoped secret '{secretName}': {ex.Message}");
        }
    }
}
