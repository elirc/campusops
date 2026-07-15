using System.Collections.Concurrent;
using CampusOps.Core.WorkOrders;
namespace CampusOps.Infrastructure.WorkOrders;

public sealed class InMemoryWorkOrderStore : IWorkOrderStore
{
    private readonly ConcurrentDictionary<Guid, WorkOrder> _items = new();
    public Task AddAsync(WorkOrder order, CancellationToken token) { _items[order.Id] = order; return Task.CompletedTask; }
    public Task<WorkOrder?> GetAsync(Guid id, CancellationToken token) { _items.TryGetValue(id, out var order); return Task.FromResult(order); }
    public Task<IReadOnlyCollection<WorkOrder>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyCollection<WorkOrder>>(_items.Values.OrderByDescending(x => x.Priority).ThenBy(x => x.OpenedAt).ToArray());
}
