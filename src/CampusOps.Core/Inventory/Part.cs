using CampusOps.Core.Common;
namespace CampusOps.Core.Inventory;

public sealed class Part
{
    public Part(Guid id, string sku, string name, int onHand) { if (id == Guid.Empty || string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name)) throw new DomainException("part.details_required", "Part ID, SKU, and name are required."); if (onHand < 0) throw new DomainException("part.quantity_invalid", "Quantity cannot be negative."); Id = id; Sku = sku.Trim().ToUpperInvariant(); Name = name.Trim(); OnHand = onHand; }
    public Guid Id { get; }
    public string Sku { get; }
    public string Name { get; }
    public int OnHand { get; private set; }
    public int Reserved { get; private set; }
    public int Available => OnHand - Reserved;
    public void Receive(int quantity) { Positive(quantity); OnHand += quantity; }
    public void Reserve(int quantity) { Positive(quantity); if (quantity > Available) throw new DomainException("part.stock_insufficient", "Not enough available stock."); Reserved += quantity; }
    public void Release(int quantity) { Positive(quantity); if (quantity > Reserved) throw new DomainException("part.release_invalid", "Cannot release more than reserved."); Reserved -= quantity; }
    public void Consume(int quantity) { Positive(quantity); if (quantity > Reserved) throw new DomainException("part.consume_invalid", "Parts must be reserved before consumption."); Reserved -= quantity; OnHand -= quantity; }
    private static void Positive(int quantity) { if (quantity <= 0) throw new DomainException("part.quantity_invalid", "Quantity must be positive."); }
}
public sealed record PartReservation(Guid Id, Guid WorkOrderId, Guid PartId, int Quantity, DateTimeOffset ReservedAt);
public interface IPartStore { Task AddAsync(Part part, CancellationToken token); Task<Part?> GetAsync(Guid id, CancellationToken token); Task<IReadOnlyCollection<Part>> ListAsync(CancellationToken token); Task<Part?> TryReserveAsync(Guid id, int quantity, CancellationToken token); }
public interface IPartReservationStore { Task AddAsync(PartReservation reservation, CancellationToken token); Task<IReadOnlyCollection<PartReservation>> ListForWorkOrderAsync(Guid id, CancellationToken token); }
public sealed class PartsService(IPartStore parts, IPartReservationStore reservations)
{
    public async Task<PartReservation> ReserveAsync(Guid workOrderId, Guid partId, int quantity, CancellationToken token) { var part = await parts.TryReserveAsync(partId, quantity, token) ?? throw new DomainException("part.stock_insufficient", "Part was not found or stock is insufficient."); var reservation = new PartReservation(Guid.NewGuid(), workOrderId, part.Id, quantity, DateTimeOffset.UtcNow); await reservations.AddAsync(reservation, token); return reservation; }
}
