# Concepts that turn endpoints into dependable features

## An invariant is a promise across every operation

For a part, the core quantity promise is:

```text
0 <= Reserved <= OnHand <= int.MaxValue
Available = OnHand - Reserved
```

Start with ten filters. Reserving four gives `OnHand=10`, `Reserved=4`, `Available=6`. Consuming three gives `OnHand=7`, `Reserved=1`, `Available=6`: consumption decreases both physical stock and its reserved portion. Releasing the last reservation gives `OnHand=7`, `Reserved=0`, `Available=7`. A negative reservation is not a clever way to release stock; it is an invalid operation because it bypasses the meaning and validation of the release command.

[Part](../src/CampusOps.Core/Inventory/Part.cs) guards these rules before mutation. Receiving stock also checks capacity before adding integers. With `OnHand=int.MaxValue`, adding one in ordinary unchecked arithmetic wraps to a negative value. The repaired guard compares `quantity` with `int.MaxValue - OnHand`, which is safe because existing stock is nonnegative. It returns a domain error and leaves the original quantity unchanged. Throwing after assignment would be too late.

## Thread-safe containers do not create a business transaction

Previously, one store changed stock and a second store appended a reservation. A dictionary and queue can each be safe for concurrent calls while the combined business operation remains unsafe:

```text
reserve 2 filters -> stock becomes reserved
history write fails or is cancelled -> no record explains those filters
```

The repair makes the store contract express the complete operation. One private lock protects both inventory and reservation history. Validation and construction happen before publishing either change; there is no awaited operation or user callback between the writes. Readers acquire the same lock. They cannot observe an ordinary intermediate state with only one side committed.

This is a single-process memory guarantee. A second API process has another lock and another dictionary. A database implementation needs one real transaction over both tables, plus a conditional update or concurrency control that prevents overselling. Replacing `Dictionary` with `ConcurrentDictionary` alone would not establish that guarantee. Process termination can still erase the entire teaching store.

## Object ownership matters as much as locks

Suppose `GetAsync` returns the actual `Part` stored internally. A caller can execute `part.Reserve(3)` after the lock ends. The store's synchronization is then bypassed. The repaired adapter copies on insertion and on reads. Changing the original constructor object, a fetched object, or an item in a list changes only that detached snapshot.

`Snapshot()` uses `MemberwiseClone` safely for the current fields: integers, a GUID, and immutable strings. If you later add a mutable `List<Reservation>` field, a shallow clone would share that list. You must revisit copy semantics when the model changes. Do not memorize “clone equals safe”; explain the ownership of every referenced object.

## Cancellation has a boundary

A cancellation token signals that the caller no longer wants to wait. Before a reservation commit, the store checks it and changes nothing if cancellation is already requested. After a successful commit, this implementation returns the receipt without a second cancellation check. Reporting cancellation after committing could encourage the client to retry an operation that already happened.

A lost network response can still make the outcome uncertain. The current reservation API has no idempotency key: repeating the same request can reserve more stock. Atomicity and retry safety are different promises. The exercise sequence adds retry design only after you can explain the existing commit boundary.

## Authentication, authorization, and relationships answer different questions

Authentication asks who supplied a request. Authorization asks what that principal may do. Existence checks ask whether a referenced work order is present. Inventory endpoints now require the existing Operations policy, allowing demo Dispatchers and Managers. Missing demo identity receives 401; a Technician receives 403. Neither result makes caller-controlled headers trustworthy in production.

The service checks work-order existence before touching stock. That prevents an ordinary reservation for an unknown order. It does not prove campus ownership or enforce all order statuses. Parts currently have no campus field, and the work-order store has no deletion operation; if deletion is introduced later, a read-before-write existence check alone will not guarantee a durable foreign-key relationship. Those boundaries belong in design reviews, tests, and deployment decisions.

## Bridge: the same guarantees against a real database

Every guarantee in this chapter is enforced here by one `lock (_gate)` and one `Snapshot()` call, inside one process. Your next job will enforce them with EF Core. The mechanisms are different; the promises are identical. Keep this table next to chapter 03 while you read it.

| In this course | EF Core / SQL equivalent | The trap |
| --- | --- | --- |
| `lock (_gate)` around "validate, build the updated part, build the receipt, publish both" | `await using var tx = await db.Database.BeginTransactionAsync(token); ... await tx.CommitAsync(token);` | A transaction protects the *writes*. It does not stop you making the decision on stale data you read before it started |
| `if (quantity > current.Available) return null;` followed by a write, both inside the lock | `db.Parts.Where(p => p.Id == id && p.OnHand - p.Reserved >= quantity).ExecuteUpdateAsync(...)`, then branch on the affected-row count | Read-then-check-then-update across two statements is the oversell bug. Two connections both pass the check. The predicate must be *in* the `UPDATE` |
| `part.Snapshot()` on every read and every insert, so callers cannot mutate stored state | `AsNoTracking()` on reads; a tracked entity is the database's equivalent of a live reference | An entity you loaded *with* tracking and then mutated will be written by the next `SaveChangesAsync` anywhere in the same scope, even if you decided to reject the request |
| `_parts.TryAdd(...)` throwing `part.already_exists` | A unique index (`HasIndex(x => x.Sku).IsUnique()`), producing `DbUpdateException` with a unique-violation inner exception | Checking "does this SKU exist?" first and inserting second is a race. Let the index decide and translate the exception |
| `token.ThrowIfCancellationRequested()` before the write, never after | The same call, plus passing `token` to every EF `...Async` method | A cancelled request that has already committed must still report success-ish state honestly; cancellation is not rollback |
| "One singleton implementing both interfaces" (`Program.cs:48-50`) | `AddScoped<SqlitePartStore>()` plus two forwarding registrations, so one `DbContext` — one transaction, one change tracker — serves the whole request | Registering the class twice as scoped gives two `DbContext`s in one request, i.e. two transactions. Same bug, one layer down |
| Optimistic concurrency is not modelled at all here | A `byte[] RowVersion` with `[Timestamp]`, or `[ConcurrencyCheck]` on `Reserved`, producing `DbUpdateConcurrencyException` | "Retry on conflict" is a decision, not a default. Decide between reload-and-retry and returning 409 before you write the catch block |

One thing does **not** translate, and it is the reason chapter 03 spends so long on `Snapshot()`: a rolled-back transaction restores the database, not your C# objects. If a request mutated a tracked entity and the save failed, that entity is still mutated in memory, and the scope it lives in must be thrown away. This is why the in-memory store's detached-copy discipline is worth learning on its own terms rather than dismissing as a toy: EF Core gives you the same problem under a different name.

Exercises 4a and 4b make you build this. Exercise 0 is the reason to trust it.
