using CampusOps.Api.Security;
using CampusOps.Core.Reporting;
namespace CampusOps.Api.Reporting;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder endpoints)
    { var group = endpoints.MapGroup("/api/v1/reports/operations").WithTags("Reports").RequireAuthorization(SecurityPolicies.Manager); group.MapGet("/summary", async (OperationsReportService service, CancellationToken token) => Results.Ok(await service.SummarizeAsync(token))); group.MapGet("/work-orders.csv", async (OperationsReportService service, CancellationToken token) => Results.Text(await service.ExportWorkCsvAsync(token), "text/csv")); return endpoints; }
}
