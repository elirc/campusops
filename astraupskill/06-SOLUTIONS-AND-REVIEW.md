# Hints, solution reasoning, and a review rubric

Read this after attempting the exercises. The goal is not to copy a finished patch: it is to compare your reasoning against the promises a reviewer should expect. Several designs can satisfy the same contract, but a design that cannot explain its failure behavior is incomplete.

## Receiving quantities

For the capacity exercise, construct a fresh part for each independent boundary. With `OnHand=int.MaxValue-2`, `Receive(2)` should succeed. With the same starting stock, `Receive(3)` should throw `part.quantity_overflow` and leave both stock and reservations unchanged. Reserve one unit before the successful receive and assert `Reserved=1` afterward. Available stock increases by the amount received; reservations are commitments and do not disappear when a delivery arrives.

A useful review question is whether your test could pass if the method assigned an invalid quantity and then threw. If you assert only the exception, yes. Add the state assertion. This pattern applies equally to account balances, quotas, seats, and order totals.

## Work-order lifecycle

A clear first solution loads the order once, rejects missing identity, then rejects terminal statuses before calling the inventory operation. Use a dedicated business code so the UI can explain the rule. Do not test status by string parsing when the domain already exposes an enum.

However, a status check and an inventory write across independently mutable stores leave a race: another operation could complete the order between them. Explain this in the review. A stronger implementation needs an agreed cross-module consistency strategy, such as a database transaction with appropriate concurrency checks, or a workflow policy that defines which operations may proceed after completion. Adding a lock to a scoped service alone does not coordinate other service instances or the work-order transition path.

## Idempotency

Use a compound key with an unambiguous representation, such as a tuple of work-order ID and normalized key. A receipt lookup, payload comparison, stock update, and first receipt creation belong in the same synchronized operation. On a replay, return the stored receipt, including its original timestamp and ID. Generating a new ID for every retry defeats the client-visible identity promise even if the quantity is not charged twice.

Compare all request fields that affect the business operation. A key reused for another part or quantity should not silently return a receipt for a different request. Under contention, assert one stored receipt and one stock effect. A thread-safe dictionary's separate `TryGetValue` and `Add` calls do not automatically make that multi-step algorithm safe.

## Durable storage

A practical database design has a part primary key, nonnegative quantity constraints, reservation identity, and a part foreign key. A conditional stock update can require sufficient available quantity in the update predicate; the affected-row count tells you whether the reservation can proceed. Insert the reservation within the same transaction and commit only when both operations succeed.

Test rollback with a controlled insert failure, not merely a mocked transaction method. For concurrency, use independent connections and the database's actual locking behavior. Decide how transient database contention is reported and retried. Do not copy the in-memory lock into a SQL adapter and claim it coordinates other processes. Document migration and startup selection separately: defining tables does not change dependency-injection registration by itself.

### A compilable skeleton for 4a

You are not being given the answer, only the shape — everything below compiles once you fill the marked gaps, and none of the assertions are written for you.

```csharp
// src/CampusOps.Infrastructure/Data/CampusOpsDbContext.cs — add alongside Locations and Assets
public DbSet<PartRow> Parts => Set<PartRow>();
public DbSet<PartReservationRow> PartReservations => Set<PartReservationRow>();

protected override void OnModelCreating(ModelBuilder builder)
{
    // ...existing Location/Asset configuration...
    builder.Entity<PartRow>().HasKey(x => x.Id);
    builder.Entity<PartRow>().HasIndex(x => x.Sku).IsUnique();
    builder.Entity<PartReservationRow>().HasKey(x => x.Id);
    builder.Entity<PartReservationRow>().HasIndex(x => x.WorkOrderId);
    builder.Entity<PartReservationRow>().HasOne<PartRow>().WithMany().HasForeignKey(x => x.PartId);
}

public sealed class PartRow
{
    public Guid Id { get; set; }
    public required string Sku { get; set; }
    public required string Name { get; set; }
    public int OnHand { get; set; }
    public int Reserved { get; set; }
}

public sealed class PartReservationRow
{
    public Guid Id { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
    public DateTimeOffset ReservedAt { get; set; }
}
```

