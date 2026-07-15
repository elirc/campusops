using System.Collections.Concurrent;
using CampusOps.Core.PreventiveMaintenance;
namespace CampusOps.Infrastructure.PreventiveMaintenance;

public sealed class InMemoryMaintenancePlanStore : IMaintenancePlanStore
{ private readonly ConcurrentDictionary<Guid, MaintenancePlan> _items = new(); public Task AddAsync(MaintenancePlan plan, CancellationToken token) { _items[plan.Id] = plan; return Task.CompletedTask; } public Task<IReadOnlyCollection<MaintenancePlan>> ListAsync(CancellationToken token) => Task.FromResult<IReadOnlyCollection<MaintenancePlan>>(_items.Values.ToArray()); }
