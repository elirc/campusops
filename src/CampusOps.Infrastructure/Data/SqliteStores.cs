using CampusOps.Core.Assets;
using CampusOps.Core.Locations;
using Microsoft.EntityFrameworkCore;
namespace CampusOps.Infrastructure.Data;

public sealed class SqliteLocationStore(CampusOpsDbContext db) : ILocationStore
{
    public async Task AddAsync(Location x, CancellationToken token) { db.Locations.Add(new LocationRow { Id = x.Id, Campus = x.Campus, Building = x.Building, Room = x.Room }); await db.SaveChangesAsync(token); }
    public async Task<Location?> GetAsync(Guid id, CancellationToken token) { var x = await db.Locations.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token); return x is null ? null : new Location(x.Id, x.Campus, x.Building, x.Room); }
    public async Task<IReadOnlyCollection<Location>> ListAsync(CancellationToken token) => (await db.Locations.AsNoTracking().OrderBy(x => x.Campus).ThenBy(x => x.Building).ThenBy(x => x.Room).ToArrayAsync(token)).Select(x => new Location(x.Id, x.Campus, x.Building, x.Room)).ToArray();
}
public sealed class SqliteAssetStore(CampusOpsDbContext db) : IAssetStore
{
    public async Task AddAsync(Asset x, CancellationToken token) { db.Assets.Add(new AssetRow { Id = x.Id, AssetTag = x.AssetTag, Name = x.Name, Category = x.Category, LocationId = x.LocationId, Status = x.Status.ToString() }); await db.SaveChangesAsync(token); }
    public async Task<Asset?> GetAsync(Guid id, CancellationToken token) { var x = await db.Assets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, token); return x is null ? null : Map(x); }
    public async Task<IReadOnlyCollection<Asset>> ListAsync(CancellationToken token) => (await db.Assets.AsNoTracking().OrderBy(x => x.AssetTag).ToArrayAsync(token)).Select(Map).ToArray();
    private static Asset Map(AssetRow x) => new(x.Id, x.AssetTag, x.Name, x.Category, x.LocationId, Enum.Parse<AssetStatus>(x.Status));
}
