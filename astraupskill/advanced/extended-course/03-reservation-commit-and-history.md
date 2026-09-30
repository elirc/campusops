# 03. Reservation commit and matching history

The parts store makes a stronger promise than simply preventing a negative quantity. A successful reservation changes aggregate stock and creates a PartReservation record together. A rejected reservation changes neither. This chapter locates the exact authority that enforces that promise, identifies its ordinary failure boundaries, and distinguishes reservation history from the separate work-order audit timeline.

Read [IPartStore and PartsService](../../../src/CampusOps.Core/Inventory/Part.cs), [InMemoryPartStore](../../../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs), and [ConcurrentReservationTests](../../../tests/CampusOps.Infrastructure.Tests/Inventory/ConcurrentReservationTests.cs). The central artifact is a stage ledger. It should let a reviewer predict which values are private candidates, which are published, and which readers can observe the transition.

## Begin at the service boundary

PartsService.ReserveAsync checks cancellation before its other work. It rejects empty work-order or part IDs and nonpositive quantity. It then loads the work order and rejects absence with work_order.not_found. Only after those checks does it call the parts store's combined reservation command using the injected clock instant.

The service verifies work-order existence, not every possible eligibility rule. It does not reject a Completed or Cancelled work order in this method. Whether terminal work should accept new parts is a product policy to define separately. Do not turn a successful existence check into an unsupported statement that the order is active, assigned, or authorized for a particular technician.

The store repeats identity and quantity checks because it is also a public command boundary. A caller can invoke ReserveForWorkOrderAsync directly without going through PartsService. Repeating those local checks protects the adapter contract, while work-order existence remains outside the store because it does not own the work-order collection. This is a useful example of deliberately split responsibilities.

## Preparation happens before publication

Inside the gate, the store checks cancellation and inputs, then locates the current Part. Missing stock or insufficient Available returns null before any record or stock update. For an accepted quantity, it creates an updated snapshot and calls Reserve on that candidate. It constructs a new immutable PartReservation with normalized UTC time.

At this point, the current stored Part still has its old quantities. The prepared snapshot and reservation record are local variables. Domain validation has completed before either publication write. This preparation phase means ordinary invalid-input failures do not require an inverse operation to restore stock; they never changed the stored aggregate.

Publication first adds the reservation record, then replaces the stored part with the prepared candidate. Both statements run under the same gate. Public stock and reservation readers use that gate too, so they cannot read through the store between those writes. The method has no await, external callback, or second store dependency in that interval.

## A precise same-process commit claim

Under ordinary supported execution, participating operations observe either the pre-reservation state or the post-reservation state, not a stock/history mismatch between the two writes. This is the useful commit property of the current in-memory authority. Its scope includes readers and writers using the same instance and gate, which Program arranges for the two interfaces.

Do not claim the two assignments are a hardware transaction or a crash-proof durable commit. Process termination loses the dictionaries. Catastrophic failures such as process abort or resource exhaustion are not recovered by an in-memory rollback log here. The code is designed to eliminate ordinary business-validation and collaborator-failure gaps by preparing first and avoiding external work inside publication.

This nuance strengthens the explanation. It recognizes a real improvement over a service that mutates stock and later calls an unrelated history store, while keeping the guarantee tied to the mechanism that enforces it. A future durable implementation must reproduce the relationship using its own shared transaction or recovery authority.

## Worked stage ledger

Begin with Part P containing eight on hand and zero reserved. Work order W exists. The request asks for three units at a fixed UTC instant. Service validation passes, the store enters the gate, and current Available is eight. The prepared candidate has eight on hand, three reserved, and five available. The prepared record names W, P, quantity three, and the chosen instant.

Before publication, a hypothetical observer outside the gate cannot use the public store API to enter the protected region; it waits. After publication and gate release, GetAsync returns a snapshot with the new quantities, and ListForWorkOrderAsync returns the matching record. The caller's original Part supplied during AddAsync remains unchanged because the store copied it on input.

