# 01. Stock algebra and boundary conditions

CampusOps tracks parts used by maintenance work orders. Its Part object separates OnHand, Reserved, and Available rather than treating stock as one number. That distinction is the foundation of the inventory workflow. This chapter derives the arithmetic invariants from current methods, traces a realistic sequence, and shows how a small boundary test can expose a serious quantity defect without a large dataset.

Read [Part and PartsService](../../../src/CampusOps.Core/Inventory/Part.cs), [the capacity test](../../../tests/CampusOps.Core.Tests/Inventory/StockCapacityTests.cs), and [the concurrent reservation tests](../../../tests/CampusOps.Infrastructure.Tests/Inventory/ConcurrentReservationTests.cs). Keep object-level rules separate from store-level coordination. A correct arithmetic method can still be used incorrectly by an uncoordinated caller, while a correct store can only preserve the invariants its domain object actually enforces.

## Three quantities describe different facts

OnHand represents the quantity physically held by the modeled inventory. Reserved represents the portion allocated but not yet consumed. Available is derived as OnHand minus Reserved. It is not independently assigned, which prevents one common inconsistency: changing OnHand and forgetting to update a separate Available field.

The constructor requires a nonempty ID, nonblank SKU and name, and nonnegative initial OnHand. SKU is trimmed and uppercased invariantly; Name is trimmed. Reserved starts at zero. The constructor therefore establishes OnHand greater than or equal to zero, Reserved equal to zero, and Available equal to OnHand. It does not establish uniqueness of SKU across the store or verify a supplier's inventory count.

The central object invariant is zero less than or equal to Reserved less than or equal to OnHand, with OnHand within the supported integer range. Available is consequently nonnegative. Write the relationship before memorizing method names. It lets you predict which operations should change physical stock, which merely allocate it, and which guards are required before arithmetic.

## Receive increases physical stock

Receive first requires a positive quantity. It then rejects a quantity greater than int.MaxValue minus OnHand with part.quantity_overflow. Only afterward does it add the quantity. The subtraction-based guard checks capacity before performing the potentially overflowing addition. A rejected receipt leaves all quantities unchanged.

Consider OnHand equal to the maximum integer and Reserved equal to zero. Receiving one is rejected, and the existing capacity test asserts both the error code and unchanged state. This is a small deterministic example: there is no need to allocate billions of objects or execute billions of operations to reach the numeric boundary. The quantity is a value, not a collection size.

Now consider a part with some reservations. Receiving more stock increases OnHand while Reserved stays the same, so Available increases by the received quantity. The method does not fulfill or release a reservation. A procurement or receiving workflow would need to decide how incoming parts relate to outstanding demand; the arithmetic operation alone does not implement that product policy.

## Reserve reallocates availability

Reserve requires a positive quantity and rejects any amount greater than Available with part.stock_insufficient. It increases Reserved without changing OnHand. Therefore Available decreases by the same amount. Because the amount is bounded by OnHand minus Reserved, the new Reserved cannot exceed OnHand under the established invariant.

The object method does not know which work order owns the reserved units. That relationship is represented by PartReservation and coordinated by the store operation introduced in the next chapter. Calling Reserve on a detached Part can produce a valid local quantity state without recording any work-order association. This is why object correctness is necessary but insufficient for application consistency.

Two independent callers reading the same quantity can both believe stock is available unless a shared authority coordinates the check and update. The current InMemoryPartStore provides that authority for its reservation command. Do not attribute the store's lock semantics to Part itself; the object has no synchronization mechanism and can be manipulated directly in tests or snapshots.

## Release and consume have different conservation rules

Release requires a positive quantity no greater than Reserved. It decreases Reserved while leaving OnHand unchanged. Available therefore increases. This represents abandoning an allocation without using the physical stock. Releasing more than reserved throws part.release_invalid before the subtraction.

Consume also requires a positive quantity no greater than Reserved. It decreases both Reserved and OnHand by the same amount. Available remains unchanged because the difference between those two values is preserved. Consumption removes physical stock that had already been unavailable to other work orders. A test expecting Available to decrease again during consumption would misunderstand the model.

