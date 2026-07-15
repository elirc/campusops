using Microsoft.EntityFrameworkCore;
namespace CampusOps.Infrastructure.Data;

public sealed class CampusOpsDbContext(DbContextOptions<CampusOpsDbContext> options) : DbContext(options)
{
    public DbSet<LocationRow> Locations => Set<LocationRow>();
    public DbSet<AssetRow> Assets => Set<AssetRow>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<LocationRow>().HasKey(x => x.Id);
        builder.Entity<LocationRow>().HasIndex(x => new { x.Campus, x.Building, x.Room }).IsUnique();
        builder.Entity<AssetRow>().HasKey(x => x.Id);
        builder.Entity<AssetRow>().HasIndex(x => x.AssetTag).IsUnique();
        builder.Entity<AssetRow>().HasOne<LocationRow>().WithMany().HasForeignKey(x => x.LocationId);
    }
}
public sealed class LocationRow { public Guid Id { get; set; } public required string Campus { get; set; } public required string Building { get; set; } public required string Room { get; set; } }
public sealed class AssetRow { public Guid Id { get; set; } public required string AssetTag { get; set; } public required string Name { get; set; } public required string Category { get; set; } public Guid LocationId { get; set; } public required string Status { get; set; } }
