using CampusOps.Core.Requests;
using CampusOps.Core.ServiceLevels;
namespace CampusOps.Core.Tests.ServiceLevels;

public sealed class ServiceLevelTests
{
    [Fact] public void Policy_UsesPriorityTarget() { var opened = DateTimeOffset.UtcNow; var policy = new ServiceLevelPolicy(new Dictionary<RequestPriority, TimeSpan> { [RequestPriority.Emergency] = TimeSpan.FromHours(1) }); Assert.Equal(opened.AddHours(1), policy.DueAt(RequestPriority.Emergency, opened)); }
    [Fact] public void PausedTime_ExtendsDueDateAndPreventsEscalation() { var start = DateTimeOffset.UtcNow; var sla = new WorkOrderServiceLevel(Guid.NewGuid(), start.AddHours(1)); sla.Pause(start.AddMinutes(30)); Assert.False(sla.EscalateIfOverdue(start.AddHours(3))); sla.Resume(start.AddHours(2)); Assert.Equal(start.AddHours(2.5), sla.DueAt); }
}
