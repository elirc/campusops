# Worked solutions and capstone reference designs

These answers complete the extended route. They describe current behavior where source establishes it and proposed contracts where the exercises ask for a change. Database, HTTP, and restart experiments remain designs unless separately executed. The course's recorded console checks establish only their named copied-source observations.

## Chapter 11: purchasing policy

For C11.1, amounts below or equal to one thousand become Approved with policy:auto, while a greater amount becomes PendingApproval. Inactive vendor rejects in ProcurementService before quote creation. Nonpositive amount or blank justification rejects in PurchaseRequest construction. A past ValidUntil is carried into the quote but not checked against a clock in the current creation path. Equality at the threshold and expiry at the current instant are separate policy boundaries.

For C11.2, a proposed approval receipt identifies the purchase, expected version, verified actor, decision, and stable operation ID. A coordinated compare-and-transition allows only one competing approve or reject decision from the expected state. Identical retry replays its own outcome; conflicting reuse rejects. Supplied Approver text can remain display attribution but cannot replace the verified actor. A durable receipt must commit with the decision it describes.

For C11.3, receiving references a delivery identity, purchase or order where required, part, and delivered quantity. It validates acceptance and capacity, updates OnHand, records a stock movement, and records a receipt together. Approval authorizes spending; ordering requests goods; delivery is a supplier event; receipt accepts quantity into stock. Current Approved status performs none of those later steps. Duplicate receiving must resolve the original delivery operation rather than add stock again.

## Chapter 12: deadline arithmetic

For C12.1, before and equal to DueAt are not overdue; strictly after can escalate once when unpaused. Pausing at 10:30 and resuming at 12:00 extends an 11:00 deadline to 12:30. Current backward resume subtracts time because the duration is negative. A proposed guard rejects before changing DueAt or clearing the pause, preserving both fields. UTC normalization alone does not establish chronological validity.

For C12.2, return equality on the first clock read and a later instant on the second for two equal-deadline records in a controlled order. The current per-item loop can escalate only the second. A captured-threshold proposal evaluates both against the first instant and escalates neither. Assert item states and count, not merely how many times the fake clock was called. The outcome expresses the batch's temporal policy.

For C12.3, commit an escalation state and a durable event or outbox item together. A sender later resolves delivery with stable identity. Setting EscalatedAt and then performing an unrecorded send can lose notification if the send fails and later sweeps skip the already escalated item. Ordinary deadlines should derive from authoritative work-order values; an override should carry explicit permission and reason rather than masquerading as normal input.

## Chapter 13: recurrence and topology

For C13.1, one-per-sweep generates one occurrence and advances one interval even when still overdue. Skip-to-future advances beyond now and records that missed intervals were not separately generated. Bounded catch-up creates a defined maximum number of occurrence-keyed requests and retains remaining backlog. Each is a valid proposal when the product meaning is explicit. Current code implements one generation per selected plan per invocation.

For C13.2, a sufficiently long nonblank Task passes plan construction but becomes an oversized summary after the Preventive prefix. MaintenanceRequest construction rejects before insertion and plan advancement. A proposed fix can validate the plan's projected summary length or define a short summary plus full task description. The choice should preserve job identification and make persistent invalid plans visible rather than retrying them indefinitely.

For C13.3, the API and worker need a shared durable plan/request authority or a transport that conveys generated requests and progress. A test must create a plan through the chosen authority, run generation in one process, and read the result from another after the relevant restart boundary. Two services instantiated with one in-memory store in a unit test do not establish that deployment property. Occurrence identity prevents a crash between insertion and progress from duplicating work.

## Chapter 14: enqueue recovery

For C14.1, normal enqueue claims the key and creates deliveries. A duplicate successful retry returns zero. Failure after key claim but before listing leaves the key with zero deliveries, and the retry still returns zero. Partial two-subscription insertion leaves the key and a successful prefix, again suppressing completion on retry. The stronger proposal needs a completed receipt or per-destination progress, not only a started marker.

For C14.2, a positive maximum is validated as configuration. The first two nonterminal failure delays under the current formula are two and four minutes from each supplied failure instant. Proposed terminal guards reject accidental success/failure calls after Succeeded or DeadLetter unless an explicit replay workflow is invoked. Payload identity is compared for reused event keys. Rejection should preserve Attempts, Status, and NextAttemptAt under the chosen contract.

For C14.3, a sender resolves a secret reference through an authorized mechanism and uses a stable event identity in the receiver protocol. The receiver deduplicates or tolerates repeated attempts when acknowledgment is lost. Enqueue completion means intended delivery records exist; it does not mean HTTP succeeded or the remote effect happened. The current code does not contain the sending loop or signing implementation, so those remain proposed features.

