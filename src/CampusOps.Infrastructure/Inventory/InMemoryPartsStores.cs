using CampusOps.Core.Common;
using CampusOps.Core.Inventory;

namespace CampusOps.Infrastructure.Inventory;

// Both interfaces must resolve to this SAME singleton instance (see Program.cs).
public sealed class InMemoryPartStore : IPartStore, IPartReservationStore
{
    private readonly object _gate = new();
    private readonly Dictionary<Guid, Part> _parts = new();
    private readonly Dictionary<Guid, PartReservation> _reservations = new();

    public Task AddAsync(Part part, CancellationToken token)
    {
        lock (_gate)
        {
            token.ThrowIfCancellationRequested();
            if (!_parts.TryAdd(part.Id, part.Snapshot()))
                throw new DomainException("part.already_exists", "A part with this ID already exists.");
        }
        return Task.CompletedTask;
    }

    public Task<Part?> GetAsync(Guid id, CancellationToken token)
    {
        lock (_gate)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(_parts.TryGetValue(id, out var part) ? part.Snapshot() : null);
        }
    }

    public Task<IReadOnlyCollection<Part>> ListAsync(CancellationToken token)
    {
        lock (_gate)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyCollection<Part>>(_parts.Values.OrderBy(x => x.Sku).Select(x => x.Snapshot()).ToArray());
        }
    }

    public Task<PartReservation?> ReserveForWorkOrderAsync(Guid workOrderId, Guid partId, int quantity, DateTimeOffset at, CancellationToken token)
    {
        lock (_gate)
        {
            // Cancellation is honored before commit, never reported after mutation.
            token.ThrowIfCancellationRequested();
            if (workOrderId == Guid.Empty || partId == Guid.Empty)
                throw new DomainException("part.reservation_identity_required", "Work order and part IDs are required.");
            if (quantity <= 0) throw new DomainException("part.quantity_invalid", "Quantity must be positive.");
            if (!_parts.TryGetValue(partId, out var current) || quantity > current.Available)
                return Task.FromResult<PartReservation?>(null);

            var updated = current.Snapshot();
            updated.Reserve(quantity);
            var reservation = new PartReservation(Guid.NewGuid(), workOrderId, partId, quantity, at.ToUniversalTime());
            // Prepare and validate first. No await, callback or external store between writes.
            _reservations.Add(reservation.Id, reservation);
            _parts[partId] = updated;
            return Task.FromResult<PartReservation?>(reservation);
        }
    }

    public Task<IReadOnlyCollection<PartReservation>> ListForWorkOrderAsync(Guid id, CancellationToken token)
    {
        lock (_gate)
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyCollection<PartReservation>>(_reservations.Values
                .Where(x => x.WorkOrderId == id).OrderBy(x => x.ReservedAt).ThenBy(x => x.Id).ToArray());
        }
    }
}
