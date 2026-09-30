# 04. Cancellation before the inventory commit

The parts store makes a deliberate cancellation choice: it checks the token inside the gate before mutation and does not report cancellation after its publication writes. This is different from a workflow that changes a live object and only later reaches a cancellation-aware save. Understanding this choice requires separating the caller's request to stop, the point where the store observes that request, and the outcome already committed.

Read [InMemoryPartStore](../../../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs), [PartsService](../../../src/CampusOps.Core/Inventory/Part.cs), and [the cancellation reservation test](../../../tests/CampusOps.Infrastructure.Tests/Inventory/ConcurrentReservationTests.cs). This chapter's deliverable is a cancellation outcome table with exact observation points. It does not promise that every CampusOps adapter honors tokens identically.

## Check after acquiring the authority

Each part-store operation acquires the gate and then calls ThrowIfCancellationRequested. For a reservation, the token check occurs before identity validation, quantity validation, stock inspection, candidate preparation, and publication. An already-canceled token therefore produces cancellation without changing stock or reservation records in that method.

Placing the check inside the gate matters for a caller that waits while another operation owns the gate. The token can become canceled during that wait. Once the waiting caller enters, it observes cancellation before doing its work. The ordinary monitor acquisition itself is not a cancellation-aware asynchronous wait; the check happens after acquisition. Do not describe it as an instantly interruptible lock queue.

The store's critical sections are short and synchronous in the current implementation. That limits the ordinary waiting interval, but it is not a universal latency guarantee. A future extension that performs I/O or invokes callbacks while holding the gate would change that assumption and should trigger a design review. Cancellation responsiveness depends on where the code can observe the token.

## Why there is no post-commit token check

Once the store has prepared a valid candidate and record, it publishes them and returns success. It does not call ThrowIfCancellationRequested after changing state. Therefore, a token canceled just after the pre-commit check does not cause this method to claim that the completed reservation was canceled. The state and method outcome remain aligned for the ordinary execution path.

This does not mean the token was ignored accidentally. It means the command has a commit point after which success is the truthful result for that operation. A client can still disconnect and fail to receive the response, but that is an outcome-discovery problem rather than a reason for the store to throw cancellation after committing.

Adding a final token check may look like extra responsiveness while actually creating an ambiguous result: the caller sees cancellation even though stock and history changed. If you propose such a change, explain the new contract and retry behavior. More token checks are not automatically better when their placement misrepresents completed effects.

## Service checks and adapter checks have different roles

PartsService checks cancellation immediately, validates IDs and quantity, checks work-order existence, and then calls the store. Its early check avoids unnecessary work for an already-abandoned request. The store's check protects its own public boundary and catches cancellation that occurs after the service's earlier check but before the store enters its commit region.

The work-order adapter used by the default composition returns stored objects without an explicit token check in its methods. PartsService still passes the token, but passing a token is not proof that a collaborator observes it. The later part-store check remains important. A broader cancellation review must inspect actual implementations rather than assume every token parameter has the same behavior.

This variation across modules is part of the course's evidence discipline. The parts adapter has an explicit no-cancellation-after-mutation policy. The work-order and history adapters need their own analysis. Do not copy the strongest guarantee in one module onto the entire application because the interfaces all contain CancellationToken.

## Worked outcomes at three moments

Case one starts with five available units and a token already canceled. ReserveForWorkOrderAsync enters the gate, throws at its check, and leaves both dictionaries unchanged. A later uncanceled lookup sees five available and empty history. This is the case directly covered by the existing reservation test.

Case two cancels before the store call but after earlier service validation. The store still sees the canceled token at its own check and changes nothing. A deterministic wrapper can cancel just before forwarding the call to the real store. This tests the adapter boundary without relying on a timed race or modifying the original application.

