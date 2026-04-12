using HoneyDrunk.Vault.EventGrid.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Vault.EventGrid.Extensions;

/// <summary>
/// Registers Vault Event Grid invalidation services.
/// </summary>
public static class VaultEventGridServiceCollectionExtensions
{
    /// <summary>
    /// Adds reusable Event Grid invalidation handlers for ASP.NET Core and Function hosts.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection instance so additional registrations can be chained.</returns>
    public static IServiceCollection AddVaultEventGridInvalidation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<VaultInvalidationWebhookHandler>();
        services.TryAddSingleton<VaultInvalidationFunctionHandler>();
        return services;
    }
}
