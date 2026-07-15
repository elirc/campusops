using CampusOps.Core.Common;

namespace CampusOps.Core.Requests;

public enum RequestPriority { Low, Normal, High, Emergency }
public enum RequestStatus { Submitted, Triaged, Converted, Cancelled }

public sealed class MaintenanceRequest
{
    public MaintenanceRequest(Guid id, Guid locationId, Guid? assetId, string summary, string description, DateTimeOffset submittedAt)
    {
        if (id == Guid.Empty || locationId == Guid.Empty) throw new DomainException("request.identity_required", "Request and location IDs are required.");
        if (string.IsNullOrWhiteSpace(summary) || summary.Trim().Length > 120) throw new DomainException("request.summary_invalid", "Summary is required and limited to 120 characters.");
        if (string.IsNullOrWhiteSpace(description)) throw new DomainException("request.description_required", "Description is required.");
        Id = id; LocationId = locationId; AssetId = assetId; Summary = summary.Trim(); Description = description.Trim(); SubmittedAt = submittedAt.ToUniversalTime();
    }
    public Guid Id { get; }
    public Guid LocationId { get; }
    public Guid? AssetId { get; }
    public string Summary { get; }
    public string Description { get; }
    public DateTimeOffset SubmittedAt { get; }
    public RequestPriority Priority { get; private set; } = RequestPriority.Normal;
    public RequestStatus Status { get; private set; } = RequestStatus.Submitted;
    public void Triage(RequestPriority priority) { if (Status != RequestStatus.Submitted) throw new DomainException("request.not_submitted", "Only submitted requests can be triaged."); Priority = priority; Status = RequestStatus.Triaged; }
    public void MarkConverted() { if (Status != RequestStatus.Triaged) throw new DomainException("request.not_triaged", "Triage is required before conversion."); Status = RequestStatus.Converted; }
}

public interface IMaintenanceRequestStore
{
    Task AddAsync(MaintenanceRequest request, CancellationToken token);
    Task<MaintenanceRequest?> GetAsync(Guid id, CancellationToken token);
    Task<IReadOnlyCollection<MaintenanceRequest>> ListAsync(CancellationToken token);
}

public sealed class MaintenanceRequestService(IMaintenanceRequestStore store)
{
    public async Task<MaintenanceRequest> SubmitAsync(Guid locationId, Guid? assetId, string summary, string description, CancellationToken token)
    {
        var request = new MaintenanceRequest(Guid.NewGuid(), locationId, assetId, summary, description, DateTimeOffset.UtcNow);
        await store.AddAsync(request, token); return request;
    }
    public async Task<MaintenanceRequest> TriageAsync(Guid id, RequestPriority priority, CancellationToken token) { var request = await store.GetAsync(id, token) ?? throw new DomainException("request.not_found", "Request was not found."); request.Triage(priority); return request; }
    public Task<IReadOnlyCollection<MaintenanceRequest>> ListAsync(CancellationToken token) => store.ListAsync(token);
}
