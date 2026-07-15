using CampusOps.Contracts.Procurement;
using CampusOps.Core.Procurement;
namespace CampusOps.Api.Procurement;

public static class ProcurementEndpoints
{
    public static IEndpointRouteBuilder MapProcurementEndpoints(this IEndpointRouteBuilder endpoints)
    { var group = endpoints.MapGroup("/api/v1/procurement").WithTags("Procurement"); group.MapPost("/vendors", async (CreateVendorRequest body, IVendorStore store, CancellationToken token) => { var vendor = new Vendor(Guid.NewGuid(), body.Name, body.ContactEmail); await store.AddAsync(vendor, token); return Results.Created($"/api/v1/procurement/vendors/{vendor.Id}", vendor); }); group.MapPost("/purchase-requests", async (CreatePurchaseRequest body, ProcurementService service, CancellationToken token) => Results.Ok(await service.CreateAsync(body.WorkOrderId, body.VendorId, body.QuoteReference, body.Amount, body.ValidUntil, body.Justification, token))); group.MapPost("/purchase-requests/{id:guid}/approve", async (Guid id, ApprovalRequest body, IPurchaseRequestStore store, CancellationToken token) => { var request = await store.GetAsync(id, token); if (request is null) return Results.NotFound(); request.Approve(body.Approver); return Results.Ok(request); }); return endpoints; }
}
