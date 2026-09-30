# 02. Snapshot ownership and store authority

The current CampusOps parts store owns its mutable Part instances and returns detached snapshots. This is a deliberate boundary that prevents callers from changing stored stock merely by holding a reference. It also combines stock reservation and its PartReservation record inside one locked operation. This chapter traces those choices and explains why dependency registration is part of the consistency design.

Read [InMemoryPartStore](../../../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs), [Part.Snapshot and store interfaces](../../../src/CampusOps.Core/Inventory/Part.cs), [isolation tests](../../../tests/CampusOps.Infrastructure.Tests/Inventory/PartStoreIsolationTests.cs), and [Program](../../../src/CampusOps.Api/Program.cs). Your deliverable is an ownership map that names every mutable object and the authority allowed to change the stored version.

## Copy on input closes one mutation channel

AddAsync acquires the store's gate, checks cancellation, and attempts to add part.Snapshot under the part's ID. The stored object is therefore distinct from the caller's original instance. If the caller later reserves units on the original Part, the store's quantities do not change. The existing isolation test demonstrates exactly that sequence.

Without this input copy, even a store with safe read methods could leak authority through the object supplied during creation. A caller would retain a mutable alias after AddAsync completed. That is why ownership review must inspect both entry and exit boundaries. Copying only on GetAsync does not protect a live object retained by the original creator.

The store also uses TryAdd and rejects duplicate IDs with part.already_exists rather than replacing an existing aggregate. This prevents a caller from resetting stock by adding a different Part under the same identity. It does not enforce unique SKU across different IDs; the dictionary key is the GUID. Keep identity replacement and business-key uniqueness as separate contracts.

## Copy on output closes another channel

GetAsync returns a snapshot when an item exists. ListAsync orders by SKU and returns snapshots in a new array. Mutating either a lookup result or a list member changes only the caller's detached object. A subsequent lookup still returns the stored quantities. The isolation tests check both paths rather than assuming that protecting one protects the other.

Part.Snapshot uses MemberwiseClone. That is sufficient for the current fields because they are value types or immutable strings. It is a shallow copy, so adding a mutable collection or nested mutable object in the future would require reviewing the snapshot contract. A copied reference to a mutable list would reopen an ownership channel even though the Snapshot method still existed and compiled.

Do not generalize this store behavior to all CampusOps modules. Other in-memory stores have their own ownership choices, and some return stored references. A course learner should inspect each boundary rather than conclude that every GetAsync in the repository returns a snapshot. Consistent naming is not proof of consistent alias semantics.

## A snapshot is an observation, not a write handle

Suppose GetAsync returns Part P2 representing stored Part P1 with five available units. The caller invokes Reserve(2) on P2. P2 now has three available, while P1 still has five. There is no SaveAsync on IPartStore to publish P2's arbitrary state. The supported stored reservation path is ReserveForWorkOrderAsync, which reevaluates stock under the store's authority.

This design prevents a stale client snapshot from overwriting newer stock simply by being saved wholesale. It also makes commands more meaningful: callers request a reservation quantity, and the store decides whether it can commit that request. A future receive or consume feature should similarly define an explicit command rather than reintroducing an unrestricted detached-object replacement path without a concurrency contract.

A snapshot can become stale immediately after retrieval. That is normal and does not make it useless. It is suitable for display or preliminary decisions, but the authoritative command must recheck current state. A UI showing five available units cannot guarantee that a later reservation for five succeeds if another request commits first.

## One gate protects stock and records

InMemoryPartStore contains a Dictionary of parts and a Dictionary of PartReservation records behind one private gate. ReserveForWorkOrderAsync checks cancellation and input, locates current stock, rejects missing or insufficient stock with null, creates an updated snapshot, applies the domain reservation to that candidate, and constructs a reservation record. It then adds the record and replaces the stored part while still holding the gate.

There is no await, external callback, or separate store invocation between those two publication writes. Readers using this same store also acquire the gate, so they cannot observe the intermediate state between reservation insertion and part replacement through its public methods. This is a strong and specific same-process consistency property for ordinary supported execution.

