using CampusOps.Api.Security;
using CampusOps.Contracts.Inventory;
using CampusOps.Core.Inventory;

namespace CampusOps.Api.Inventory;

public static class PartEndpoints
{
    public static IEndpointRouteBuilder MapPartEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/parts").WithTags("Parts")
            .RequireAuthorization(SecurityPolicies.Operations);
        group.MapPost("/", async (CreatePartRequest body, IPartStore store, CancellationToken token) =>
        {
            var part = new Part(Guid.NewGuid(), body.Sku, body.Name, body.OnHand);
            await store.AddAsync(part, token);
            return Results.Created($"/api/v1/parts/{part.Id}", Map(part));
        });
        group.MapGet("/", async (IPartStore store, CancellationToken token) =>
            Results.Ok((await store.ListAsync(token)).Select(Map)));
        group.MapGet("/{id:guid}", async (Guid id, IPartStore store, CancellationToken token) =>
        {
            var part = await store.GetAsync(id, token);
            return part is null ? Results.NotFound() : Results.Ok(Map(part));
        });
        var workOrders = endpoints.MapGroup("/api/v1/work-orders/{id:guid}/parts")
            .WithTags("Work orders").RequireAuthorization(SecurityPolicies.Operations);
        workOrders.MapPost("/", async (Guid id, ReservePartRequest body, PartsService service, CancellationToken token) =>
            Results.Ok(await service.ReserveAsync(id, body.PartId, body.Quantity, token)));
        workOrders.MapGet("/", async (Guid id, IPartReservationStore store, CancellationToken token) =>
            Results.Ok(await store.ListForWorkOrderAsync(id, token)));
        return endpoints;
    }

    private static PartResponse Map(Part x) => new(x.Id, x.Sku, x.Name, x.OnHand, x.Reserved, x.Available);
}
