# 06. Concurrency and observable inventory invariants

The parts store's single gate coordinates stock and reservation history for all its public operations. The existing concurrency test launches many contenders against a small stock quantity and checks both successful results and stored evidence. This chapter explains why that test is useful, what its source mechanism establishes, and which stronger or different claims require additional experiments.

Read [InMemoryPartStore](../../../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs), [ConcurrentReservationTests](../../../tests/CampusOps.Infrastructure.Tests/Inventory/ConcurrentReservationTests.cs), and [Program's shared registrations](../../../src/CampusOps.Api/Program.cs). Your deliverable is a concurrency argument with named participants, an invariant, an observation boundary, and a reproducible test plan.

## State the invariant before counting tasks

For a fixture that begins with zero reserved and only performs successful reservation creation, aggregate Reserved should equal the sum of quantities in its reservation records. Available should remain nonnegative. Each successful call should have one distinct returned record present in history. Rejected calls should not consume stock or append records.

The fixture restriction matters. A future release or consume feature could change aggregate Reserved while retaining historical creation records, so summing all historical reservation quantities would no longer equal current allocation. That would require a lifecycle-aware balance calculation. Do not elevate an invariant from a creation-only test into a permanent accounting identity for every future workflow.

The current record and command model make this restricted invariant appropriate. The test uses one part, one work order, one-unit commands, and five units on hand. Sixty-four competing attempts can produce at most five accepted reservations. The expected successful count is also at least five in the absence of cancellation or unexpected faults because enough valid contenders are supplied.

## What the existing test observes

The test uses a shared start signal and Task.Run to let many attempts contend. After all complete, it verifies five non-null results, five history records, distinct reservation IDs, matching quantity sum and Reserved, zero Available, and the fixed timestamp on all records. It also checks that the caller's original Part still has its initial availability.

These assertions jointly cover capacity, record publication, identity, time propagation, and input isolation under that workload. A test checking only five successes could miss extra history records. A test checking only final Reserved could miss a successful result with no matching record. The paired observations make the concurrency test more discriminating than a raw no-exceptions assertion.

The test is still a bounded workload, not a proof of every possible machine schedule. The source's lock and copy boundaries provide the structural argument that complements the execution. A sound review uses both: the mechanism explains why participating operations serialize, and the experiment verifies representative behavior and catches implementation regressions.

## Identify the publication authority

All reads and writes in InMemoryPartStore acquire the same gate. Each accepted command sees the latest published Part after entering, prepares its candidate, and publishes record and stock before releasing the gate. Another command cannot base its decision on an old internal object read outside that authority because the method performs its own lookup inside the gate.

Callers can hold stale snapshots, but they cannot publish those snapshots through IPartStore. This is important to the concurrency proof. If a general SaveAsync later allowed arbitrary snapshots to replace stock without a version check, a caller could overwrite newer reservations and break the invariant despite the reservation method's lock. Review new interface methods against the existing authority model.

The gate is global to this store instance, not keyed by part ID. Reservations for unrelated parts therefore serialize through the same critical section. That simplifies consistency and avoids multi-lock coordination in the teaching implementation. It may limit throughput at larger scale, but performance changes should preserve the relationship rather than removing the gate based on an unmeasured concern.

## A proposed per-part optimization has obligations

Suppose a future design uses a lock per part to allow unrelated reservations concurrently. Stock decisions can then be independent, but the shared reservation collection still needs safe publication and reading. A per-work-order history query may span records from several parts. The design must define whether it returns a consistent snapshot or a best-effort collection while those commands commit.

Lock identity must also remain stable. Creating a new lock object for each call provides no exclusion even if every call uses the same part GUID. Removing a lock entry while another waiter still references it can create two live locks for one part. A lock registry needs a lifecycle strategy or another stable authority, not merely a dictionary of convenient objects.

Do not implement that optimization as part of a correctness exercise unless you can state its benefit and test its obligations. A simple global gate can be the right design for a small in-memory teaching store. The course's purpose is to identify the tradeoff, not to reward complexity for its own sake.

## Work-order validity is another resource

PartsService checks that a work order exists before entering the parts authority. A work-order action can change its status through a separate live object and store. The inventory gate does not coordinate that status. If the product later requires that terminal work orders cannot receive parts, that rule spans two authorities and needs a shared decision mechanism or a clearly defined observation policy.

For example, a reservation can read an InProgress order, pause, and then commit after another operation cancels the order. The current service has no status check, so this is first a missing product policy rather than a violated implemented check. A proposed active-order check outside the gate would still leave a race unless the policy allows eligibility to be decided at that earlier point.

This is how concurrency analysis should proceed: establish the actual contract, then evaluate whether the mechanism enforces it. Do not accuse the current store of violating a rule it never promised, and do not claim that adding one read enforces a stronger rule under overlap. Both errors come from skipping the policy definition.

## Deterministic tests and workload tests differ

A start gate creates contention without dictating the exact order of every attempt. That is useful for the current capacity test because the invariant should hold regardless of winner order. For a specific stale-observation race, use a more precise gate at the relevant read boundary so both participants reach the intended state before either proceeds.

Avoid tests that depend on a particular reservation GUID winning or on Thread.Sleep producing an overlap. The winner is not part of the product rule in the five-unit fixture. Assert the set-level invariant and per-record consistency instead. A deterministic test should control only the order needed to distinguish the behavior, leaving irrelevant scheduling unconstrained.

Bound every artificial wait. If a changed implementation rejects before reaching a gate, a barrier expecting two arrivals can otherwise hang. The failure report should name the missing event, making it clear that the experiment setup no longer matches the code path. A timeout is a diagnostic boundary, not a substitute for explaining the schedule.

## Same-process is not cross-process

The gate and dictionaries exist in one InMemoryPartStore instance. Program ensures its two interfaces share that instance within the API host. Another process has another memory space and another store unless a durable shared authority is introduced. Running two hosts does not automatically produce a shared warehouse merely because both use singleton registrations.

If a future database stores inventory, concurrency must be enforced at the database transaction or another shared coordination boundary. An in-process lock can still reduce local contention, but it cannot exclude another process's database writes. The durable capstone must test the actual shared authority rather than reuse the in-memory result as proof.

The current readiness response explicitly describes non-durable defaults. Preserve that honesty in your concurrency report. A same-process inventory invariant is a real achievement, and it remains useful even when a larger deployment needs another mechanism. Clear scope prevents both overclaiming and unfairly dismissing the existing design.

## Independent exercises

**C06.1: Test anatomy.** Map each assertion in the competing-reservation test to a defect it could catch. Identify one defect a success-count-only test would miss and one deployment claim the whole test still cannot establish.

**C06.2: Optimization review.** Propose a per-part coordinator and explain how reservation insertion, history queries, and lock identity remain safe. State the workload benefit you would measure before adopting it and the invariant you would retain unchanged.

**C06.3: Cross-authority policy.** Define whether a reservation may commit after its work order becomes terminal. Give a schedule and enforcement owner for your chosen rule. Distinguish a new product policy from the current existence-only check.

## Review checkpoint

History ordering is not a contention winner log. The store sorts records by supplied reservation time and then ID, while callers can supply equal or earlier instants. A test should compare the accepted record set and quantities rather than infer lock acquisition order from the returned list. If operational sequencing becomes a requirement, introduce an authoritative sequence explicitly instead of overloading timestamps.

Your argument should combine the source mechanism with bounded execution evidence and a clear deployment scope. It should preserve the stock/history relationship under any allowed winner order, while recognizing that work-order policy and durable sharing are separate concerns. This prepares you to inspect the weaker and different version boundary in work orders next.
