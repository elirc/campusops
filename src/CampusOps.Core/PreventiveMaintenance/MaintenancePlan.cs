using CampusOps.Core.Common;
using CampusOps.Core.Requests;
namespace CampusOps.Core.PreventiveMaintenance;

public sealed class MaintenancePlan
{
    public MaintenancePlan(Guid id, Guid assetId, Guid locationId, string task, int intervalDays, DateTimeOffset nextDueAt) { if (id == Guid.Empty || assetId == Guid.Empty || locationId == Guid.Empty || string.IsNullOrWhiteSpace(task)) throw new DomainException("plan.details_required", "Plan identity, location, asset, and task are required."); if (intervalDays < 1) throw new DomainException("plan.interval_invalid", "Interval must be at least one day."); Id = id; AssetId = assetId; LocationId = locationId; Task = task.Trim(); IntervalDays = intervalDays; NextDueAt = nextDueAt.ToUniversalTime(); }
    public Guid Id { get; }
    public Guid AssetId { get; }
    public Guid LocationId { get; }
    public string Task { get; }
    public int IntervalDays { get; }
    public DateTimeOffset NextDueAt { get; private set; }
    public bool IsDue(DateTimeOffset now) => NextDueAt <= now.ToUniversalTime();
    public void MarkGenerated() => NextDueAt = NextDueAt.AddDays(IntervalDays);
}
public interface IMaintenancePlanStore { Task AddAsync(MaintenancePlan plan, CancellationToken token); Task<IReadOnlyCollection<MaintenancePlan>> ListAsync(CancellationToken token); }
public sealed class PreventiveMaintenanceService(IMaintenancePlanStore plans, IMaintenanceRequestStore requests, IClock clock)
{
    public async Task<int> GenerateDueAsync(CancellationToken token) { var count = 0; foreach (var plan in (await plans.ListAsync(token)).Where(x => x.IsDue(clock.UtcNow))) { var request = new MaintenanceRequest(Guid.NewGuid(), plan.LocationId, plan.AssetId, $"Preventive: {plan.Task}", $"Scheduled maintenance plan {plan.Id}", clock.UtcNow); request.Triage(RequestPriority.Normal); await requests.AddAsync(request, token); plan.MarkGenerated(); count++; } return count; }
}
