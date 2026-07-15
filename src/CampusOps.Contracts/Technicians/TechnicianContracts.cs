namespace CampusOps.Contracts.Technicians;

public sealed record CreateTechnicianRequest(string Name, IReadOnlyCollection<string> Skills);
public sealed record AssignTechnicianRequest(Guid TechnicianId, string RequiredSkill);
public sealed record TechnicianResponse(Guid Id, string Name, IReadOnlyCollection<string> Skills, bool IsActive);
