using CampusOps.Contracts.Requests;
using CampusOps.Core.Requests;
namespace CampusOps.Api.Requests;

public static class MaintenanceRequestEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/maintenance-requests").WithTags("Maintenance requests");
        group.MapPost("/", async (SubmitMaintenanceRequest body, MaintenanceRequestService service, CancellationToken token) => { var request = await service.SubmitAsync(body.LocationId, body.AssetId, body.Summary, body.Description, token); return Results.Created($"/api/v1/maintenance-requests/{request.Id}", Map(request)); });
        group.MapPost("/{id:guid}/triage", async (Guid id, TriageRequest body, MaintenanceRequestService service, CancellationToken token) => Enum.TryParse<RequestPriority>(body.Priority, true, out var priority) ? Results.Ok(Map(await service.TriageAsync(id, priority, token))) : Results.BadRequest());
        group.MapGet("/", async (MaintenanceRequestService service, CancellationToken token) => Results.Ok((await service.ListAsync(token)).Select(Map)));
        return endpoints;
    }
    private static MaintenanceRequestResponse Map(MaintenanceRequest x) => new(x.Id, x.LocationId, x.AssetId, x.Summary, x.Priority.ToString(), x.Status.ToString(), x.SubmittedAt);
}
