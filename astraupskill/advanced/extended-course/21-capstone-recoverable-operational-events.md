# 21. Capstone: recoverable operational events

The second capstone connects preventive maintenance, request conversion, audit history, and webhook enqueue through a recoverable event boundary. Current modules demonstrate each concern separately, but their writes do not form one durable operational story. Your proposed feature will give generated occurrences and accepted transitions stable identities, commit their evidence, and let downstream delivery recover after interruption.

Use [preventive generation](../../../src/CampusOps.Core/PreventiveMaintenance/MaintenancePlan.cs), [request conversion](../../../src/CampusOps.Core/WorkOrders/WorkOrder.cs), [history](../../../src/CampusOps.Core/WorkOrders/WorkOrderHistory.cs), and [webhook publishing](../../../src/CampusOps.Core/Integrations/Webhooks.cs) as the source baseline. Implement only in a disposable prototype. This chapter provides a complete design assignment and reference expectations, not a shipped integration service.

## Bound the feature around one journey

A maintenance plan reaches a scheduled occurrence. The system creates one triaged request for that occurrence, an operator converts it to one work order under a defined policy, and the conversion records an authoritative event. A delivery component creates one intended delivery per active subscription and retries transport using stable identity. Operators can identify pending or failed work without inventing a new event on every retry.

Keep the first prototype focused on this journey. It does not need every CampusOps action, a message broker, or a production identity provider implementation. It needs explicit identity, commit boundaries, recovery state, and a small set of meaningful acceptance cases. A database-backed outbox can be a suitable proposed mechanism, but the pattern name is not the acceptance criterion.

The separate worker and API must share a durable authority or communicate through a defined transport. Their current independent in-memory stores cannot satisfy the cross-process story. A model milestone can simulate both components in one process, but the report must state that limit until a real process-boundary experiment is executed.

## Scheduled occurrence identity

Define an occurrence key from plan identity and the scheduled due instant or another stable sequence under the selected recurrence policy. Processing time is separate. A delayed worker should not create a new identity merely because it runs later, and a retry of the same scheduled occurrence should resolve to the same generated request.

Choose the catch-up policy from chapter thirteen. If every missed occurrence matters, each gets its own identity and generation can be bounded per sweep. If missed periods consolidate into one job, record which interval range the job represents. Do not let repeated worker ticks accidentally determine the product policy through one-step plan advancement.

Commit generated request, occurrence completion evidence, and plan progress consistently. A request inserted without progress can duplicate on retry; progress advanced without a request can lose work. The receipt or occurrence record should let another process distinguish pending generation from completed generation after restart.

## Conversion identity and audit evidence

Define whether each request produces one work order or can be split. For the bounded single-order policy, conversion records the linked order and marks the request converted in one transaction, together with a conversion event and operation receipt. An identical retry returns the existing order, while a conflicting command receives a defined result.

The event contains stable event ID, request and work-order IDs, verified actor, occurrence linkage where applicable, accepted time, and a version or sequence useful for ordering. User-supplied display names can be retained separately if needed. They must not replace the authoritative actor field.

Audit evidence is part of the commit if the feature promises that every conversion can be explained. A comment or optional display note can have a different policy, but the authoritative conversion event cannot be silently omitted after a second-store append fails. This directly addresses the two-stream gap observed in chapter ten.

## Outbox as a proposed handoff

A proposed outbox record is written in the same transaction as the conversion event. It describes the event that needs publication and its stable identity. A dispatcher later reads pending outbox work and creates or attempts delivery without changing the business transition's already committed meaning.

The dispatcher must handle a crash after performing part of its work but before marking completion. Stable event and subscription identities allow idempotent per-destination creation or delivery attempts. A claimed-but-incomplete operation cannot simply suppress all retries as the current TryBegin-based publisher can after failure.

Do not assume the outbox alone guarantees a remote effect occurs once. It preserves durable intent and recoverable handoff. A receiver may still process a request whose acknowledgment is lost, so the external protocol needs idempotency or a documented duplicate-tolerant operation. Separate local publication guarantees from remote processing guarantees.

## Delivery state and terminal rules

