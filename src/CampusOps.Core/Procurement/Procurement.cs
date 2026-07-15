using CampusOps.Core.Common;
namespace CampusOps.Core.Procurement;

public sealed class Vendor
{
    public Vendor(Guid id, string name, string contactEmail) { if (id == Guid.Empty || string.IsNullOrWhiteSpace(name) || !contactEmail.Contains('@')) throw new DomainException("vendor.details_invalid", "Vendor name and valid contact email are required."); Id = id; Name = name.Trim(); ContactEmail = contactEmail.Trim().ToLowerInvariant(); }
    public Guid Id { get; }
    public string Name { get; }
    public string ContactEmail { get; }
    public bool IsActive { get; private set; } = true; public void Deactivate() => IsActive = false;
}
public sealed record VendorQuote(Guid Id, Guid VendorId, string Reference, decimal Amount, DateTimeOffset ValidUntil);
public enum PurchaseRequestStatus { Draft, PendingApproval, Approved, Rejected, Ordered }
public sealed class PurchaseRequest
{
    public PurchaseRequest(Guid id, Guid workOrderId, VendorQuote quote, string justification) { if (id == Guid.Empty || workOrderId == Guid.Empty || string.IsNullOrWhiteSpace(justification) || quote.Amount <= 0) throw new DomainException("purchase.details_invalid", "Purchase identity, positive quote, and justification are required."); Id = id; WorkOrderId = workOrderId; Quote = quote; Justification = justification.Trim(); }
    public Guid Id { get; }
    public Guid WorkOrderId { get; }
    public VendorQuote Quote { get; }
    public string Justification { get; }
    public PurchaseRequestStatus Status { get; private set; } = PurchaseRequestStatus.Draft; public string? ApprovedBy { get; private set; }
    public void Submit(decimal approvalThreshold) { if (Status != PurchaseRequestStatus.Draft) throw Invalid(); Status = Quote.Amount > approvalThreshold ? PurchaseRequestStatus.PendingApproval : PurchaseRequestStatus.Approved; if (Status == PurchaseRequestStatus.Approved) ApprovedBy = "policy:auto"; }
    public void Approve(string approver) { if (Status != PurchaseRequestStatus.PendingApproval || string.IsNullOrWhiteSpace(approver)) throw Invalid(); ApprovedBy = approver.Trim(); Status = PurchaseRequestStatus.Approved; }
    public void Reject() { if (Status != PurchaseRequestStatus.PendingApproval) throw Invalid(); Status = PurchaseRequestStatus.Rejected; }
    private static DomainException Invalid() => new("purchase.transition_invalid", "Purchase request cannot perform that action.");
}
public interface IVendorStore { Task AddAsync(Vendor vendor, CancellationToken token); Task<Vendor?> GetAsync(Guid id, CancellationToken token); }
public interface IPurchaseRequestStore { Task AddAsync(PurchaseRequest request, CancellationToken token); Task<PurchaseRequest?> GetAsync(Guid id, CancellationToken token); Task<IReadOnlyCollection<PurchaseRequest>> ListAsync(CancellationToken token); }
public sealed class ProcurementService(IVendorStore vendors, IPurchaseRequestStore purchases, decimal approvalThreshold)
{
    public async Task<PurchaseRequest> CreateAsync(Guid workOrderId, Guid vendorId, string reference, decimal amount, DateTimeOffset validUntil, string justification, CancellationToken token) { var vendor = await vendors.GetAsync(vendorId, token) ?? throw new DomainException("vendor.not_found", "Vendor was not found."); if (!vendor.IsActive) throw new DomainException("vendor.inactive", "Vendor is inactive."); var quote = new VendorQuote(Guid.NewGuid(), vendor.Id, reference, amount, validUntil.ToUniversalTime()); var request = new PurchaseRequest(Guid.NewGuid(), workOrderId, quote, justification); request.Submit(approvalThreshold); await purchases.AddAsync(request, token); return request; }
}
