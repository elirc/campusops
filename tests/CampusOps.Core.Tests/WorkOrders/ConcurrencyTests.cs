using CampusOps.Core.Common;
using CampusOps.Core.Requests;
using CampusOps.Core.WorkOrders;
namespace CampusOps.Core.Tests.WorkOrders;

public sealed class ConcurrencyTests
{
    [Fact] public void StaleVersion_IsRejected() { var order = new WorkOrder(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, RequestPriority.Normal, "Inspect", DateTimeOffset.UtcNow); order.Assign(Guid.NewGuid()); Assert.Equal("work_order.version_conflict", Assert.Throws<DomainException>(() => order.EnsureVersion(0)).Code); }
}
