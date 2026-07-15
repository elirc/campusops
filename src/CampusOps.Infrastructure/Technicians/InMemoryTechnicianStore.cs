using System.Collections.Concurrent;
using CampusOps.Core.Technicians;
namespace CampusOps.Infrastructure.Technicians;

public sealed class InMemoryTechnicianStore : ITechnicianStore
{
    private readonly ConcurrentDictionary<Guid, Technician> _items = new();
    public Task AddAsync(Technician technician, CancellationToken token) { _items[technician.Id] = technician; return Task.CompletedTask; }
    public Task<Technician?> GetAsync(Guid id, CancellationToken token) { _items.TryGetValue(id, out var technician); return Task.FromResult(technician); }
    public Task<IReadOnlyCollection<Technician>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyCollection<Technician>>(_items.Values.OrderBy(x => x.Name).ToArray());
}
