using CampusOps.Core.Common;

namespace CampusOps.Core.Locations;

public sealed class Location
{
    public Location(Guid id, string campus, string building, string room)
    {
        if (id == Guid.Empty) throw new DomainException("location.id_required", "Location ID is required.");
        if (string.IsNullOrWhiteSpace(campus) || string.IsNullOrWhiteSpace(building) || string.IsNullOrWhiteSpace(room))
            throw new DomainException("location.details_required", "Campus, building, and room are required.");
        Id = id;
        Campus = campus.Trim();
        Building = building.Trim();
        Room = room.Trim().ToUpperInvariant();
    }

    public Guid Id { get; }
    public string Campus { get; }
    public string Building { get; }
    public string Room { get; }
    public string DisplayName => $"{Campus} / {Building} / {Room}";
}

public interface ILocationStore
{
    Task AddAsync(Location location, CancellationToken cancellationToken);
    Task<Location?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Location>> ListAsync(CancellationToken cancellationToken);
}

public sealed class LocationService(ILocationStore store)
{
    public async Task<Location> CreateAsync(string campus, string building, string room, CancellationToken cancellationToken)
    {
        var location = new Location(Guid.NewGuid(), campus, building, room);
        await store.AddAsync(location, cancellationToken).ConfigureAwait(false);
        return location;
    }

    public Task<Location?> GetAsync(Guid id, CancellationToken cancellationToken) => store.GetAsync(id, cancellationToken);
    public Task<IReadOnlyCollection<Location>> ListAsync(CancellationToken cancellationToken) => store.ListAsync(cancellationToken);
}
