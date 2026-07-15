using CampusOps.Core.Common;
using CampusOps.Core.WorkOrders;

namespace CampusOps.Core.Technicians;

public sealed class Technician
{
    private readonly HashSet<string> _skills;
    public Technician(Guid id, string name, IEnumerable<string> skills) { if (id == Guid.Empty || string.IsNullOrWhiteSpace(name)) throw new DomainException("technician.identity_required", "Technician ID and name are required."); Id = id; Name = name.Trim(); _skills = skills.Where(x => !string.IsNullOrWhiteSpace(x)).Select(Normalize).ToHashSet(); }
    public Guid Id { get; }
    public string Name { get; }
    public bool IsActive { get; private set; } = true;
    public IReadOnlyCollection<string> Skills => _skills;
    public bool HasSkill(string skill) => _skills.Contains(Normalize(skill));
    public void Deactivate() => IsActive = false;
    private static string Normalize(string skill) => skill.Trim().ToUpperInvariant();
}
public interface ITechnicianStore { Task AddAsync(Technician technician, CancellationToken token); Task<Technician?> GetAsync(Guid id, CancellationToken token); Task<IReadOnlyCollection<Technician>> ListAsync(CancellationToken token); }
public sealed class AssignmentService(ITechnicianStore technicians, IWorkOrderStore workOrders)
{
    public async Task<WorkOrder> AssignAsync(Guid workOrderId, Guid technicianId, string requiredSkill, CancellationToken token)
    { var technician = await technicians.GetAsync(technicianId, token) ?? throw new DomainException("technician.not_found", "Technician was not found."); if (!technician.IsActive) throw new DomainException("technician.inactive", "Technician is inactive."); if (!technician.HasSkill(requiredSkill)) throw new DomainException("technician.skill_missing", $"Technician lacks required skill {requiredSkill}."); var order = await workOrders.GetAsync(workOrderId, token) ?? throw new DomainException("work_order.not_found", "Work order was not found."); order.Assign(technician.Id); return order; }
}