## Chapter 15: client contracts

For C15.1, part creation can be followed through its returned Location to the mapped item GET using an Operations principal. A proposed work-order item GET must first be implemented or its absence documented; conversion's formatted URI alone does not provide it. The test should follow the actual header and verify response identity and authorization, not independently reconstruct a route that happens to work.

For C15.2, explicit vendor and purchase response records can expose stable IDs, selected descriptive fields, quote summary, status, and approval evidence according to the product contract. Omit internal or unneeded data deliberately, while accounting for clients of the current direct-domain response. Shape tests detect accidental field changes; mapping tests verify values and null states. Adding a domain property should not silently become an API feature under the proposed projection.

For C15.3, invalid quantity calls for correcting input; stale version calls for reloading and reconsidering intent; missing part calls for resolving the target; forbidden campus calls for appropriate authority or target correction; interrupted reservation calls for outcome resolution under a receipt proposal. Current domain failures generally map to 400, so a richer taxonomy is a separate compatibility change. Blanket retry is unsafe for additive commands.

## Chapter 16: authority boundaries

For C16.1, inventory and work-order groups request Operations, procurement and webhook groups request Manager, and location creation additionally calls CampusAuthorizer. Separately mapped assignment and service-level actions lack those explicit requirements in the inspected source. A full runtime matrix should test the intended host configuration, but source review already prevents assuming policy inheritance from a shared URL prefix.

For C16.2, a work-order command resolves its authoritative LocationId, loads the location, and checks verified actor membership against the chosen campus interpretation. Missing or inconsistent location data produces a defined result before mutation. A cross-campus manager is not automatically permitted unless the role's scope explicitly includes that campus. Historical versus current location must be decided so an asset move does not silently change authorization meaning.

For C16.3, production identity verifies issuer, credentials or tokens, and claim meaning under the selected provider contract. A dedicated test scheme supplies deterministic principals for policy tests without pretending demo headers are production authentication. Existing role-gate tests remain useful, while new evidence verifies identity trust, resource scope, and actor attribution. The migration should preserve the difference between authenticated identity and client-supplied labels.

## Chapter 17: durable adapter behavior

For C17.1, ChangeStatusAsync mutates the reconstructed Asset and can return OutOfService, but the current SQLite adapter exposes no update call in that path. A fresh-context read is predicted to retain the stored Active status. After implementing an explicit persistence command, the regression expects both returned and fresh observations to agree. Run the actual provider test before reporting the prediction as executed evidence.

For C17.2, in-memory GUID assignment can replace entries while database primary keys constrain inserts. SQLite models unique asset tags and location-label tuples plus asset-location foreign keys; the default dictionaries do not supply identical business-key rules. Case comparison depends on normalized values and provider semantics, so it deserves a real integration test when part of the contract. Do not treat a schema declaration as proof of every provider behavior.

For C17.3, a complete durable workflow needs registrations and context lifetimes, schema and migration policy, explicit mutation publication, concurrency handling, exception mapping, and fresh-context/restart tests appropriate to its claim. Location and asset adapters alone do not persist parts, reservations, requests, or worker progress. The record set must match the business invariant rather than the subset of adapters already available.

## Chapter 18: report meaning

For C18.1, include one Completed High-priority order and one Open Normal order. WorkByPriority totals two while OpenWorkOrders is one. Include a part with ten on hand and nine reserved; Available one makes it low stock. Both outcomes follow current predicates. A label implying that priority groups contain only active work or that low stock means low physical quantity would be misleading.

For C18.2, a best-effort contract documents sequential store observations and live member risks. A per-store snapshot captures scalar values under each authority but still spans stores. A cross-store snapshot needs a shared read boundary or versioned projection. The report timestamp should identify what it means, such as projection version or generation time, rather than imply simultaneity unsupported by the data reads.

For C18.3, a parser-based test handles multiline titles and doubled quotes as logical fields. Equal sort keys need an explicit tie-breaker only if deterministic order is promised. Formula-like values require a separate spreadsheet-consumer policy. These are three independent requirements: valid CSV structure, stable ordering, and safe downstream interpretation. Passing one does not establish the others.

## Chapter 19: experiment review

For C19.1, use before-order-add, after-order-add, before-audit-event, after-event-key-claim, before-durable-commit, and after-commit-before-response as distinct seams. Each has a different successful prefix and observer. A designated exception and state ledger make the result reproducible; an undifferentiated failing store hides the very distinction being tested.

For C19.2, verify explicit new external output, current-source copying, checked mutation anchors, source hashes, isolated build configuration, and honest expected outcomes. A useful improvement is a suite identifier in the run record. A destructive cleanup step or unannounced dependency restore would broaden the lab without improving its core evidence.

