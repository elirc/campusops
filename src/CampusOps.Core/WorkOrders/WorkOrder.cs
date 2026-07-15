using CampusOps.Core.Common;
using CampusOps.Core.Requests;

namespace CampusOps.Core.WorkOrders;

public enum WorkOrderStatus { Open, Assigned, InProgress, Blocked, Completed, Cancelled }
public sealed class WorkOrder
{
    public WorkOrder(Guid id, Guid requestId, Guid locationId, Guid? assetId, RequestPriority priority, string title, DateTimeOffset openedAt)
    { if (id == Guid.Empty || requestId == Guid.Empty) throw new DomainException("work_order.identity_required", "Work order and request IDs are required."); Id = id; RequestId = requestId; LocationId = locationId; AssetId = assetId; Priority = priority; Title = title; OpenedAt = openedAt.ToUniversalTime(); }
    public Guid Id { get; }
    public Guid RequestId { get; }
    public Guid LocationId { get; }
    public Guid? AssetId { get; }
    public RequestPriority Priority { get; }
    public string Title { get; }
    public DateTimeOffset OpenedAt { get; }
    public WorkOrderStatus Status { get; private set; } = WorkOrderStatus.Open;
    public Guid? TechnicianId { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? BlockedReason { get; private set; }
    public void Assign(Guid technicianId) { Require(WorkOrderStatus.Open); if (technicianId == Guid.Empty) throw new DomainException("work_order.technician_required", "Technician is required."); TechnicianId = technicianId; Status = WorkOrderStatus.Assigned; }
    public void Start() { if (Status is not (WorkOrderStatus.Assigned or WorkOrderStatus.Blocked)) throw Invalid(); Status = WorkOrderStatus.InProgress; BlockedReason = null; }
    public void Block(string reason) { Require(WorkOrderStatus.InProgress); if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("work_order.block_reason_required", "Blocked reason is required."); BlockedReason = reason.Trim(); Status = WorkOrderStatus.Blocked; }
    public void Complete(DateTimeOffset at) { Require(WorkOrderStatus.InProgress); Status = WorkOrderStatus.Completed; CompletedAt = at.ToUniversalTime(); }
    public void Cancel() { if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled) throw Invalid(); Status = WorkOrderStatus.Cancelled; }
    private void Require(WorkOrderStatus expected) { if (Status != expected) throw Invalid(); }
    private DomainException Invalid() => new("work_order.transition_invalid", $"Cannot change a work order from {Status} using that action.");
}
public interface IWorkOrderStore { Task AddAsync(WorkOrder order, CancellationToken token); Task<WorkOrder?> GetAsync(Guid id, CancellationToken token); Task<IReadOnlyCollection<WorkOrder>> ListAsync(CancellationToken token); }
public sealed class WorkOrderService(IWorkOrderStore orders, IMaintenanceRequestStore requests)
{
    public async Task<WorkOrder> ConvertAsync(Guid requestId, CancellationToken token) { var request = await requests.GetAsync(requestId, token) ?? throw new DomainException("request.not_found", "Request was not found."); request.MarkConverted(); var order = new WorkOrder(Guid.NewGuid(), request.Id, request.LocationId, request.AssetId, request.Priority, request.Summary, DateTimeOffset.UtcNow); await orders.AddAsync(order, token); return order; }
    public Task<IReadOnlyCollection<WorkOrder>> ListAsync(CancellationToken token) => orders.ListAsync(token);
    public async Task<WorkOrder> ActAsync(Guid id, string action, string? reason, CancellationToken token) { var order = await orders.GetAsync(id, token) ?? throw new DomainException("work_order.not_found", "Work order was not found."); if (action == "start") order.Start(); else if (action == "block") order.Block(reason ?? ""); else if (action == "complete") order.Complete(DateTimeOffset.UtcNow); else if (action == "cancel") order.Cancel(); else throw new DomainException("work_order.action_invalid", "Unknown action."); return order; }
}
