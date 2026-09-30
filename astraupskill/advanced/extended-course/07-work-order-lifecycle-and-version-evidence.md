# 07. Work-order lifecycle and version evidence

WorkOrder has an explicit lifecycle and a Version property. Those features support useful local rules, but their presence does not by itself create an atomic compare-and-update operation. This chapter reconstructs the actual state machine, reads the existing stale-version test precisely, and proposes a stronger command boundary without mistaking it for current behavior.

Read [WorkOrder and WorkOrderService](../../../src/CampusOps.Core/WorkOrders/WorkOrder.cs), [the in-memory work-order store](../../../src/CampusOps.Infrastructure/WorkOrders/InMemoryWorkOrderStore.cs), [lifecycle tests](../../../tests/CampusOps.Core.Tests/WorkOrders/WorkOrderTests.cs), and [the version test](../../../tests/CampusOps.Core.Tests/WorkOrders/ConcurrencyTests.cs). Keep a transition ledger beside the source; the file's compact formatting makes it especially important to separate guards from assignments.

## Constructing an order establishes limited facts

The constructor requires nonempty work-order and request IDs. It assigns location, optional asset, priority, title, and a UTC-normalized opening instant. It does not independently validate that the location exists, that an asset belongs there, or that the title is nonblank. Conversion from a validated request supplies useful values, but direct construction has a narrower validation contract.

Status starts Open, Version starts zero, and no technician or completion timestamp is assigned. These defaults define the initial local state. They do not prove a request was actually converted in another store. As with inventory ownership, a relationship across records requires orchestration beyond one constructor.

A test may construct an order directly to isolate lifecycle behavior. That is legitimate, but it should not claim a complete request-conversion journey. Record which preconditions the fixture supplies manually and which business steps it bypasses. This makes later failure tests easier to interpret.

## Trace every accepted transition

Assign requires Open and a nonempty technician ID. It sets TechnicianId, moves to Assigned, and increments Version. Start accepts Assigned or Blocked, moves to InProgress, clears BlockedReason, and increments Version. Block requires InProgress and nonblank reason, trims the reason, moves to Blocked, and increments Version.

Complete requires InProgress, moves to Completed, records the supplied instant in UTC, and increments Version. It does not compare completion time to OpenedAt. Cancel rejects only Completed and Cancelled, so it can cancel Open, Assigned, InProgress, or Blocked work. It increments Version but does not clear TechnicianId or BlockedReason in the current method.

These details belong in the transition table even when a different product policy seems more intuitive. A canceled blocked order can retain the reason that blocked it; that may be useful history rather than a defect. If the UI interprets any non-null BlockedReason as current blockage, the response contract or presentation needs to distinguish status from retained data.

## Worked lifecycle and versions

An order begins Open at version zero. Assigning a technician produces Assigned at one. Starting produces InProgress at two. Blocking for access produces Blocked at three with the trimmed reason. Starting again produces InProgress at four and clears the reason. Completing produces Completed at five with CompletedAt set.

The existing lifecycle test exercises that sequence and checks the final status. A more discriminating extension can assert intermediate versions, reason clearing, technician preservation, and rejected postconditions after completion. The additional assertions should correspond to intended contracts rather than simply mirror every implementation statement.

Now attempt to Complete while Open. The method rejects work_order.transition_invalid before changing status or version. A wrong-state test should assert both fields remain unchanged. An exception-only test might miss a future implementation that increments Version before checking the state and then rejects.

## EnsureVersion is a comparison, not a reservation

EnsureVersion compares the current Version to an expected integer and throws work_order.version_conflict when they differ. The existing test assigns an order, then asks it to ensure version zero. That proves a sequential stale value is rejected after the version has advanced. It does not execute two concurrent service commands or verify a database conditional update.

WorkOrderService.ActAsync loads a live order, calls EnsureVersion, and then dispatches Start, Block, Complete, or Cancel. The store has no combined expected-version command, no save method with a concurrency token, and no lock around that service sequence. The comparison and mutation are distinct operations on the object.

A caller can therefore call EnsureVersion, allow another operation to change the object, and later invoke a mutation without rechecking. The version method did not grant exclusive ownership of that version. A deterministic domain-level interleaving can demonstrate this fact without claiming that it reproduces every scheduling detail of the asynchronous service.

