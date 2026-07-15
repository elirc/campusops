using CampusOps.Core.Common;
using CampusOps.Core.Requests;
namespace CampusOps.Core.Tests.Requests;

public sealed class MaintenanceRequestTests
{
    [Fact] public void Triage_SetsPriorityAndStatus() { var request = NewRequest(); request.Triage(RequestPriority.Emergency); Assert.Equal(RequestStatus.Triaged, request.Status); Assert.Equal(RequestPriority.Emergency, request.Priority); }
    [Fact] public void Triage_Twice_IsRejected() { var request = NewRequest(); request.Triage(RequestPriority.High); Assert.Equal("request.not_submitted", Assert.Throws<DomainException>(() => request.Triage(RequestPriority.Low)).Code); }
    private static MaintenanceRequest NewRequest() => new(Guid.NewGuid(), Guid.NewGuid(), null, "Water leak", "Pipe leaking near sink", DateTimeOffset.UtcNow);
}