Define Pending, InFlight if needed, Succeeded, and DeadLetter with allowed transitions. Validate maximum attempts and backoff bounds. A succeeded delivery should not be returned to pending by an accidental failure call unless an explicit replay operation creates a new delivery attempt policy. A dead-letter replay should be authorized and auditable rather than an unrecorded status reset.

Store attempt records or sufficient outcome evidence to explain failures without logging secrets or unnecessary payload data. The sender resolves a secret reference through an authorized mechanism, signs or authenticates according to the chosen protocol, and records a safe diagnostic result. A secret name in a subscription is configuration, not proof that signing happened.

Choose how subscription changes affect already queued deliveries. The current delivery copies Endpoint at construction. A proposed system can preserve that snapshot, use the current subscription, or support cancellation on disable, but it must define the rule. Otherwise a retry may unexpectedly send historical payloads to a newly edited destination.

## Worked interrupted journey

Occurrence O generates request R and commits its completion record. The worker crashes before returning its count. On restart, O resolves to R and no duplicate request is created. The operator converts R under command C; the transaction creates work order W, event E, and outbox item X. The response is lost, but retrying C returns W from the receipt.

The dispatcher creates delivery D for subscription S, sends it, and loses the acknowledgment after the receiver processes E. A retry uses the same event identity, allowing a cooperating receiver to recognize the duplicate. The sender eventually records success or a documented unresolved/dead-letter result. None of these retries creates a new business conversion event.

This trace connects three distinct recovery layers: scheduled generation, business command, and external delivery. Each uses stable identity at its own scope. A single global boolean called processed cannot describe all three stages or identify where intervention is needed.

## Reconciliation and operator actions

Provide a read-only view of pending occurrences, conversions without expected relationships, outbox age, delivery attempt state, and dead letters. Each row should identify the relevant stable key, observed version, and failure category. Avoid treating an empty webhook queue as proof that no events occurred; committed events and publication progress are separate records.

Operator actions should be commands with expected-state checks. Replaying a dead letter should not create another conversion or bypass payload-conflict detection. Repairing a missing projection should be idempotent by event identity. If records disagree in a way the protocol cannot resolve safely, preserve evidence and require reviewed reconciliation rather than guessing an owner.

A useful runbook distinguishes retryable infrastructure failures, invalid configuration, authorization failures, and inconsistent business records. Repeatedly retrying an oversized generated summary or an invalid endpoint does not repair the input. The system should make persistent failures visible without flooding logs or creating duplicate work.

## Acceptance and evidence stages

The core tests cover duplicate occurrence generation, long-task validation failure, conversion failure before commit, identical conversion retry, conflicting operation reuse, outbox recovery, partial subscription fan-out, terminal delivery guards, and ambiguous receiver acknowledgment. Include a test showing that two separate processes see the same retained operation state for the durable milestone.

A pure model can verify state-machine decisions and identity semantics. Real database tests verify transactional relationships. A fake HTTP handler can verify sender construction without external network access. A controlled receiver can test duplicate-event handling. State each boundary separately so a passing model is not promoted into an end-to-end deployment guarantee.

## Independent capstone challenges

**C21.1: Identity map.** Define occurrence, command, event, outbox, delivery, and attempt identities. Explain which persist across retries and which represent genuinely new work. Include a conflicting payload case and a changed-subscription case.

**C21.2: Failure harness.** Specify three crash or fault windows across generation, conversion, and delivery. For each, name the durable evidence used to resume and the test observer needed to prove recovery.

**C21.3: Operational review.** Design a dashboard and runbook for a growing outbox backlog with some dead letters. Distinguish delayed delivery from missing business events and define safe operator replay commands with authority and version checks.

## Review checkpoint

Record one end-to-end correlation example linking occurrence, request, order, event, and delivery IDs. Keep that diagnostic chain separate from authoritative operation identity. It should help an operator navigate evidence without suggesting that a caller-supplied correlation string proves who performed the action or whether it committed.

The capstone is coherent when every accepted intent has a discoverable outcome and each recovery action preserves identity and ownership. It is complete as a learning brief when its contracts, traces, acceptance cases, and reference design are reviewable. Optional infrastructure choices remain implementation milestones rather than missing instructional content.
