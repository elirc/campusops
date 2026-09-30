# Practice: progress from a small rule to feature ownership

These exercises are proposed learner changes. They are not part of the delivered runtime behavior unless the worked-change chapter explicitly says so. Work on a branch or copy, keep each exercise reviewable, and write the acceptance contract before implementation. Use [the solution guide](06-SOLUTIONS-AND-REVIEW.md) after making an initial attempt.

## Exercise 0: run the signature-compatible disposable starter

Use the [advanced failure lab](advanced/03-FAILURE-LAB.md) before studying the worked repair. Its generator copies current source into a new absolute output directory outside the project and introduces bounded behavioral defects without changing constructor or interface signatures.

Do not overwrite live source with the historical snapshots. They remain useful for reading the original change, but are not a compiling exercise baseline. The generator rejects existing output directories and changed source anchors. Edit only the generated starter, record its failing checks, repair one defect at a time, and compare with a separately generated reference lab.

The new harness has its own documented result counts; they are not counts from the application's old full test suite. [Follow the complete advanced route](advanced/README.md) for persistence failure, lifecycle, concurrency, exercises and a separate solution rubric.

## 1. Add a receiving boundary case

Add tests for receiving exactly the remaining supported capacity and for attempting one unit beyond it. Start with `OnHand=int.MaxValue-2`. Receiving two should reach the maximum. Starting from the same initial value, receiving three should reject without changing stock. Include a reservation before receiving and explain why receiving should not change `Reserved`.

Acceptance: the tests assert all three quantities and the stable rejection code. Do not add a second production guard if the existing rule already satisfies the examples. Your deliverable can be tests plus a short explanation; useful engineering is not measured by the number of runtime lines changed.

## 2. Define a lifecycle rule for reservations

Propose this contract: completed and cancelled work orders cannot receive new part reservations. Review [WorkOrderStatus](../src/CampusOps.Core/WorkOrders/WorkOrder.cs), then enforce the decision in the application use case. Open, assigned, in-progress, and blocked orders remain eligible under this exercise's policy.

Acceptance: one allowed case, both terminal cases, no stock/history mutation after rejection, and an HTTP test showing the business code. Explain whether checking a mutable work order before a separate inventory commit protects against a simultaneous status transition. Your first implementation may deliberately remain a single-process teaching solution, but document that race rather than claiming a cross-module transaction.

## 3. Design safe retries before implementing them

A dispatcher clicks Reserve, the server commits, and the response disappears. The current API can reserve again on retry. Add a caller-supplied idempotency key scoped to the work order. Store the key, normalized request identity, and original receipt inside the same reservation commit boundary.

Acceptance: same work order, key, part, and quantity returns the original receipt with no second stock effect; changed part or quantity under the same scoped key rejects; the same key on another work order is independent; competing retries produce one record. Decide how the HTTP contract signals a replay. Empty keys and whitespace normalization need explicit rules. Do not implement deduplication as a separate lookup followed by an unlocked write.

## 4a. A durable adapter that commits both effects (one session)

This is the first half of the old exercise 4. Stop when it works; 4b is a separate sitting.

Model two tables on `CampusOpsDbContext`, which today knows only about `LocationRow` and `AssetRow`. Follow that file's conventions exactly: a `DbSet`, a `HasKey`, a unique index where the domain has a natural key, and a `HasOne<...>().WithMany().HasForeignKey(...)` for the relationship.

- `PartRow` — `Id` (key), `Sku` (unique index), `Name`, `OnHand`, `Reserved`.
- `PartReservationRow` — `Id` (key), `WorkOrderId`, `PartId` (FK to `PartRow`), `Quantity`, `ReservedAt`. Index `WorkOrderId`, because `ListForWorkOrderAsync` filters on it.

Then write `SqlitePartStore : IPartStore, IPartReservationStore` implementing the same five methods. The only interesting one is `ReserveForWorkOrderAsync`. The rule from chapter 02 — *validate everything, then commit both effects together* — survives the move to a database, but the mechanism changes: the `lock (_gate)` is replaced by a **conditional update**, so the check and the write are one statement that the database evaluates atomically.

