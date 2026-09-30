# Worked solutions and review: inventory and maintenance boundaries

These answers preserve the distinction between current code, executed laboratory observations, and proposed stronger contracts. Alternative policies can be valid when stated explicitly and enforced coherently. Current-behavior answers must match the source, even when the behavior is weaker than the learner's preferred design.

## Chapter 03: the paired reservation commit

For C03.1, the caller's original part stays at eight on hand and zero reserved after being copied into the store. A successful three-unit command prepares a candidate with eight on hand, three reserved, and five available, then publishes it with one record. A later GetAsync returns another snapshot of that state. The insufficient six-unit command returns null at the store boundary and publishes neither another part state nor another record. PartsService translates that null to part.stock_insufficient.

For C03.2, a derived timeline can query reservation records and present them without maintaining a second event stream, subject to its read-consistency contract. An atomic recorded event needs the event in the same commit authority. An asynchronous projection needs a stable reservation identity, idempotent delivery, and a visible backlog or delay policy. Absence of a projected timeline entry must not be interpreted as proof that no allocation exists. Calling another service while holding the inventory gate would introduce a collaborator failure between previously bounded steps and should not be added casually.

For C03.3, work-order terminal eligibility belongs to a coordinated workflow policy; retry identity belongs to an operation receipt boundary; SKU uniqueness belongs to a defined business-key authority; durability belongs to persistent storage and recovery. The current store establishes none of those broader properties merely by keeping its stock and allocation dictionaries consistent. A scoped statement recognizes the achieved property without hiding the missing ones.

## Chapter 04: cancellation outcomes

For C04.1, a pre-canceled service call stops at its initial check. Cancellation just before the store call is observed inside the gate before mutation. Cancellation after a successful store return leaves the returned record and committed stock/history intact. The first two cases produce cancellation with no new allocation; the third produces a completed allocation alongside a now-canceled token. All can be forced at public boundaries without sleeps.

For C04.2, a post-publication ThrowIfCancellationRequested can make a caller receive cancellation after the reservation exists. A controlled copied variant can signal after publication, cancel, and execute the extra check. The state remains committed despite the exception. The current implementation avoids that misleading post-commit cancellation path; adding a check is a contract change, not automatically a safety improvement.

For C04.3, abandoning a request stops waiting or asks execution to stop where supported. Releasing an accepted allocation changes business state and needs reservation ownership, remaining quantity, a release command, and matching history. The current record does not model that lifecycle. A UI should therefore avoid promising that canceling a network request unreserves parts. A proposed receipt lets the user first determine whether an allocation was accepted.

## Chapter 05: operation identity

For C05.1, duplicate Part ID is rejected by TryAdd; duplicate SKU under different IDs is not prevented by that dictionary. Repeating reservation fields creates another record if stock permits. Repeated operation ID has no current inventory contract because the command accepts none. A technician intentionally requesting two more units should use a new logical operation identity under the proposal, even if the business fields match an earlier request.

For C05.2, one coherent policy records completed successes and their canonical payloads in the same commit as stock and reservation. Identical retry replays the original result; conflicting reuse rejects; a pre-commit business rejection is not recorded as a completed success and can be retried according to a documented attempt policy. An interrupted response is resolved through the stored outcome. An expired receipt produces an explicit unsupported-or-unresolved outcome rather than falsely asserting that the original operation never happened.

The design must coordinate the first lookup and insertion under concurrent attempts. Two callers both observing no receipt cannot each publish a separate allocation. A unique key and transaction or equivalent same-process authority must arbitrate that race. Storing a receipt after unrelated writes leaves a failure gap and does not satisfy the proposed contract.

For C05.3, the client retains the same operation identity after timeout and queries or retries that operation. It compares or supplies the same payload and accepts a replayed receipt as the original result. Current availability alone cannot identify who consumed stock or whether the earlier call committed. Generating a new operation ID immediately turns uncertainty into a new allocation attempt and can double-reserve.

## Chapter 06: concurrency evidence

For C06.1, successful count checks capacity admission; history count checks publication; distinct IDs check record identity; quantity sum checks stock/history agreement; zero Available checks final capacity; fixed timestamps check argument propagation; unchanged original part checks input isolation. A count-only test can miss duplicate records or a mismatched aggregate. Even the full test cannot establish database durability or coordination between separate processes.

For C06.2, a per-part coordinator needs stable lock identity, safe reservation collection publication, and a defined history-read snapshot. It should be evaluated against a measured unrelated-part contention problem before replacing the simple gate. A lock entry cannot be removed while waiters still use it and replaced with another live lock for the same part. The retained invariant is matching accepted quantities and records, not the incidental choice of lock data structure.

For C06.3, one policy allows completion based on work-order eligibility observed when the operation is accepted; another requires eligibility at inventory commit. The latter needs coordination with work-order status changes or a versioned shared decision. Adding a status read outside that boundary leaves a race. Current PartsService checks only existence, so the terminal-status rule is explicitly new policy rather than a regression already covered by the inventory gate.

