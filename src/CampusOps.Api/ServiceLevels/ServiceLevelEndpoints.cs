using CampusOps.Core.Common;
using CampusOps.Core.Requests;
using CampusOps.Core.ServiceLevels;
namespace CampusOps.Api.ServiceLevels;

public sealed record StartServiceLevelRequest(string Priority, DateTimeOffset OpenedAt);
public static class ServiceLevelEndpoints
{
    public static IEndpointRouteBuilder MapServiceLevelEndpoints(this IEndpointRouteBuilder endpoints)
    { endpoints.MapPost("/api/v1/work-orders/{id:guid}/service-level", async (Guid id, StartServiceLevelRequest body, ServiceLevelPolicy policy, IServiceLevelStore store, CancellationToken token) => { if (!Enum.TryParse<RequestPriority>(body.Priority, true, out var priority)) return Results.BadRequest(); var item = new WorkOrderServiceLevel(id, policy.DueAt(priority, body.OpenedAt)); await store.AddAsync(item, token); return Results.Ok(item); }).WithTags("Service levels"); endpoints.MapPost("/api/v1/service-levels/escalate", async (ServiceLevelService service, CancellationToken token) => Results.Ok(new { escalated = await service.EscalateOverdueAsync(token) })).WithTags("Service levels"); return endpoints; }
}
