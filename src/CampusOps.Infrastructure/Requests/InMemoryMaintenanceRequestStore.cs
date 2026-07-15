using System.Collections.Concurrent;
using CampusOps.Core.Requests;
namespace CampusOps.Infrastructure.Requests;

public sealed class InMemoryMaintenanceRequestStore : IMaintenanceRequestStore
{
    private readonly ConcurrentDictionary<Guid, MaintenanceRequest> _items = new();
    public Task AddAsync(MaintenanceRequest request, CancellationToken token) { _items[request.Id] = request; return Task.CompletedTask; }
    public Task<MaintenanceRequest?> GetAsync(Guid id, CancellationToken token) { _items.TryGetValue(id, out var request); return Task.FromResult(request); }
    public Task<IReadOnlyCollection<MaintenanceRequest>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyCollection<MaintenanceRequest>>(_items.Values.OrderByDescending(x => x.SubmittedAt).ToArray());
}
