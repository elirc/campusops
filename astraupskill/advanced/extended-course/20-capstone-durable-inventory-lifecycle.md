# 20. Capstone: durable inventory lifecycle

The first capstone extends the current parts authority into a proposed durable lifecycle for reserve, release, consume, and receive commands. It must preserve the existing stock/history relationship, add logical operation identity, and make outcomes discoverable after an interrupted response. This is a feature brief and learning assignment, not a claim that the application already contains durable inventory.

Start with [Part and the inventory interfaces](../../../src/CampusOps.Core/Inventory/Part.cs), [InMemoryPartStore](../../../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs), and the quantity and receipt artifacts from chapters one through six. Work in a newly generated external prototype. Keep the original application, tests, and user changes intact.

## A bounded operational story

A dispatcher reserves filters for a work order. A technician later consumes some of that allocation and releases the remainder. A receiving clerk records newly delivered filters. Each accepted command has a stable identity so an interrupted request can be retried without repeating the stock effect. Every quantity change has matching lifecycle evidence.

The core capstone does not require a new frontend, supplier integration, or warehouse optimization engine. It concerns inventory arithmetic, reservation ownership, durable publication, and outcome discovery. Optional procurement linkage can be added after the core invariants are demonstrated, but should not distract from the exact stock and record relationships.

Choose an implementation milestone explicitly. A same-process model can clarify the command contract and test invariants. A durable milestone requires a real database transaction and fresh-context evidence. A restart milestone crosses a process boundary against retained disposable storage. Report the highest milestone actually executed rather than describing every prototype as durable.

## Proposed records and ownership

Keep Part identity and quantities, but add a persistence version or equivalent concurrency authority. Define a reservation record with original quantity, remaining allocated quantity, consumed quantity, released quantity, and lifecycle status or equivalent append-only events. The representation should make it possible to prove that the same units cannot be consumed and released twice.

Add operation receipts keyed by a stable scoped operation ID. Each receipt records canonical request identity, accepted outcome, relevant record IDs, and committed version information. A matching retry returns the recorded result; conflicting payload reuse rejects. Decide the retry horizon and what happens when the receipt is no longer retained.

Stock movements should identify their source command and reservation where applicable. A receipt for receiving goods should also identify the delivery or receiving operation so duplicate submission does not inflate OnHand. Do not use an unverified free-text note as the only duplicate-control key when the feature promises idempotent receiving.

## State the accounting equations

For each reservation, original allocated quantity equals remaining plus consumed plus released, assuming no separate adjustment feature. Every component is nonnegative. Aggregate Reserved equals the sum of remaining allocations for that part. OnHand includes received physical units minus consumed units and any explicitly modeled adjustments. Available remains OnHand minus Reserved.

These equations extend the creation-only invariant from earlier chapters. Summing all original reservation quantities no longer equals current Reserved after consumption or release. The lifecycle model must preserve enough information to compute the current balance and explain historical movement separately. A report that confuses original allocation with remaining allocation will misstate stock.

Keep adjustments outside the core unless explicitly needed. If introduced, they require reasons, authority, and reconciliation semantics because they can alter physical stock independently of ordinary receipt or consumption. A vague adjustment method can otherwise become a way to bypass every invariant the capstone is meant to enforce.

## Command preconditions and commit sets

Reserve validates positive quantity, current availability, work-order policy, actor authority, and operation identity, then commits stock, allocation, movement evidence, and receipt. Consume validates ownership and remaining allocation before decreasing both OnHand and Reserved and recording consumed quantity. Release decreases Reserved and remaining allocation without changing OnHand. Receive increases OnHand within capacity and records delivery identity and receipt.

All related writes for one command belong in one proposed transaction when the storage permits it. A transaction around only the movement record while stock commits elsewhere does not preserve the invariant. Likewise, a receipt written after an independent commit leaves an unknown-result gap. Draw the commit set for each command before choosing a repository interface.

State which work-order statuses may reserve or consume. The current service checks existence only. A capstone can adopt a stronger policy, but it must specify how a concurrent terminal transition is coordinated or when eligibility is considered accepted. A read outside the transaction does not guarantee eligibility at commit.

