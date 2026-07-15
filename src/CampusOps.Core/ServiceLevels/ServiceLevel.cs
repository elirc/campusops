using CampusOps.Core.Common;
using CampusOps.Core.Requests;
namespace CampusOps.Core.ServiceLevels;

public sealed class ServiceLevelPolicy(IReadOnlyDictionary<RequestPriority, TimeSpan> targets)
{
    public DateTimeOffset DueAt(RequestPriority priority, DateTimeOffset openedAt) => openedAt.ToUniversalTime() + (targets.TryGetValue(priority, out var target) ? target : throw new DomainException("sla.target_missing", $"No SLA target for {priority}."));
}
public sealed class WorkOrderServiceLevel(Guid workOrderId, DateTimeOffset dueAt)
{
    private DateTimeOffset? _pausedAt;
    public Guid WorkOrderId { get; } = workOrderId;
    public DateTimeOffset DueAt { get; private set; } = dueAt.ToUniversalTime();
    public DateTimeOffset? EscalatedAt { get; private set; }
    public bool IsPaused => _pausedAt is not null;
    public void Pause(DateTimeOffset at) { if (IsPaused) throw new DomainException("sla.already_paused", "SLA is already paused."); _pausedAt = at.ToUniversalTime(); }
    public void Resume(DateTimeOffset at) { if (_pausedAt is null) throw new DomainException("sla.not_paused", "SLA is not paused."); DueAt += at.ToUniversalTime() - _pausedAt.Value; _pausedAt = null; }
    public bool EscalateIfOverdue(DateTimeOffset now) { if (IsPaused || EscalatedAt is not null || now.ToUniversalTime() <= DueAt) return false; EscalatedAt = now.ToUniversalTime(); return true; }
}
public interface IServiceLevelStore { Task AddAsync(WorkOrderServiceLevel item, CancellationToken token); Task<IReadOnlyCollection<WorkOrderServiceLevel>> ListAsync(CancellationToken token); }
public sealed class ServiceLevelService(IServiceLevelStore store, IClock clock)
{
    public async Task<int> EscalateOverdueAsync(CancellationToken token) { var count = 0; foreach (var item in await store.ListAsync(token)) if (item.EscalateIfOverdue(clock.UtcNow)) count++; return count; }
}