## Worked stale approval interleaving

Begin with an Assigned order at version one. Actor A checks expected one, and actor B checks expected one before either mutation. A starts the order, producing InProgress at two. B then cancels it, which is a locally allowed transition from InProgress, producing Cancelled at three. Both earlier comparisons succeeded even though B's decision was based on an older observation.

This sequence is deliberately expressed through the public domain methods. It establishes that a successful EnsureVersion call does not protect a later mutation. It does not prove a particular HTTP race was executed, and it does not require changing the application. The source's separate service calls explain why a stronger atomic boundary would be needed for a stronger concurrency promise.

A proposed store command such as ApplyAction with expected version can compare and mutate under one authority. An in-memory implementation could use a gate and owned snapshot, while a database implementation could condition an update on the expected version. The result should report conflict without applying the losing command. Both are proposals beyond the current IWorkOrderStore contract.

## Action validation and error precedence

ActAsync checks expected version before deciding whether the action string is recognized. Thus an unknown action against a stale version reports version conflict first in the current sequence. A recognized action with an invalid lifecycle state reports transition_invalid. Block with missing reason passes an empty string to the domain method, whose state guard occurs before the reason validation.

Do not assume every combination of bad inputs has one universal priority. A contract can intentionally preserve error precedence, but incidental order should not become a brittle public promise without a reason. Tests should focus on the business distinction needed by clients: reload after conflict, correct an invalid action, or choose a transition valid for current state.

Action strings are exact comparisons in the service. A differently cased word is not normalized there. If an API design wants case-insensitive commands or an enum-shaped action contract, that is an explicit change to validate and document. Do not infer normalization from the separate priority parsing endpoint.

## Persistence depends on live objects

InMemoryWorkOrderStore stores and returns the same WorkOrder reference. ActAsync mutations become visible without an explicit save call. This differs from the parts store's detached snapshots and command authority. Replacing the work-order adapter with a snapshot-returning implementation would require a persistence contract for lifecycle changes.

ConcurrentDictionary protects its own dictionary operations, not the multi-step state machine on each stored object. It also does not turn EnsureVersion followed by mutation into an indivisible action. A reviewer should name the actual protected structure rather than describe every object inside a concurrent collection as thread safe automatically.

The store's AddAsync assigns by ID and can replace an entry. That is another difference from part-store duplicate rejection. A stronger work-order identity policy may prohibit replacement, but it is not currently enforced by the same TryAdd pattern. Compare module contracts explicitly before trying to reuse assumptions from inventory.

## Guided investigation and exercises

Use the disposable workflow lab to observe the stale-approval sequence and current chronology behavior. Label them as current domain observations. Then design a copied-store command that atomically checks expected version and applies an action. Your acceptance test should force two commands with the same expected version and allow at most one committed mutation under the proposed contract.

**C07.1: Transition ledger.** Record status, version, technician, blocked reason, and completion instant through assign, start, block, resume, and complete. Add two rejected transitions and assert the relevant unchanged fields.

**C07.2: Version boundary.** Explain exactly what the existing stale-version test proves. Design a stronger same-version competing-command test and name the new authority required to make it pass. Distinguish a domain interleaving model from an executed API race.

**C07.3: Adapter proposal.** Specify how lifecycle changes would persist if GetAsync returned detached work orders. Include duplicate identity, expected version, and lost-response behavior rather than adding an unqualified SaveAsync and assuming concurrency is solved.

## Review checkpoint

Time deserves one additional boundary check. WorkOrder.Complete accepts an explicit instant and normalizes it, but does not reject completion before opening. A direct-domain test can demonstrate that current behavior with fixed values. A proposed chronology rule should reject before status, timestamp, or version changes, and should be reviewed separately from atomic version handling. Injecting a clock into the service improves deterministic tests but does not itself add that missing domain comparison.

Your state machine should match every guard and retained field in current source. Your concurrency claim should stop at the evidence actually present: sequential version comparison exists, while atomic expected-version mutation is proposed. This distinction prepares you to analyze conversion, where another record changes before the work order is even added.