## Worked lifecycle

Begin with ten units on hand and none reserved. Reserve six for W under operation R1: Reserved becomes six, Available four, and the allocation has six remaining. Consume two under C1: OnHand eight, Reserved four, Available four, with two consumed. Release one under L1: OnHand eight, Reserved three, Available five, with one released and three remaining. Receive four under G1: OnHand twelve, Reserved three, Available nine.

Retry C1 with identical payload. The state remains twelve on hand and three reserved, and the original consume receipt is returned. Submit C1 with quantity three instead of two and receive a conflict without mutation. Submit a genuinely new consume command C2 for the remaining three and permit it only under the chosen work-order and ownership rules.

This trace distinguishes arithmetic, lifecycle balance, and idempotency. A system can satisfy the first while failing the second or third. The acceptance harness should inspect all three rather than treating a nonnegative Available value as sufficient proof of correctness.

## Concurrency cases are part of acceptance

Two reserve commands competing for the last units must not overallocate. A consume and release command competing for the same remaining reservation must not both spend the same units. Two identical operation IDs arriving concurrently must produce one logical outcome. Two different receiving IDs may both succeed, while the same receiving identity must not duplicate physical stock.

Use controlled schedules or real transactional contention tests appropriate to the milestone. Do not infer cross-process safety from a static lock. For a database implementation, specify expected-version or locking behavior and verify final records through fresh contexts. Conflict handling must preserve intent and distinguish a retryable storage conflict from a business rejection.

Overflow remains a first-class boundary. Receiving beyond supported capacity should reject before any movement or receipt claims success. A rejected oversized command must not leave an orphan movement record. Test the maximum valid quantity and the first invalid quantity without constructing enormous datasets.

## Failure and cancellation outcomes

Inject failures before commit and verify no completed receipt or partial business state becomes visible under the proposed transaction contract. Simulate a lost response after commit and resolve it using the same operation identity. Cancellation before acceptance should leave no effect; cancellation racing with commit must not be reported as proof of rollback without checking the actual outcome.

A durable recovery test should restart a separate process and query the retained receipt and records. If that milestone is not implemented, explicitly state that the prototype establishes only same-process behavior. The course's existing in-memory checks remain baseline evidence, not proof that the new durable design exists.

The operator runbook should explain how to resolve a pending or unknown result without manually editing stock. Reconciliation requires command identity, versions, and lifecycle records. A dashboard button that simply overwrites OnHand would bypass the very evidence needed to determine whether the earlier command committed.

## Independent capstone challenges

**C20.1: Model submission.** Define records, equations, command payloads, actor scope, and receipt policy for all four commands. Include one rejected alternative and the concrete invariant it would violate.

**C20.2: Acceptance harness.** Cover normal lifecycle, insufficient stock, overconsumption, overrelease, overflow, identical retry, conflicting reuse, competing allocation, failed commit, and interrupted response. Name the observer and execution milestone for each case.

**C20.3: Recovery handoff.** Write a runbook for an interrupted consume command. Include receipt resolution, unknown outcome, stale version, and escalation for inconsistent records. Do not advise creating new command IDs until the quantity looks right.

## Review checkpoint

Add a migration boundary to the implementation plan. Existing PartReservation records contain original quantities but no consumption or release lifecycle. A new schema should not invent historical consumption from current aggregate values when several histories could explain them. One defensible teaching migration preserves existing records as opening allocations and records an explicit opening balance after a reviewed reconciliation. Another starts the prototype from new invented data and clearly excludes migration from its implementation claim. Both are more honest than silently treating old records as a complete movement ledger.

The review should also include read contracts. A reservation history query may expose original, remaining, consumed, and released quantities, while an operational stock view reports current aggregate totals. Name those fields so clients cannot confuse historical allocation with current demand. Verify that the two views reconcile under the chosen equations after every accepted command and after an identical retry.

The capstone passes design review when equations, ownership, transaction boundaries, and retry semantics agree. It passes implementation review only at the boundary demonstrated by executed tests. Optional integrations can remain deferred without weakening the completed inventory feature brief, provided its core lifecycle and evidence obligations are explicit.
