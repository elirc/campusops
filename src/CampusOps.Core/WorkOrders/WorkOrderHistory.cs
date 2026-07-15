using CampusOps.Core.Common;

namespace CampusOps.Core.WorkOrders;

public sealed record WorkOrderComment(Guid Id, Guid WorkOrderId, string Author, string Body, DateTimeOffset CreatedAt);
public sealed record AuditEvent(Guid Id, Guid WorkOrderId, string EventType, string Actor, string Detail, DateTimeOffset OccurredAt);
public interface IWorkOrderHistoryStore
{
    Task AppendCommentAsync(WorkOrderComment comment, CancellationToken token);
    Task AppendEventAsync(AuditEvent auditEvent, CancellationToken token);
    Task<IReadOnlyCollection<WorkOrderComment>> ListCommentsAsync(Guid workOrderId, CancellationToken token);
    Task<IReadOnlyCollection<AuditEvent>> ListEventsAsync(Guid workOrderId, CancellationToken token);
}
public sealed class WorkOrderHistoryService(IWorkOrderStore orders, IWorkOrderHistoryStore history)
{
    public async Task<WorkOrderComment> CommentAsync(Guid workOrderId, string author, string body, CancellationToken token)
    { _ = await orders.GetAsync(workOrderId, token) ?? throw new DomainException("work_order.not_found", "Work order was not found."); if (string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(body)) throw new DomainException("comment.details_required", "Author and body are required."); var comment = new WorkOrderComment(Guid.NewGuid(), workOrderId, author.Trim(), body.Trim(), DateTimeOffset.UtcNow); await history.AppendCommentAsync(comment, token); await history.AppendEventAsync(new AuditEvent(Guid.NewGuid(), workOrderId, "CommentAdded", author.Trim(), "A work-order comment was added.", DateTimeOffset.UtcNow), token); return comment; }
    public Task<IReadOnlyCollection<AuditEvent>> TimelineAsync(Guid id, CancellationToken token) => history.ListEventsAsync(id, token);
}