For C19.3, threshold equality versus just above catches an incorrect comparison operator. Backward versus forward resume catches missing chronology. A failed subscription list after a real key claim catches unrecoverable started-only idempotency. The pair should change one relevant condition and use fixed fixtures. Unexecuted variants remain designs even when their predicted outcomes are clear.

## Chapter 20: inventory reference design

For C20.1, define Part, ReservationBalance, StockMovement, and OperationReceipt under one durable inventory authority. Reservation original quantity equals remaining plus consumed plus released. Aggregate Reserved equals remaining allocation totals. Each command has a scoped operation ID and canonical payload. Actor permission and work-order eligibility are evaluated under the chosen commit policy. A transaction commits stock, balance, movement, and receipt together.

Reserve increases aggregate and owned remaining allocation. Consume decreases OnHand, Reserved, and remaining while increasing consumed. Release decreases Reserved and remaining while increasing released. Receive increases OnHand and records a unique delivery operation. Identical receipt reuse replays; conflicting payload rejects. An unrestricted Save of a stale Part is a rejected alternative because it can erase allocations without matching lifecycle evidence.

For C20.2, normal lifecycle follows the chapter's numerical trace. Insufficient reserve, overconsume, overrelease, and overflow reject without completed receipt or partial movement. Competing commands cannot spend the same remaining allocation twice. Identical concurrent operation IDs produce one outcome. A pre-commit fault leaves prior durable state, while an interrupted response after commit resolves through the retained receipt. The implementation must use the real storage observer for durable claims rather than a fake that assumes rollback.

For C20.3, the operator queries the original consume operation identity, checks its recorded result and versions, and retries only under the documented same-operation contract. An unknown result is reconciled through durable records, not by adjusting stock until it looks plausible. A stale-version conflict requires fresh evidence. Inconsistent allocation and aggregate totals produce a reviewed repair command with audit evidence, not an untracked overwrite.

## Chapter 21: operational-events reference design

For C21.1, occurrence identity represents scheduled work; command identity represents a user's logical transition; event identity represents the committed fact; outbox identity tracks publication intent; delivery identity combines event and destination; attempt identity records one transport try. Retries preserve the relevant higher-level identities while creating new attempt records where appropriate. Conflicting payload reuse rejects, and subscription edits follow an explicit snapshot-or-current policy for existing deliveries.

For C21.2, a generation transaction commits request, occurrence completion, and plan progress. A conversion transaction commits request state, work order, authoritative event, receipt, and outbox intent. A dispatcher can recover after creating some deliveries by using unique event/destination keys. A lost receiver acknowledgment may cause another attempt with the same event identity; remote deduplication or duplicate tolerance is required for the external effect contract. Separate process tests prove shared durable state only when actually executed.

For C21.3, the dashboard reports oldest pending outbox age, failed publication categories, delivery attempt state, and dead letters linked to committed events. A backlog means delayed publication, not necessarily missing business events. Operator replay uses expected state and stable identity, records who requested it, and cannot create another work-order conversion accidentally. Persistent invalid configuration is corrected before retry rather than hidden behind an endless loop.

## Chapter 22: final handoff

For C22.1, include quantity bounds, snapshot ownership, paired commit, additive retries, version comparison, conversion failure, threshold equality, pause chronology, recurrence topology, enqueue recovery, route scope, and detached persistence. Label each by evidence status and link to its source or run record. The index should make missing runtime boundaries visible rather than forcing reviewers to infer them.

For C22.2, useful challenges include cancellation after inventory success, a burned webhook key with no deliveries, and a second worker process using separate memory. The response should predict state and caller knowledge, then name the stronger mechanism needed for any proposal. A local lock cannot coordinate another process, and a timeout cannot prove a remote effect did not occur.

For C22.3, choose a small chronology guard, a medium explicit asset persistence command, and a larger durable inventory or event capstone. Define acceptance evidence at each scope. Optional broker, identity-provider, or frontend integrations can remain deferred because the instructional briefs are complete and explicitly distinguish design from implementation.

## Final rubric

Evaluate source accuracy, complete state traces, discriminating experiments, coherent proposed invariants, and honest evidence scope with equal weight. A submission cannot earn full scope credit while claiming HTTP authorization from a console harness or restart durability from a shared in-memory object. Reward a smaller precisely demonstrated milestone over a larger unsupported guarantee.

The final artifact set should include quantity and ownership ledgers, transition tables, policy matrices, fault catalogues, adapter comparisons, and both capstone designs. Their purposes differ, so preserve their distinctions. Together they show how a business promise becomes a concrete authority, state transition, and testable outcome in the actual CampusOps codebase.
