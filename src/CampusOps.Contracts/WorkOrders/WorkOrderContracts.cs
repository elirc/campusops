namespace CampusOps.Contracts.WorkOrders;

public sealed record WorkOrderActionRequest(string? Reason, int ExpectedVersion);
public sealed record WorkOrderResponse(Guid Id, Guid RequestId, string Title, string Priority, string Status, string? BlockedReason, DateTimeOffset? CompletedAt, int Version);
