using CampusOps.Core.Common;
namespace CampusOps.Core.Integrations;

public sealed class WebhookSubscription
{
    public WebhookSubscription(Guid id, string eventType, Uri endpoint, string secretName) { if (id == Guid.Empty || string.IsNullOrWhiteSpace(eventType) || endpoint.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(secretName)) throw new DomainException("webhook.subscription_invalid", "Event type, HTTPS endpoint, and secret reference are required."); Id = id; EventType = eventType.Trim(); Endpoint = endpoint; SecretName = secretName.Trim(); }
    public Guid Id { get; }
    public string EventType { get; }
    public Uri Endpoint { get; }
    public string SecretName { get; }
    public bool IsActive { get; private set; } = true; public void Disable() => IsActive = false;
}
public enum DeliveryStatus { Pending, Succeeded, DeadLetter }
public sealed class WebhookDelivery
{
    public WebhookDelivery(Guid id, string eventId, WebhookSubscription subscription, string payload, DateTimeOffset now) { Id = id; EventId = eventId; SubscriptionId = subscription.Id; Endpoint = subscription.Endpoint; Payload = payload; NextAttemptAt = now.ToUniversalTime(); }
    public Guid Id { get; }
    public string EventId { get; }
    public Guid SubscriptionId { get; }
    public Uri Endpoint { get; }
    public string Payload { get; }
    public int Attempts { get; private set; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public void MarkSucceeded() { Status = DeliveryStatus.Succeeded; Attempts++; }
    public void MarkFailed(DateTimeOffset now, int maximumAttempts) { Attempts++; if (Attempts >= maximumAttempts) { Status = DeliveryStatus.DeadLetter; return; } NextAttemptAt = now.ToUniversalTime().AddMinutes(Math.Pow(2, Attempts)); }
}
public interface IWebhookStore { Task AddSubscriptionAsync(WebhookSubscription item, CancellationToken token); Task<IReadOnlyCollection<WebhookSubscription>> ListSubscriptionsAsync(string eventType, CancellationToken token); Task AddDeliveryAsync(WebhookDelivery item, CancellationToken token); Task<IReadOnlyCollection<WebhookDelivery>> ListDeliveriesAsync(CancellationToken token); }
public interface IIdempotencyStore { Task<bool> TryBeginAsync(string key, CancellationToken token); }
public sealed class WebhookPublisher(IWebhookStore store, IIdempotencyStore idempotency, IClock clock)
{
    public async Task<int> EnqueueAsync(string eventId, string eventType, string payload, CancellationToken token) { if (!await idempotency.TryBeginAsync($"{eventType}:{eventId}", token)) return 0; var subscriptions = await store.ListSubscriptionsAsync(eventType, token); foreach (var item in subscriptions.Where(x => x.IsActive)) await store.AddDeliveryAsync(new WebhookDelivery(Guid.NewGuid(), eventId, item, payload, clock.UtcNow), token); return subscriptions.Count(x => x.IsActive); }
}
