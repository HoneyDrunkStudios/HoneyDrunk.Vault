using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.EventGrid.Constants;
using HoneyDrunk.Vault.EventGrid.Extensions;
using HoneyDrunk.Vault.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Text;

namespace HoneyDrunk.Vault.Tests.EventGrid;

/// <summary>Exercises registered webhook delegates with real HTTP request and response streams.</summary>
public sealed class VaultInvalidationEndpointTests
{
    /// <summary>HTTP guards reject missing credentials and oversized bodies before resolving secrets.</summary>
    /// <param name="hasHeader">Whether the authentication header is present.</param>
    /// <param name="hasLength">Whether the request declares its size.</param>
    /// <param name="expectedStatus">The expected rejection status.</param>
    /// <returns>The test task.</returns>
    [Theory]
    [InlineData(false, true, 401)]
    [InlineData(true, true, 413)]
    [InlineData(true, false, 413)]
    public async Task RequestGuardsRejectInvalidEnvelopes(bool hasHeader, bool hasLength, int expectedStatus)
    {
        var store = Substitute.For<ISecretStore>();
        await using var app = CreateApp(store);
        using var body = new MemoryStream(new byte[(256 * 1024) + 1]);
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Body = body;
        context.Request.ContentLength = hasLength ? body.Length : null;
        if (hasHeader)
        {
            context.Request.Headers[VaultInvalidationWebhookConstants.SharedSecretHeaderName] = "fixture-secret";
        }

        await GetHandler(app)(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Empty(store.ReceivedCalls());
        Assert.True(body.CanRead);
    }

    /// <summary>The subscription handshake preserves the handler status, JSON body and cancellation token.</summary>
    /// <returns>The test task.</returns>
    [Fact]
    public async Task SubscriptionHandshakeWritesTheValidationResponse()
    {
        var store = Substitute.For<ISecretStore>();
        using var cancellation = new CancellationTokenSource();
        store.TryGetSecretAsync(Arg.Any<SecretIdentifier>(), cancellation.Token)
            .Returns(call => VaultResult.Success(new SecretValue(call.Arg<SecretIdentifier>(), "fixture-secret", "v1")));
        await using var app = CreateApp(store);
        const string payload = """
            [{"id":"validation","eventType":"Microsoft.EventGrid.SubscriptionValidationEvent","subject":"","eventTime":"2026-04-12T12:00:00Z","data":{"validationCode":"fixture-code"},"dataVersion":"1","metadataVersion":"1"}]
            """;
        using var body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        using var output = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = app.Services, RequestAborted = cancellation.Token };
        context.Request.Body = body;
        context.Request.Headers[VaultInvalidationWebhookConstants.SharedSecretHeaderName] = "fixture-secret";
        context.Response.Body = output;

        await GetHandler(app)(context);

        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.Equal("{\"validationResponse\":\"fixture-code\"}", Encoding.UTF8.GetString(output.ToArray()));
        await store.Received(1).TryGetSecretAsync(Arg.Any<SecretIdentifier>(), cancellation.Token);
    }

    private static WebApplication CreateApp(ISecretStore store)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(Substitute.For<ISecretCacheInvalidator>());
        builder.Services.AddVaultEventGridInvalidation();
        var app = builder.Build();
        app.MapVaultInvalidationWebhook("/vault/events");
        return app;
    }

    private static RequestDelegate GetHandler(WebApplication app) =>
        Assert.Single(((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints)).RequestDelegate!;
}
