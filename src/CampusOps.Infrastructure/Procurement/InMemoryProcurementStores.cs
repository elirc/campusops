using System.Collections.Concurrent;
using CampusOps.Core.Procurement;
namespace CampusOps.Infrastructure.Procurement;

public sealed class InMemoryVendorStore : IVendorStore { private readonly ConcurrentDictionary<Guid, Vendor> _items = new(); public Task AddAsync(Vendor x, CancellationToken token) { _items[x.Id] = x; return Task.CompletedTask; } public Task<Vendor?> GetAsync(Guid id, CancellationToken token) { _items.TryGetValue(id, out var x); return Task.FromResult(x); } }
public sealed class InMemoryPurchaseRequestStore : IPurchaseRequestStore { private readonly ConcurrentDictionary<Guid, PurchaseRequest> _items = new(); public Task AddAsync(PurchaseRequest x, CancellationToken token) { _items[x.Id] = x; return Task.CompletedTask; } public Task<PurchaseRequest?> GetAsync(Guid id, CancellationToken token) { _items.TryGetValue(id, out var x); return Task.FromResult(x); } public Task<IReadOnlyCollection<PurchaseRequest>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyCollection<PurchaseRequest>>(_items.Values.ToArray()); }