## Chapter 07: work-order state and versions

For C07.1, assign advances zero to one and records the technician; start advances to two; block advances to three and stores the trimmed reason; resume advances to four and clears the reason; complete advances to five and records the UTC completion instant. Completing an Open order rejects without advancing version. Starting a Completed order also rejects. A canceled Blocked order can retain BlockedReason because Cancel does not clear it in current code.

For C07.2, the existing test proves that EnsureVersion rejects zero after assignment advanced the version to one. It does not prove atomic compare-and-mutate. The laboratory sequence checks version one twice, starts, then cancels, demonstrating that earlier successful comparisons do not reserve later authority. A proposed combined command should force two callers using the same expected version and allow only one committed mutation under its shared gate or database condition.

For C07.3, a detached adapter needs an explicit lifecycle command or save-with-expected-version contract. Duplicate identity should have a chosen reject-or-replace policy. Lost response after a successful mutation needs a receipt or a documented resource-query strategy if retries must distinguish completion. A bare SaveAsync without an expected version can overwrite a newer detached state and would not fulfill the proposed concurrency guarantee.

## Chapter 08: conversion and references

For C08.1, an untriaged request rejects before order creation and remains Submitted. A successful conversion changes the request to Converted and adds one Open order carrying the selected request values. A designated before-add failure leaves the live request Converted and no stored order. Retrying then rejects request.not_triaged in the current flow. Moving the request mutation after insertion merely creates the opposite partial window; a shared conversion authority is the stronger proposal.

For C08.2, a proposed submission service can require a known location, validate an optional asset, and check that asset location agrees with the request's chosen meaning. Decide whether the request stores the location at submission or follows the asset's current location. A later move should not silently rewrite historical intent unless that is the explicit policy. A nonempty GUID is only local format validity, so the authority needs actual relationship evidence.

For C08.3, a move command can load the asset and destination, reject retired assets, verify actor scope, and commit the new location with an audit record under its chosen boundary. The direct MoveTo method currently lacks destination existence and empty-ID checks. A domain regression can test a proposed local nonempty rule, while a service test covers destination lookup and authorization. Neither test alone proves a durable move audit transaction.

## Chapter 09: qualifications and assignment

For C09.1, normalized membership is implemented by the HashSet and HasSkill. Verified certification, workload, and actor permission are not established by that check. A work order or task specification should own an authoritative required capability under the proposal. Request-supplied RequiredSkill currently chooses the comparison input; it does not prove the job's real requirement.

For C09.2, success changes an Open order to Assigned, increments Version, and records TechnicianId. Inactive or missing-skill rejection occurs before order mutation. A non-Open order rejects through Assign, and a sequential duplicate assignment leaves the first assignment intact. These traces do not establish atomic eligibility-at-commit or durable persistence because the service acts on live references through separate observations.

For C09.3, an assignment request test must target the actual separately mapped endpoint and verify the intended policy. Another authorized group with a similar path prefix does not automatically contribute metadata. The domain ownership test observes that Skills returns a runtime HashSet and that casting and mutation change HasSkill. This is an in-process exposure, not a claim that a remote client can perform a CLR cast through JSON. A proposed immutable or copied projection should be tested at that exact ownership boundary.

## Chapter 10: audit completeness

For C10.1, normal completion appends one comment and one event. Blank author or body rejects before either append. A before-comment fault leaves both empty. A before-event fault leaves one comment and no event. An identical retry after that fault can add another comment and one event, producing two comments with distinct IDs and only one event. Timestamp proximity does not identify an exact comment-event pair because the model has no explicit comment link in AuditEvent.

For C10.2, an atomic-pair proposal introduces a combined append command with operation identity, comment ID linkage, and shared publication. An eventual-projection proposal makes comments authoritative and delivers their events idempotently, exposing lag and failures as part of the contract. Either should define how Timeline distinguishes a missing order from a known empty stream if clients need that distinction. Current TimelineAsync does not perform the existence check.

For C10.3, verified actor identity comes from trusted request context or another authenticated authority, while attribution text remains user content. Store both separately when the product needs both meanings. A test changes Author in the request while keeping the authenticated principal fixed and verifies that the authoritative actor remains that principal. Nonblank validation alone cannot provide this binding. The existing route policy restricts access but does not transform supplied text into verified identity.

## Review rubric for the installment

Score each chapter out of ten: three points for source accuracy, two for complete state traces, two for discriminating experiments, two for clear proposed-versus-current boundaries, and one for a specific limitation. A solution loses accuracy points when it claims universal token handling, atomic work-order versions, trusted request author text, or inventory retry deduplication that the current code does not implement.

Retain the paired-commit ledger, cancellation table, receipt contract, concurrency assertion map, work-order transition table, conversion fault trace, assignment authority map, and two-stream audit ledger. These artifacts answer different questions and should remain distinct. The next installment will use them to examine procurement, time-based operations, integrations, API policy, and durable storage without repeating the same inventory lesson under different names.
