using CampusOps.Contracts.Inventory;
using CampusOps.Core.Inventory;
namespace CampusOps.Api.Inventory;

public static class PartEndpoints
{
    public static IEndpointRouteBuilder MapPartEndpoints(this IEndpointRouteBuilder endpoints)
    { var group = endpoints.MapGroup("/api/v1/parts").WithTags("Parts"); group.MapPost("/", async (CreatePartRequest body, IPartStore store, CancellationToken token) => { var part = new Part(Guid.NewGuid(), body.Sku, body.Name, body.OnHand); await store.AddAsync(part, token); return Results.Created($"/api/v1/parts/{part.Id}", Map(part)); }); group.MapGet("/", async (IPartStore store, CancellationToken token) => Results.Ok((await store.ListAsync(token)).Select(Map))); endpoints.MapPost("/api/v1/work-orders/{id:guid}/parts", async (Guid id, ReservePartRequest body, PartsService service, CancellationToken token) => Results.Ok(await service.ReserveAsync(id, body.PartId, body.Quantity, token))).WithTags("Work orders"); return endpoints; }
    private static PartResponse Map(Part x) => new(x.Id, x.Sku, x.Name, x.OnHand, x.Reserved, x.Available);
}
