using CampusOps.Core.Assets;
using CampusOps.Core.Locations;
using CampusOps.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
namespace CampusOps.Infrastructure.Tests.Data;

public sealed class SqliteStoreTests
{
    [Fact]
    public async Task Stores_PersistLocationAndAssetAcrossContexts()
    { await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var options = new DbContextOptionsBuilder<CampusOpsDbContext>().UseSqlite(connection).Options; Guid assetId; await using (var setup = new CampusOpsDbContext(options)) { await setup.Database.EnsureCreatedAsync(); var location = new Location(Guid.NewGuid(), "North", "Science", "B-12"); await new SqliteLocationStore(setup).AddAsync(location, default); var asset = new Asset(Guid.NewGuid(), "PUMP-1", "Pump", "Plumbing", location.Id); assetId = asset.Id; await new SqliteAssetStore(setup).AddAsync(asset, default); } await using var verify = new CampusOpsDbContext(options); Assert.Equal("PUMP-1", (await new SqliteAssetStore(verify).GetAsync(assetId, default))?.AssetTag); }
}
