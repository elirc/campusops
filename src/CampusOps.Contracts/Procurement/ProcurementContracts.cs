namespace CampusOps.Contracts.Procurement;

public sealed record CreateVendorRequest(string Name, string ContactEmail);
public sealed record CreatePurchaseRequest(Guid WorkOrderId, Guid VendorId, string QuoteReference, decimal Amount, DateTimeOffset ValidUntil, string Justification);
public sealed record ApprovalRequest(string Approver);
