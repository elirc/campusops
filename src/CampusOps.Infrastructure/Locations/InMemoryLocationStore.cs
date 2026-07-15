using System.Collections.Concurrent;
using CampusOps.Core.Locations;

namespace CampusOps.Infrastructure.Locations;

public sealed class InMemoryLocationStore : ILocationStore
{
    private readonly ConcurrentDictionary<Guid, Location> _locations = new();
    public Task AddAsync(Location location, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _locations[location.Id] = location;
        return Task.CompletedTask;
    }
    public Task<Location?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _locations.TryGetValue(id, out var location);
        return Task.FromResult(location);
    }
    public Task<IReadOnlyCollection<Location>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<Location>>(_locations.Values.OrderBy(item => item.DisplayName).ToArray());
}
