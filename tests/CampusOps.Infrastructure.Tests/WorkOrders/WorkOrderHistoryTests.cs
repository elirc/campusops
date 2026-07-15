using CampusOps.Core.Requests;
using CampusOps.Core.WorkOrders;
using CampusOps.Infrastructure.WorkOrders;
namespace CampusOps.Infrastructure.Tests.WorkOrders;

public sealed class WorkOrderHistoryTests
{
    [Fact]
    public async Task Comment_AppendsCommentAndAuditEvent()
    { var orders = new InMemoryWorkOrderStore(); var history = new InMemoryWorkOrderHistoryStore(); var order = new WorkOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, RequestPriority.Normal, "Inspect", DateTimeOffset.UtcNow); await orders.AddAsync(order, default); var service = new WorkOrderHistoryService(orders, history); await service.CommentAsync(order.Id, "Alex", "Access scheduled", default); Assert.Single(await history.ListCommentsAsync(order.Id, default)); Assert.Equal("CommentAdded", Assert.Single(await history.ListEventsAsync(order.Id, default)).EventType); }
}
