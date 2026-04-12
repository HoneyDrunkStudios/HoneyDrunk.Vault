using HoneyDrunk.Vault.EventGrid.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Vault.EventGrid.Extensions;

/// <summary>
/// Endpoint registration helpers for Vault invalidation webhooks.
/// </summary>
public static class VaultInvalidationEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps a POST webhook endpoint that validates Event Grid requests and invalidates Vault cache entries.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The convention builder.</returns>
    public static IEndpointConventionBuilder MapVaultInvalidationWebhook(
        this IEndpointRouteBuilder endpoints,
        string pattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapPost(pattern, async context =>
        {
            var handler = context.RequestServices.GetRequiredService<VaultInvalidationWebhookHandler>();
            var request = context.Request;

            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync(context.RequestAborted).ConfigureAwait(false);

            var headers = request.Headers.ToDictionary(
                pair => pair.Key,
                pair => (string?)pair.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            var response = await handler.HandleAsync(
                new Models.VaultInvalidationWebhookRequest(headers, body),
                context.RequestAborted).ConfigureAwait(false);

            context.Response.StatusCode = response.StatusCode;

            if (!string.IsNullOrWhiteSpace(response.Body))
            {
                context.Response.ContentType = response.ContentType;
                await context.Response.WriteAsync(response.Body, context.RequestAborted).ConfigureAwait(false);
            }
        });
    }
}
