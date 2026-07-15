namespace CampusOps.Contracts.Requests;

public sealed record SubmitMaintenanceRequest(Guid LocationId, Guid? AssetId, string Summary, string Description);
public sealed record TriageRequest(string Priority);
public sealed record MaintenanceRequestResponse(Guid Id, Guid LocationId, Guid? AssetId, string Summary, string Priority, string Status, DateTimeOffset SubmittedAt);
