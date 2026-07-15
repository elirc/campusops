using CampusOps.Core.Technicians;
namespace CampusOps.Core.Tests.Technicians;

public sealed class TechnicianTests
{
    [Fact] public void SkillMatching_IsCaseInsensitive() { var technician = new Technician(Guid.NewGuid(), "Sam", [" electrical "]); Assert.True(technician.HasSkill("Electrical")); }
    [Fact] public void Deactivate_PreventsActiveAssignmentState() { var technician = new Technician(Guid.NewGuid(), "Sam", ["HVAC"]); technician.Deactivate(); Assert.False(technician.IsActive); }
}
