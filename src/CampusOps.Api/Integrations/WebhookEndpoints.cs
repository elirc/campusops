using CampusOps.Api.Security;
using CampusOps.Contracts.Integrations;
using CampusOps.Core.Integrations;
namespace CampusOps.Api.Integrations;

public static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder endpoints)
    { var group = endpoints.MapGroup("/api/v1/integrations/webhooks").WithTags("Integrations").RequireAuthorization(SecurityPolicies.Manager); group.MapPost("/subscriptions", async (CreateWebhookSubscription body, IWebhookStore store, CancellationToken token) => { var item = new WebhookSubscription(Guid.NewGuid(), body.EventType, new Uri(body.Endpoint), body.SecretName); await store.AddSubscriptionAsync(item, token); return Results.Created($"/api/v1/integrations/webhooks/subscriptions/{item.Id}", item); }); group.MapPost("/events", async (PublishWebhookEvent body, WebhookPublisher publisher, CancellationToken token) => Results.Accepted(value: new { deliveries = await publisher.EnqueueAsync(body.EventId, body.EventType, body.Payload, token) })); return endpoints; }
}
