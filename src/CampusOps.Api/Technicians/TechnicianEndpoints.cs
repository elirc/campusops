using CampusOps.Contracts.Technicians;
using CampusOps.Core.Technicians;
namespace CampusOps.Api.Technicians;

public static class TechnicianEndpoints
{
    public static IEndpointRouteBuilder MapTechnicianEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/technicians").WithTags("Technicians");
        group.MapPost("/", async (CreateTechnicianRequest body, ITechnicianStore store, CancellationToken token) => { var technician = new Technician(Guid.NewGuid(), body.Name, body.Skills); await store.AddAsync(technician, token); return Results.Created($"/api/v1/technicians/{technician.Id}", new TechnicianResponse(technician.Id, technician.Name, technician.Skills, technician.IsActive)); });
        endpoints.MapPost("/api/v1/work-orders/{id:guid}/assignment", async (Guid id, AssignTechnicianRequest body, AssignmentService service, CancellationToken token) => Results.Ok(await service.AssignAsync(id, body.TechnicianId, body.RequiredSkill, token))).WithTags("Work orders");
        return endpoints;
    }
}
