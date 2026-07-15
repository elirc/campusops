using System.Collections.Concurrent;
using CampusOps.Core.WorkOrders;
namespace CampusOps.Infrastructure.WorkOrders;

public sealed class InMemoryWorkOrderHistoryStore : IWorkOrderHistoryStore
{
    private readonly ConcurrentQueue<WorkOrderComment> _comments = new();
    private readonly ConcurrentQueue<AuditEvent> _events = new();
    public Task AppendCommentAsync(WorkOrderComment comment, CancellationToken token) { _comments.Enqueue(comment); return Task.CompletedTask; }
    public Task AppendEventAsync(AuditEvent auditEvent, CancellationToken token) { _events.Enqueue(auditEvent); return Task.CompletedTask; }
    public Task<IReadOnlyCollection<WorkOrderComment>> ListCommentsAsync(Guid id, CancellationToken token) => Task.FromResult<IReadOnlyCollection<WorkOrderComment>>(_comments.Where(x => x.WorkOrderId == id).OrderBy(x => x.CreatedAt).ToArray());
    public Task<IReadOnlyCollection<AuditEvent>> ListEventsAsync(Guid id, CancellationToken token) => Task.FromResult<IReadOnlyCollection<AuditEvent>>(_events.Where(x => x.WorkOrderId == id).OrderBy(x => x.OccurredAt).ToArray());
}
