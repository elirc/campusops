using System.Collections.Concurrent;
using CampusOps.Core.ServiceLevels;
namespace CampusOps.Infrastructure.ServiceLevels;

public sealed class InMemoryServiceLevelStore : IServiceLevelStore
{ private readonly ConcurrentDictionary<Guid, WorkOrderServiceLevel> _items = new(); public Task AddAsync(WorkOrderServiceLevel item, CancellationToken token) { _items[item.WorkOrderId] = item; return Task.CompletedTask; } public Task<IReadOnlyCollection<WorkOrderServiceLevel>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyCollection<WorkOrderServiceLevel>>(_items.Values.ToArray()); }
