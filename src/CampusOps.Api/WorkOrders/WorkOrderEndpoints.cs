using CampusOps.Contracts.WorkOrders;
using CampusOps.Core.WorkOrders;
namespace CampusOps.Api.WorkOrders;

public static class WorkOrderEndpoints
{
    public static IEndpointRouteBuilder MapWorkOrderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/work-orders").WithTags("Work orders");
        group.MapPost("/from-request/{requestId:guid}", async (Guid requestId, WorkOrderService service, CancellationToken token) => { var order = await service.ConvertAsync(requestId, token); return Results.Created($"/api/v1/work-orders/{order.Id}", Map(order)); });
        group.MapPost("/{id:guid}/{action}", async (Guid id, string action, WorkOrderActionRequest body, WorkOrderService service, CancellationToken token) => Results.Ok(Map(await service.ActAsync(id, action, body.Reason, token))));
        group.MapGet("/", async (WorkOrderService service, CancellationToken token) => Results.Ok((await service.ListAsync(token)).Select(Map)));
        return endpoints;
    }
    private static WorkOrderResponse Map(WorkOrder x) => new(x.Id, x.RequestId, x.Title, x.Priority.ToString(), x.Status.ToString(), x.BlockedReason, x.CompletedAt);
}