Neither method accepts a reservation ID or work-order ID. They protect aggregate quantities, not ownership of a particular allocation. A future persisted release or consume feature must update the matching reservation lifecycle as well as the quantities. The current store interface exposes reservation creation, so do not assume every domain method is already available as a complete stored workflow.

## Worked ledger: filters for two jobs

Start with ten filters on hand, none reserved, and ten available. Reserve four for a heating repair: OnHand remains ten, Reserved becomes four, Available becomes six. Reserve two for another job: the values become ten, six, and four. Consume three from the aggregate reserved quantity: the values become seven, three, and four. Release one: they become seven, two, and five. Receive five: they become twelve, two, and ten.

The ledger teaches several distinctions. Reservation changes availability without changing physical stock. Consumption reduces two fields and preserves availability. Release and receipt both increase availability, but only receipt increases OnHand. If a report shows an availability increase, it cannot infer the business event solely from that derived number.

At this stage the object-level sequence does not prove which job's units were consumed or released. We used work-order names to explain the scenario, but Part methods do not maintain per-job balances. Label that ownership as a proposed application requirement rather than pretending the aggregate object already records it. Later course work will design a reservation lifecycle that can support those claims.

## Prove preservation instead of guessing

Assume the invariant holds before Reserve. Let the requested positive amount be q and Available be OnHand minus Reserved. The guard ensures q is at most that difference. Adding q to Reserved therefore leaves it at most OnHand and still nonnegative. Because OnHand is already bounded by int.MaxValue, the valid sum is also within range. This argument explains why the reserve guard serves both business and arithmetic safety under the initial invariant.

For Consume, q is at most Reserved, which is at most OnHand. Subtracting q from both fields keeps them nonnegative and preserves their ordering. For Release, subtracting at most Reserved keeps it nonnegative and cannot make it exceed unchanged OnHand. Receive increases OnHand within the explicit capacity guard, leaving Reserved unchanged. These short proofs guide boundary cases more effectively than random quantities.

The proofs assume the object begins in a valid state and no concurrent unsynchronized mutation invalidates that assumption during a method. The constructor and private setters help establish controlled state, while store ownership and synchronization address shared access. State the assumptions so the proof is not promoted into a universal concurrency guarantee.

## Guided lab: repair the arithmetic defect safely

The existing compatible starter removes the receive-capacity guard in a copied Part.cs. Generate it into a new absolute directory outside CampusOps using the advanced lab instructions. Predict how receiving one at maximum stock behaves in that deliberately broken copy, then run the bounded harness. The intended result is a behavioral failure, not a compilation failure or an overwritten original source file.

Restore the guard in the disposable copy and rerun. Keep the error code and unchanged-state assertions so a repair that merely throws after overflowing does not pass. Do not replace the entire class with an old snapshot: that can reintroduce unrelated signatures or lose current ownership support. A small compatible repair makes the causal lesson visible.

For an independent extension, write a table-driven local test over zero, one, Available, Available plus one, Reserved, and Reserved plus one where relevant. Use separate valid fixtures for each operation so earlier mutations do not contaminate later cases. Record both accepted transitions and rejected postconditions.

## Independent exercises

**C01.1: Conservation ledger.** Starting with twelve on hand, reserve five, consume two, release one, and receive three. Record all three quantities after each step and explain why Available behaves differently for consume and receive. State what per-work-order ownership information the ledger does not establish.

**C01.2: Boundary proof.** Prove that a valid Reserve cannot overflow Reserved when the invariant holds. Then design one test that distinguishes a pre-addition capacity guard from a guard placed after an overflowing Receive. Include the expected code and unchanged state.

**C01.3: Workflow gap.** Propose a persisted consume command that names a reservation and quantity. List the records it must validate and update together. Explain why directly calling Consume on a retrieved Part is insufficient under the current detached-read store contract.

## Review checkpoint

Your explanation should distinguish physical stock, aggregate allocation, availability, and allocation ownership. Your tests should target the arithmetic boundary and the mutation order, while your proposed consume workflow should acknowledge the additional history relationship. These distinctions provide a precise foundation for the store's stronger same-process commit boundary.