Now request six additional units. Current Available is five, so the store returns null. No candidate mutation or reservation insertion is published. PartsService translates null into part.stock_insufficient. This code intentionally combines missing part and insufficient stock into that result category at the service boundary; a caller cannot distinguish those two causes from that domain code alone.

## The record is not the work-order timeline

PartReservation records live in InMemoryPartStore and explain inventory allocation. WorkOrderHistoryService writes WorkOrderComment and AuditEvent through a separate history store. These are different histories with different owners and commit behavior. The parts reservation command does not append a work-order AuditEvent in the inspected method.

A product may want the work-order timeline to display a parts reservation. That feature can derive a view from reservation records or append a corresponding event under a defined consistency protocol. Simply adding a callback to a separate history service inside the part store's gate would introduce new failure and latency behavior. The current bounded commit works partly because publication contains no such external action.

If you propose a timeline integration, decide whether a missing timeline event invalidates the reservation or is a delayed projection. A required audit record belongs in the same durable commit set or an explicit delivery mechanism. A best-effort notification can fail independently, but the UI must not mistake its absence for absence of the reservation itself.

## Relationships that remain outside the boundary

Work-order existence is checked before the part-store command. The parts store does not coordinate changes to that work order. If a future workflow deletes or changes eligibility between the check and commit, the reservation command alone does not enforce the broader relationship. The current work-order model has no deletion method in the inspected interface, but terminal status can change through other methods.

The store also does not deduplicate logical reservation requests. Repeating the same work-order ID, part ID, quantity, and timestamp creates a new reservation ID whenever stock allows it. That can be appropriate when the second call represents a genuinely additional allocation. If it represents a network retry, a proposed operation identity is needed to distinguish the two intentions.

SKU uniqueness is likewise outside the dictionary's identity contract. Two different part IDs can share the same normalized SKU unless another boundary prevents it. A report grouped by SKU may then combine or ambiguously display distinct aggregates. Keep this business-key question separate from the guarantee that one accepted reservation updates one selected aggregate and record consistently.

## Guided investigation: observe both sides

In a disposable reference lab, create one part and make successful, insufficient, invalid-quantity, and pre-canceled reservation attempts. For each, query stock and reservation history after the call. Do not inspect only the returned record. The paired observations are what establish the relationship rather than just the method's return type.

Add a second work order and reserve a different quantity for it. Verify that each history query returns only that work order's records while aggregate Reserved includes both allocations. This distinguishes per-order filtering from aggregate accounting. A test that asks only one work order about all records can miss a filtering defect even when total stock looks correct.

Use fixed timestamps so ordering assertions are deliberate. When timestamps tie, the implementation orders by reservation ID. Do not infer that GUID order represents real-time causality. If your test needs a chronological distinction, supply distinct instants; if it tests tie behavior, assert the explicit secondary ordering rather than whichever insertion order happened to occur.

## Independent exercises

**C03.1: Commit ledger.** Write the private-candidate and published-state values for a successful three-unit reservation from eight, then for an insufficient six-unit request. Include the original caller object, stored aggregate, returned snapshot, and per-order history.

**C03.2: Timeline proposal.** Design a work-order timeline projection of parts reservations. Decide whether it is derived, transactionally recorded, or asynchronously delivered. State what a user may infer when the timeline entry is absent and how the chosen design avoids adding an unsafe callback inside the store gate.

**C03.3: Scope review.** List four facts the reservation commit does not establish, including work-order terminal eligibility, logical retry identity, SKU uniqueness, and durability. For each, name a plausible future owner without changing the current guarantee's wording.

## Review checkpoint

For a command requesting exactly Available, success should leave zero available and a matching allocation. A request one unit larger should leave both state sets unchanged. Pairing these cases tests the comparison boundary without introducing concurrent scheduling. It also catches a mistaken greater-than-or-equal rejection that would unnecessarily strand the last available units.

Your explanation should show why the current commit is stronger than two unrelated writes and narrower than a durable transaction. The ledger must distinguish private preparation from public observation, while the relationship map must keep inventory records separate from work-order audit events. Those artifacts will support the cancellation and retry analysis next.
