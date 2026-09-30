using CampusOps.Core.Common;
using CampusOps.Core.WorkOrders;

namespace CampusOps.Core.Inventory;

public sealed class Part
{
    public Part(Guid id, string sku, string name, int onHand)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(sku) || string.IsNullOrWhiteSpace(name))
            throw new DomainException("part.details_required", "Part ID, SKU, and name are required.");
        if (onHand < 0) throw new DomainException("part.quantity_invalid", "Quantity cannot be negative.");
        Id = id;
        Sku = sku.Trim().ToUpperInvariant();
        Name = name.Trim();
        OnHand = onHand;
    }

    public Guid Id { get; }
    public string Sku { get; }
    public string Name { get; }
    public int OnHand { get; private set; }
    public int Reserved { get; private set; }
    public int Available => OnHand - Reserved;

    public void Receive(int quantity)
    {
        Positive(quantity);
        if (quantity > int.MaxValue - OnHand)
            throw new DomainException("part.quantity_overflow", "Received quantity exceeds supported stock capacity.");
        OnHand += quantity;
    }

    public void Reserve(int quantity)
    {
        Positive(quantity);
        if (quantity > Available) throw new DomainException("part.stock_insufficient", "Not enough available stock.");
        Reserved += quantity;
    }

    public void Release(int quantity)
    {
        Positive(quantity);
        if (quantity > Reserved) throw new DomainException("part.release_invalid", "Cannot release more than reserved.");
        Reserved -= quantity;
    }

    public void Consume(int quantity)
    {
        Positive(quantity);
        if (quantity > Reserved) throw new DomainException("part.consume_invalid", "Parts must be reserved before consumption.");
        Reserved -= quantity;
        OnHand -= quantity;
    }

    // All fields are values or immutable strings; callers receive detached state.
    public Part Snapshot() => (Part)MemberwiseClone();

    private static void Positive(int quantity)
    {
        if (quantity <= 0) throw new DomainException("part.quantity_invalid", "Quantity must be positive.");
    }
}

public sealed record PartReservation(Guid Id, Guid WorkOrderId, Guid PartId, int Quantity, DateTimeOffset ReservedAt);

public interface IPartStore
{
    Task AddAsync(Part part, CancellationToken token);
    Task<Part?> GetAsync(Guid id, CancellationToken token);
    Task<IReadOnlyCollection<Part>> ListAsync(CancellationToken token);
    // Stock and the reservation record are one commit, or neither is changed.
    Task<PartReservation?> ReserveForWorkOrderAsync(Guid workOrderId, Guid partId, int quantity, DateTimeOffset at, CancellationToken token);
}

public interface IPartReservationStore
{
    Task<IReadOnlyCollection<PartReservation>> ListForWorkOrderAsync(Guid id, CancellationToken token);
}

public sealed class PartsService(IPartStore parts, IWorkOrderStore workOrders, IClock clock)
{
    public async Task<PartReservation> ReserveAsync(Guid workOrderId, Guid partId, int quantity, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (workOrderId == Guid.Empty || partId == Guid.Empty)
            throw new DomainException("part.reservation_identity_required", "Work order and part IDs are required.");
        if (quantity <= 0) throw new DomainException("part.quantity_invalid", "Quantity must be positive.");
        if (await workOrders.GetAsync(workOrderId, token) is null)
            throw new DomainException("work_order.not_found", "Work order was not found.");
        return await parts.ReserveForWorkOrderAsync(workOrderId, partId, quantity, clock.UtcNow, token)
            ?? throw new DomainException("part.stock_insufficient", "Part was not found or stock is insufficient.");
    }
}