Case three cancels after the real store has returned a successful reservation. A wrapper can make that schedule explicit: delegate, capture the successful record, cancel the source, and return the record. Stock and history are committed, and the wrapper should not manufacture a cancellation exception unless that is the separate behavior being studied. The observation teaches that a token's current state and a completed command's outcome are not the same fact.

## Cancellation during a synchronous commit

The current store has no await or public injection seam between the token check and publication. A test that tries to cancel at an exact internal instruction using a sleep is unreliable. You can reason from source that there is no later token observation, and you can test before and after the public boundary deterministically. Do not claim an exact internal interleaving was executed when it was only inferred.

If a future exercise instruments a copied implementation with a gate after the token check, label it as a laboratory variant. It can demonstrate the consequences of the documented policy: cancellation after acceptance does not undo the commit. It cannot by itself prove timing behavior of the uninstrumented production method. The instrumentation changes the scheduling surface even when it preserves the intended contract.

A stronger durable implementation will have additional cancellation points inside database APIs. Its contract must distinguish a command canceled before commit from an ambiguous database result and a committed result whose response is lost. The in-memory policy offers a useful model, but database cancellation semantics require actual provider-level evidence.

## Cleanup should not reverse a valid reservation

An operation may need to release a local coordination handle or dispose resources when its caller cancels. That is cleanup. Releasing reserved stock is a business action with ownership and history requirements. A finally block that calls Part.Release on a snapshot neither updates stored stock nor records an allocation change; a broader unconditional release could be actively wrong.

If a user wishes to cancel an accepted parts reservation, that should be an explicit reservation-lifecycle command under a defined contract. It is different from abandoning the HTTP request that created the reservation. The current PartReservation record has no cancellation status, so a complete release feature requires new model and history decisions rather than a token trick.

This distinction helps user-interface design. A button labeled cancel request may stop waiting, while cancel reservation asks the system to reverse an accepted allocation. The interface should not promise the latter when it only cancels a network call. A proposed receipt and query path can make the outcome visible after an interrupted wait.

## Guided lab: use seams rather than sleeps

In a disposable reference workspace, wrap the actual part store with a before-call cancellation seam and an after-success cancellation seam. Keep stock, work order, quantity, and clock fixed. Assert stock and history for both cases, plus the returned result or expected cancellation. A dedicated wrapper makes the schedule reproducible in one run.

Use a canceled AddAsync case as a separate input-ownership test: the store should remain empty, and no caller object becomes authoritative. Then test an uncanceled addition to confirm the fixture is otherwise valid. This paired approach prevents a malformed fixture from masquerading as successful cancellation behavior.

Record what remains untested. These experiments do not measure lock-wait latency, HTTP disconnect handling, database rollback, or process shutdown. They establish bounded public-boundary outcomes for the current in-memory authority. A good lab report states that limitation next to the result rather than burying it in a generic disclaimer.

## Independent exercises

**C04.1: Outcome table.** Compare cancellation before PartsService begins, before the store boundary, and after a successful store return. Record token state, method outcome, stock quantities, and history count. State which cases can be forced without instrumenting internal store code.

**C04.2: Post-commit check.** Explain the behavioral change caused by adding ThrowIfCancellationRequested after the publication writes. Design a test that distinguishes the old and new outcomes and explain why the new exception would not prove rollback.

**C04.3: User action contract.** Specify the difference between abandoning a reservation request and releasing an accepted reservation. Identify the model fields and history command needed for the latter, and mark that feature as proposed rather than currently implemented.

## Review checkpoint

When documenting an API timeout, distinguish the client's elapsed-time policy from the server token observation. A timeout can occur while the server continues or after it has completed. The in-memory store's truthful success result is useful internally, but a transport failure can still hide it from the caller. That is why receipt lookup belongs to the recovery design rather than to cancellation alone.

Your cancellation story should preserve truthful outcomes: a rejected pre-commit command changes nothing, while a completed reservation remains completed even if the caller stops waiting. Keep this specific policy separate from the behavior of other CampusOps stores, and carry the interrupted-response question into the retry chapter.
