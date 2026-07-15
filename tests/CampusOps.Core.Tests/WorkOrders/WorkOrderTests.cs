using CampusOps.Core.Common;
using CampusOps.Core.Requests;
using CampusOps.Core.WorkOrders;
namespace CampusOps.Core.Tests.WorkOrders;

public sealed class WorkOrderTests
{
    [Fact] public void AssignedWork_CanStartBlockResumeAndComplete() { var order = NewOrder(); order.MarkAssigned(); order.Start(); order.Block("Awaiting access"); order.Start(); order.Complete(DateTimeOffset.UtcNow); Assert.Equal(WorkOrderStatus.Completed, order.Status); }
    [Fact] public void OpenWork_CannotComplete() => Assert.Equal("work_order.transition_invalid", Assert.Throws<DomainException>(() => NewOrder().Complete(DateTimeOffset.UtcNow)).Code);
    private static WorkOrder NewOrder() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, RequestPriority.High, "Repair leak", DateTimeOffset.UtcNow);
}
