namespace CampusOps.Contracts.Assets;

public sealed record CreateAssetRequest(string AssetTag, string Name, string Category, Guid LocationId);
public sealed record AssetResponse(Guid Id, string AssetTag, string Name, string Category, Guid LocationId, string Status);