The method is not a durable database transaction. Process termination loses both dictionaries, and a catastrophic runtime failure is outside the ordinary business-rejection contract. The code's comment that both changes form one commit should be understood in this in-memory authority and synchronization scope. It does not establish survival across processes or machines.

## Worked trace: two readers and one command

Start with stored stock of five. Reader A obtains snapshot A and reader B obtains snapshot B; both show five available. A submits a reservation command for four. Inside the gate, the store reads current five, prepares a candidate with one available, creates a record, publishes both, and returns the record. B's existing snapshot still shows five because it is detached.

B now submits a command for two. The store does not trust B's stale snapshot. It reads current one under the gate and returns null. PartsService maps that result to part.stock_insufficient. No second reservation record is appended. The stored state remains one available and four reserved, with the first record explaining that allocation.

The stale display did not violate the stock invariant because the command boundary revalidated current state. This is the central benefit of separating observation objects from mutation authority. A proposed client should handle the command rejection gracefully rather than assume its earlier display reserved the stock implicitly.

## Both interfaces must share one instance

The store implements IPartStore and IPartReservationStore. Program registers one concrete InMemoryPartStore singleton, then resolves both interfaces through that same concrete instance. This wiring is essential: the stock command and reservation-history query must reach the same dictionaries and gate.

If the interfaces were independently registered as two singleton instances, each singleton would have its own state. Reserving through IPartStore could succeed while querying through IPartReservationStore returned an empty history from another object. The word singleton alone does not prevent that mistake; it means one instance per registration relationship, not one global instance for every interface a class implements.

A composition test can resolve both interfaces from the configured service provider and verify shared identity or shared observable state. A unit test that manually passes one store instance to both variables cannot prove the application registration is correct. This is an example of an invariant whose enforcement includes dependency wiring, not only domain and adapter code.

## Reservation records expose safer values

PartReservation is a record containing GUIDs, an integer quantity, and a DateTimeOffset. The store normalizes the supplied reservation instant to UTC. History lookup filters by work-order ID and orders by ReservedAt then reservation ID. The returned array contains immutable record values under the current property definitions, so callers cannot mutate stored quantities through those records in the same way as through a mutable Part.

The ordering provides deterministic tie-breaking when timestamps match, but it does not prove real-world causal order. A GUID tie-breaker is an ordering choice, not an event sequence number. If the product needs a durable per-work-order sequence, that should become an explicit feature with its own concurrency contract. Avoid inferring causality from a sorted list alone.

The record also does not carry a lifecycle status for later release or consumption. It represents a reservation event or allocation record as currently defined. A future reservation lifecycle needs to decide whether records remain append-only evidence, gain explicit state, or are paired with additional events. That design must preserve the stock/history relationship introduced here.

## Guided lab and independent exercises

Use the compatible starter to observe input aliasing, lookup aliasing, list aliasing, duplicate replacement, and canceled-add defects. Repair only the copied source and retain the current method signatures. The reference behavior requires detached reads and writes, rejection of replacement, and cancellation before insertion. Do not make tests pass by removing the caller mutation that exposes an alias.

**C02.1: Ownership map.** Draw the caller's original Part, the stored Part, two lookup snapshots, and a list snapshot. Label which mutations can affect which objects. Then add a proposed mutable Tags collection and explain why the current shallow clone would require review.

**C02.2: Composition failure.** Design a test that detects two independently registered InMemoryPartStore instances behind the two interfaces. State why a manually constructed single-store unit test cannot catch the registration defect and what shared observation should be asserted.

**C02.3: Stale reader.** Reproduce the five-unit, four-unit, two-unit sequence above with two snapshots. Assert both snapshots, final stored quantities, successful record count, and failed-command result. Explain why stale display data is compatible with a correct authoritative command boundary.

## Review checkpoint

Your ownership map should identify both copy boundaries and the single publication authority. Your explanation should preserve the store's achieved same-process consistency while explicitly excluding durability and cross-process coordination. That combination of precision and restraint will guide the more complex CampusOps workflows in the continuation.
