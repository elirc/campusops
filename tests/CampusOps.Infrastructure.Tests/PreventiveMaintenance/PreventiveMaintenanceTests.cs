using CampusOps.Core.Common;
using CampusOps.Core.PreventiveMaintenance;
using CampusOps.Infrastructure.PreventiveMaintenance;
using CampusOps.Infrastructure.Requests;
namespace CampusOps.Infrastructure.Tests.PreventiveMaintenance;

public sealed class PreventiveMaintenanceTests
{
    [Fact] public async Task GenerateDue_CreatesOneRequestAndAdvancesPlan() { var now = new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero); var plans = new InMemoryMaintenancePlanStore(); var requests = new InMemoryMaintenanceRequestStore(); var plan = new MaintenancePlan(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Replace filter", 30, now.AddMinutes(-1)); await plans.AddAsync(plan, default); var count = await new PreventiveMaintenanceService(plans, requests, new FakeClock(now)).GenerateDueAsync(default); Assert.Equal(1, count); Assert.Single(await requests.ListAsync(default)); Assert.True(plan.NextDueAt > now); }
    private sealed record FakeClock(DateTimeOffset UtcNow) : IClock;
}
