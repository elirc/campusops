# Worked solutions and review: stock and ownership

## C01.1: Quantity conservation

Starting with twelve on hand and zero reserved gives twelve available. Reserving five produces twelve, five, and seven. Consuming two produces ten, three, and seven. Releasing one produces ten, two, and eight. Receiving three produces thirteen, two, and eleven. The order of each triple is OnHand, Reserved, Available.

Consumption preserves Available because the consumed units had already been removed from availability when reserved. Receiving increases physical stock without changing allocation, so Available rises. Release increases availability without increasing physical stock. A report observing only Available cannot distinguish release from receipt without additional event information.

The aggregate ledger does not establish which work order owned the consumed or released units. Part methods accept quantities only. A correct answer can use job names to explain a scenario, but it must not claim that those names are represented in the Part object. Ownership requires the reservation records and a coordinating workflow.

## C01.2: Bounds and mutation order

Assume zero is less than or equal to Reserved and Reserved is less than or equal to OnHand, which is at most int.MaxValue. The Reserve guard permits positive q only when q is at most OnHand minus Reserved. Therefore Reserved plus q is at most OnHand and cannot exceed int.MaxValue. This proof depends on the initial invariant and on no unsynchronized mutation changing the fields during the reasoning.

For Receive, use OnHand equal to int.MaxValue and request one. The correct method throws part.quantity_overflow before addition and preserves OnHand, Reserved, and Available. A broken method that adds first can corrupt stock before discovering the boundary. An assertion of the exception alone does not distinguish it. The existing compatible starter deliberately removes the guard so the lab can expose the arithmetic consequence in copied source.

An alternative test begins one below the maximum and receives one successfully, then rejects another one. This pairs the maximum valid transition with the first invalid transition. It is more informative than several ordinary small additions because it checks the exact supported capacity boundary.

## C01.3: A proposed consume command

A proposed stored command can accept reservation ID, quantity, and a stable operation ID if retry replay is required. The authority loads the reservation's remaining allocation and its Part, verifies the quantity is positive and does not exceed that reservation's remaining balance, and validates the aggregate quantities. It then decreases OnHand and Reserved and records the reservation's consumed amount or a corresponding event within the same commit boundary.

The current PartReservation has no remaining-balance or lifecycle field, so the proposal requires a model decision. An append-only consumption event stream and a mutable reservation balance are different possible designs. Either must prevent the same reserved units from being consumed twice and must keep aggregate stock consistent with reservation history.

Calling Consume on a GetAsync result only changes a detached snapshot. There is no general save operation to publish it, and even adding one would not automatically coordinate reservation ownership. The explicit command should operate at the authority that owns both state sets. A same-process lock can establish a bounded prototype; durable implementation requires its own storage evidence.

## C02.1: Alias map

The caller's original Part is O. AddAsync stores snapshot S. GetAsync returns A and B as separate snapshots, while ListAsync returns another snapshot L inside a new array. Mutating O, A, B, or L does not mutate S under the current value-field design. A store command reads and replaces S with a new prepared snapshot while holding the gate.

If Part gains a mutable Tags list and Snapshot remains a shallow clone, O and S can share the same list reference after input copying. A caller adding a tag could then affect stored state without invoking a store command. The proposed change needs a deep copy of that mutable member, an immutable collection design, or another ownership strategy. The existence of a Snapshot method is not enough; its adequacy depends on the fields it copies.

## C02.2: Composition evidence

A composition test resolves IPartStore and IPartReservationStore from the application's configured provider. It can assert shared identity for the concrete implementation or add and reserve through the stock interface and observe the matching record through the history interface. With independent store instances, the latter query sees a different reservation dictionary and fails the expected observation.

A unit test that assigns one manually created store to both interface variables proves the implementation can support both roles. It does not verify Program's wiring. Both tests can be useful, but they answer different questions. The actual Program factories resolve one registered InMemoryPartStore for both interfaces, which is the current source evidence for correct composition.

## C02.3: Stale snapshots and current authority

Both snapshots initially show five available and zero reserved. The first command reserves four and returns a non-null record. The stored state becomes five on hand, four reserved, and one available. Both old snapshots still show five available. The second command for two returns null because it reevaluates the stored one available under the gate.

History contains exactly the first record with quantity four, and final stored stock matches it. PartsService would translate the null into part.stock_insufficient, while a direct store test asserts null according to the store contract. The stale snapshots are not corruption; they are earlier observations. Correctness comes from the authoritative command refusing to trust them as current state.

## Review rubric

Score each chapter out of ten: three points for accurate arithmetic or ownership reasoning, two for a complete worked trace, two for assertions that catch a plausible defect, two for separating current behavior from proposed workflow, and one for a specific scope limitation. Deduct accuracy points when a solution treats a snapshot as a write handle or claims that aggregate Reserved alone identifies a work-order owner.

Retain the quantity ledger, invariant proof, alias diagram, and composition-test design. Mark any test you did not execute as a design. The existing advanced lab's recorded results are useful baseline evidence, but these new independent exercise extensions do not become executed merely because their expected outcomes are described here.