```csharp
// src/CampusOps.Infrastructure/Inventory/SqlitePartStore.cs
public sealed class SqlitePartStore(CampusOpsDbContext db) : IPartStore, IPartReservationStore
{
    public async Task<PartReservation?> ReserveForWorkOrderAsync(
        Guid workOrderId, Guid partId, int quantity, DateTimeOffset at, CancellationToken token)
    {
        // 1. Validate identity and quantity exactly as InMemoryPartStore does — same codes, same order.

        await using var tx = await db.Database.BeginTransactionAsync(token);

        var affected = await db.Parts
            .Where(p => p.Id == partId && p.OnHand - p.Reserved >= quantity)
            .ExecuteUpdateAsync(set => set.SetProperty(p => p.Reserved, p => p.Reserved + quantity), token);

        if (affected == 0)
        {
            await tx.RollbackAsync(token);
            return null;               // predicate did not match: no part, or not enough available
        }

        // 2. Insert the reservation row, SaveChangesAsync, CommitAsync, and map the row back to PartReservation.
    }

    // AddAsync / GetAsync / ListAsync / ListForWorkOrderAsync: map row <-> Part. Note that you no longer
    // need Snapshot() here — an entity read with AsNoTracking() is already detached, which is the database's
    // version of the same guarantee chapter 02 makes with Snapshot().
}
```

```csharp
// src/CampusOps.Api/Program.cs — replacing lines 48-50 when the durable adapter is selected
builder.Services.AddScoped<SqlitePartStore>();
builder.Services.AddScoped<IPartStore>(p => p.GetRequiredService<SqlitePartStore>());
builder.Services.AddScoped<IPartReservationStore>(p => p.GetRequiredService<SqlitePartStore>());
```

The registration is `AddScoped`, not `AddSingleton`, because a `DbContext` is not thread-safe and must not outlive a request. The "forward both interfaces to one instance" trick is unchanged, but its meaning narrows: with a singleton it meant one store for the whole process; with a scope it means one store, and therefore one transaction and one change tracker, for the duration of one HTTP request. Getting this wrong — registering `SqlitePartStore` twice as scoped, once per interface — gives you two `DbContext`-backed stores in the same request whose writes are in two different transactions. That is the same class of bug this whole course is about, resurfacing one layer down.

What a reviewer will look for in your 4b test: independent `DbContext` instances per task, a result set that distinguishes won / lost-on-predicate / lost-on-lock, an assertion that reconciles `SUM(Quantity)` against `PartRow.Reserved` read through a fresh context, and a sentence admitting that a shared in-memory connection is not a persistence test.

## Browser integration

Separate transport errors from domain rejections. A 400 containing a known business code should produce an actionable message; a network failure may leave the commit outcome uncertain. Once idempotency exists, the same logical retry should reuse the same key. A new user action should receive a new key. Clearing the key on every render or reusing it forever are both incorrect.

A successful reservation affects two visible resources: current stock and reservation history. Refresh both, or explain how your cache update keeps them consistent. Preserve form data on an error. Verify that the production hosting arrangement routes API requests correctly because Vite's development proxy does not exist in exported static assets.

## Review your readiness

Score yourself from zero to two on each dimension: tracing the real code, stating invariants, explaining concurrency, testing failure state, designing stable HTTP behavior, understanding persistence, building usable error states, and reporting evidence honestly. Zero means you need a worked example; one means you can implement with guidance; two means you can explain and defend an independent change. Use weak dimensions to choose the next exercise rather than repeating only comfortable syntax tasks.

A strong final review can answer: who owns the state, what commits together, how retries behave, what a caller can observe, which tests would fail if the promise broke, and what still prevents deployment. Those questions transfer directly to mid-level CRUD application work.