```csharp
public async Task<PartReservation?> ReserveForWorkOrderAsync(
    Guid workOrderId, Guid partId, int quantity, DateTimeOffset at, CancellationToken token)
{
    if (workOrderId == Guid.Empty || partId == Guid.Empty)
        throw new DomainException("part.reservation_identity_required", "Work order and part IDs are required.");
    if (quantity <= 0)
        throw new DomainException("part.quantity_invalid", "Quantity must be positive.");

    await using var tx = await db.Database.BeginTransactionAsync(token);

    // Check and write in ONE statement. Never: read Available, decide in C#, then UPDATE.
    var affected = await db.Parts
        .Where(p => p.Id == partId && p.OnHand - p.Reserved >= quantity)
        .ExecuteUpdateAsync(set => set.SetProperty(p => p.Reserved, p => p.Reserved + quantity), token);

    if (affected == 0)
    {
        await tx.RollbackAsync(token);
        return null;                      // insufficient stock, or no such part — same contract as the in-memory store
    }

    var row = new PartReservationRow { /* Id, WorkOrderId, PartId, Quantity, ReservedAt = at.ToUniversalTime() */ };
    db.PartReservations.Add(row);
    await db.SaveChangesAsync(token);
    await tx.CommitAsync(token);
    return /* map row -> PartReservation */;
}
```

Two things to look at hard. `affected == 0` is the database telling you the predicate did not match; it is not an error, and it must not be retried blindly, because "someone else reserved the last unit" and "that part does not exist" both land here — decide whether your contract needs to distinguish them, as the in-memory store does *not*. And `ExecuteUpdateAsync` bypasses the change tracker, so any `PartRow` you already loaded in this `DbContext` is now stale; do not read stock from it after this call.

Registration is the other half of the change: `Program.cs:48-50` currently registers the concrete `InMemoryPartStore` once and forwards both interfaces to it. Your durable adapter needs the same treatment (one scoped registration, both interfaces forwarded) — with the difference that a `DbContext` is scoped, not singleton, so "the same instance for both interfaces" now means *within one request*.

**Acceptance (4a):** reserving more than `Available` changes neither table and returns null; a deliberately failed reservation insert (violate a constraint, or throw before `CommitAsync`) leaves `Reserved` at its original value when read through a **new** `DbContext`; `ListForWorkOrderAsync` returns what you wrote; schema creation or migration instructions are written down; the DI registration explicitly selects the durable adapter and the existing 42 tests still pass with the in-memory adapter selected.

## 4b. Prove two connections cannot oversell (one session)

Now attack it. `ConcurrentReservationTests.CompetingReservations_CommitMatchingStockAndHistory` races 64 tasks against 5 units through one in-process lock. Repeat that shape against SQLite, and be honest about what you are proving.

- Use one `SqliteConnection("Data Source=file:campusops-test?mode=memory&cache=shared")` held open for the test's lifetime, or a temp file. A closed connection to an in-memory SQLite database destroys it.
- Give each racing task its **own** `DbContext` over that connection. Sharing one `DbContext` across threads proves nothing about the database and will throw its own concurrency exception, which is a different and misleading red.
- Assert the same reconciliation the in-memory test asserts: exactly 5 winners, exactly 5 reservation rows, distinct reservation IDs, and `SUM(Quantity) == PartRow.Reserved` read back through a fresh context.
- Expect `SQLITE_BUSY`. SQLite allows one writer at a time; without `PRAGMA busy_timeout` (or a retry loop) some tasks will fail with "database is locked" rather than with "insufficient stock". Decide which of those two your API should surface and say so — a 409 and a 503 are different promises.

**Acceptance (4b):** a test that fails if you replace the conditional update with read-then-write (try it — that is the point of the exercise), plus a paragraph distinguishing three outcomes your racing tasks can see: won, lost on the predicate, lost on a lock. Also state plainly what a shared open in-memory connection does **not** prove: nothing here shows the data survives a process restart.

## 5. Build the inventory UI capstone

Add an inventory list, a create-part form, and a reservation form for an existing work order. Reuse the actual HTTP contracts and provide loading, empty, validation-error, forbidden, and retry states. Keep numeric input as text until parsing at a clear boundary so that an empty input does not silently become zero. On a successful mutation, refresh both stock and history or update a coherent cache.

Acceptance: a user can create a part, inspect its quantities, reserve valid stock, see the history, and understand a rejected over-reservation. Preserve entered values after a recoverable error. Add accessible labels and keyboard operation. Test at least one browser flow against a disposable API instance; component mocks alone cannot prove the proxy, headers, and server agree.

End with a short reviewer note: user-visible result, invariants, failure behavior, evidence, and remaining deployment requirements. Explain how authentication and campus ownership would be established before using the feature with real campus data.
