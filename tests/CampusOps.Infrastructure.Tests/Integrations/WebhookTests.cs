using CampusOps.Core.Common;
using CampusOps.Core.Integrations;
using CampusOps.Infrastructure.Integrations;
namespace CampusOps.Infrastructure.Tests.Integrations;

public sealed class WebhookTests
{
    [Fact] public async Task DuplicateEvent_IsEnqueuedOnce() { var store = new InMemoryWebhookStore(); await store.AddSubscriptionAsync(new WebhookSubscription(Guid.NewGuid(), "work.completed", new Uri("https://example.test/hooks"), "secret-ref"), default); var publisher = new WebhookPublisher(store, new InMemoryIdempotencyStore(), new SystemClock()); Assert.Equal(1, await publisher.EnqueueAsync("evt-1", "work.completed", "{}", default)); Assert.Equal(0, await publisher.EnqueueAsync("evt-1", "work.completed", "{}", default)); Assert.Single(await store.ListDeliveriesAsync(default)); }
    [Fact] public void FailedDelivery_BacksOffThenDeadLetters() { var delivery = new WebhookDelivery(Guid.NewGuid(), "evt", new WebhookSubscription(Guid.NewGuid(), "event", new Uri("https://example.test"), "secret"), "{}", DateTimeOffset.UtcNow); delivery.MarkFailed(DateTimeOffset.UtcNow, 2); Assert.Equal(DeliveryStatus.Pending, delivery.Status); delivery.MarkFailed(DateTimeOffset.UtcNow, 2); Assert.Equal(DeliveryStatus.DeadLetter, delivery.Status); }
}
