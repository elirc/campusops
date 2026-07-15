using System.Collections.Concurrent;
using CampusOps.Core.Integrations;
namespace CampusOps.Infrastructure.Integrations;

public sealed class InMemoryWebhookStore : IWebhookStore
{ private readonly ConcurrentDictionary<Guid, WebhookSubscription> _subscriptions = new(); private readonly ConcurrentDictionary<Guid, WebhookDelivery> _deliveries = new(); public Task AddSubscriptionAsync(WebhookSubscription x, CancellationToken token) { _subscriptions[x.Id] = x; return Task.CompletedTask; } public Task<IReadOnlyCollection<WebhookSubscription>> ListSubscriptionsAsync(string type, CancellationToken token) => Task.FromResult<IReadOnlyCollection<WebhookSubscription>>(_subscriptions.Values.Where(x => x.EventType == type).ToArray()); public Task AddDeliveryAsync(WebhookDelivery x, CancellationToken token) { _deliveries[x.Id] = x; return Task.CompletedTask; } public Task<IReadOnlyCollection<WebhookDelivery>> ListDeliveriesAsync(CancellationToken token) => Task.FromResult<IReadOnlyCollection<WebhookDelivery>>(_deliveries.Values.ToArray()); }
public sealed class InMemoryIdempotencyStore : IIdempotencyStore
{ private readonly ConcurrentDictionary<string, byte> _keys = new(); public Task<bool> TryBeginAsync(string key, CancellationToken token) => Task.FromResult(_keys.TryAdd(key, 0)); }
