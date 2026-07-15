using CampusOps.Core.Common;
using CampusOps.Core.Locations;

namespace CampusOps.Core.Assets;

public enum AssetStatus { Active, OutOfService, Retired }

public sealed class Asset
{
    public Asset(Guid id, string assetTag, string name, string category, Guid locationId, AssetStatus status = AssetStatus.Active)
    {
        if (id == Guid.Empty || locationId == Guid.Empty) throw new DomainException("asset.identity_required", "Asset and location IDs are required.");
        if (string.IsNullOrWhiteSpace(assetTag) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
            throw new DomainException("asset.details_required", "Asset tag, name, and category are required.");
        Id = id; AssetTag = assetTag.Trim().ToUpperInvariant(); Name = name.Trim(); Category = category.Trim(); LocationId = locationId; Status = status;
    }
    public Guid Id { get; }
    public string AssetTag { get; }
    public string Name { get; private set; }
    public string Category { get; private set; }
    public Guid LocationId { get; private set; }
    public AssetStatus Status { get; private set; }
    public void MoveTo(Guid locationId) { if (Status == AssetStatus.Retired) throw new DomainException("asset.retired", "Retired assets cannot move."); LocationId = locationId; }
    public void MarkOutOfService() { if (Status != AssetStatus.Active) throw new DomainException("asset.not_active", "Only active assets can leave service."); Status = AssetStatus.OutOfService; }
    public void ReturnToService() { if (Status != AssetStatus.OutOfService) throw new DomainException("asset.not_out_of_service", "Asset is not out of service."); Status = AssetStatus.Active; }
    public void Retire() { if (Status == AssetStatus.Retired) throw new DomainException("asset.already_retired", "Asset is already retired."); Status = AssetStatus.Retired; }
}

public interface IAssetStore
{
    Task AddAsync(Asset asset, CancellationToken token);
    Task<Asset?> GetAsync(Guid id, CancellationToken token);
    Task<IReadOnlyCollection<Asset>> ListAsync(CancellationToken token);
}

public sealed class AssetService(IAssetStore assets, ILocationStore locations)
{
    public async Task<Asset> CreateAsync(string tag, string name, string category, Guid locationId, CancellationToken token)
    {
        _ = await locations.GetAsync(locationId, token) ?? throw new DomainException("location.not_found", "Location was not found.");
        var asset = new Asset(Guid.NewGuid(), tag, name, category, locationId);
        await assets.AddAsync(asset, token); return asset;
    }
    public Task<Asset?> GetAsync(Guid id, CancellationToken token) => assets.GetAsync(id, token);
    public Task<IReadOnlyCollection<Asset>> ListAsync(CancellationToken token) => assets.ListAsync(token);
    public async Task<Asset> ChangeStatusAsync(Guid id, string action, CancellationToken token)
    {
        var asset = await assets.GetAsync(id, token) ?? throw new DomainException("asset.not_found", "Asset was not found.");
        if (action == "out-of-service") asset.MarkOutOfService();
        else if (action == "return-to-service") asset.ReturnToService();
        else if (action == "retire") asset.Retire();
        else throw new DomainException("asset.action_invalid", "Unknown asset action.");
        return asset;
    }
}
