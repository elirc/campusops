using CampusOps.Contracts.Assets;
using CampusOps.Core.Assets;

namespace CampusOps.Api.Assets;

public static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/assets").WithTags("Assets");
        group.MapPost("/", async (CreateAssetRequest request, AssetService service, CancellationToken token) => { var asset = await service.CreateAsync(request.AssetTag, request.Name, request.Category, request.LocationId, token); return Results.Created($"/api/v1/assets/{asset.Id}", Map(asset)); });
        group.MapGet("/", async (AssetService service, CancellationToken token) => Results.Ok((await service.ListAsync(token)).Select(Map)));
        group.MapGet("/{id:guid}", async (Guid id, AssetService service, CancellationToken token) => { var asset = await service.GetAsync(id, token); return asset is null ? Results.NotFound() : Results.Ok(Map(asset)); });
        group.MapPost("/{id:guid}/{action}", async (Guid id, string action, AssetService service, CancellationToken token) => Results.Ok(Map(await service.ChangeStatusAsync(id, action, token))));
        return endpoints;
    }
    private static AssetResponse Map(Asset asset) => new(asset.Id, asset.AssetTag, asset.Name, asset.Category, asset.LocationId, asset.Status.ToString());
}
