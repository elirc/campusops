using System.Collections.Concurrent;
using CampusOps.Core.Inventory;
namespace CampusOps.Infrastructure.Inventory;

public sealed class InMemoryPartStore : IPartStore
{ private readonly ConcurrentDictionary<Guid, Part> _items = new(); public Task AddAsync(Part part, CancellationToken token) { _items[part.Id] = part; return Task.CompletedTask; } public Task<Part?> GetAsync(Guid id, CancellationToken token) { _items.TryGetValue(id, out var part); return Task.FromResult(part); } public Task<IReadOnlyCollection<Part>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyCollection<Part>>(_items.Values.OrderBy(x => x.Sku).ToArray()); public Task<Part?> TryReserveAsync(Guid id, int quantity, CancellationToken token) { if (!_items.TryGetValue(id, out var part)) return Task.FromResult<Part?>(null); lock (part) { if (quantity <= 0 || quantity > part.Available) return Task.FromResult<Part?>(null); part.Reserve(quantity); return Task.FromResult<Part?>(part); } } }
public sealed class InMemoryPartReservationStore : IPartReservationStore
{ private readonly ConcurrentQueue<PartReservation> _items = new(); public Task AddAsync(PartReservation item, CancellationToken token) { _items.Enqueue(item); return Task.CompletedTask; } public Task<IReadOnlyCollection<PartReservation>> ListForWorkOrderAsync(Guid id, CancellationToken token) => Task.FromResult<IReadOnlyCollection<PartReservation>>(_items.Where(x => x.WorkOrderId == id).ToArray()); }
