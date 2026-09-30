# 10. Comments, audit events, and partial history

Work-order history records both user comments and audit events. The current service appends them through two separate calls, while the store keeps two concurrent queues. This design works on the ordinary path, but a failure between calls can leave a comment without its event. This chapter compares that boundary with the stronger inventory commit and develops a concrete audit-consistency proposal.

Read [WorkOrderHistoryService](../../../src/CampusOps.Core/WorkOrders/WorkOrderHistory.cs), [InMemoryWorkOrderHistoryStore](../../../src/CampusOps.Infrastructure/WorkOrders/InMemoryWorkOrderHistoryStore.cs), [the history test](../../../tests/CampusOps.Infrastructure.Tests/WorkOrders/WorkOrderHistoryTests.cs), and [work-order endpoints](../../../src/CampusOps.Api/WorkOrders/WorkOrderEndpoints.cs). Your deliverable is a two-stream ledger and an audit contract that explains both normal and interrupted outcomes.

## Records are values, but the workflow spans calls

WorkOrderComment contains an ID, work-order ID, author string, body string, and creation instant. AuditEvent contains its own ID, work-order ID, event type, actor, detail, and occurrence instant. These record properties are immutable in the current definitions, so a returned record does not expose mutable stock-like state.

Immutability of individual records does not make appending two records atomic. The service first checks work-order existence, validates nonblank author and body, creates a comment with trimmed text and current UTC time, and appends it. It then creates and appends a CommentAdded event with another timestamp read. Only after both calls complete does it return the comment.

The event detail is a fixed message indicating that a comment was added; it does not copy the full comment body. That limits duplication of potentially sensitive content in the audit stream. However, the event also does not include the comment ID in its defined fields. Correlating a specific event to a specific comment relies on surrounding context rather than an explicit record link.

## Normal-path evidence is useful and narrow

The existing test creates an order, calls CommentAsync, and verifies one comment plus one CommentAdded event. It establishes the ordinary service behavior with the real in-memory history store. It does not inject a failure between appends, verify atomic publication, or prove that the recorded author is a verified identity.

The store uses ConcurrentQueue for each stream. Enqueue operations can safely add records to their respective queues under concurrent use, but two separate enqueue calls across two queues do not become one business commit automatically. A reader can observe one stream after its append and the other before its append unless another authority coordinates the pair.

This contrasts with InMemoryPartStore, whose paired publication occurs behind one gate used by readers. The difference is concrete and local; it is not a blanket judgment about queues versus dictionaries. Either structure can participate in a sound design when the workflow's required observation boundary is enforced.

## Worked failure before the audit append

Begin with an existing work order and empty history. Use a wrapper that delegates AppendCommentAsync to a real history store but throws a designated exception before AppendEventAsync delegates. CommentAsync validates and stores the comment, then reaches the injected fault. The caller receives an exception and no returned comment value from the service call.

A subsequent comment query finds one record. The event query finds none. This is a partially published history result. The failure did not erase the comment, and the absence of an event does not prove no comment was added. A timeline that displays only events can therefore omit an action that exists in the comment stream under this controlled fault.

Retrying CommentAsync with identical author and body creates new IDs and can append another comment. There is no operation identity or duplicate suppression in this service. If the first event failed, the retry can leave two comments and one event after normal completion. Both the partial outcome and retry meaning should appear in a proposed repair's acceptance tests.

## Two clocks do not create causal identity

The comment and event each use DateTimeOffset.UtcNow at construction. They will often be close, but they need not be equal. Sorting comments by CreatedAt and events by OccurredAt gives timestamp order within each stream, not a guaranteed shared sequence. Equal timestamps and concurrent writers can make a purely time-based join ambiguous.

A proposed shared operation ID or CommentId reference can make the relationship explicit. A monotonically assigned sequence under a shared authority can support a stronger timeline ordering contract if needed. Do not use timestamp equality as the only join condition between records created by two separate clock reads.

Time injection can make tests more deterministic, but it does not solve atomicity. A fixed clock could make every comment and event have the same instant while the streams still diverge on a failed second append. Keep the clock improvement and the consistency improvement separate in your design explanation.

## Author text is not authenticated actor evidence

The comment endpoint accepts an AddCommentRequest and passes body.Author to the service. The service trims that string and records it as comment author and event actor. It verifies only that the text is nonblank; it does not derive the actor from the authenticated principal in this method.

The work-order route group requires the Operations policy, which is a useful route-level gate under the configured identity scheme. That does not make a request-supplied Author string trustworthy as the identity of the caller. An authorized caller can still submit a different name unless another boundary explicitly binds the field.

A proposed audit design should distinguish a verified actor identifier from a display label or on-behalf-of attribution. If users may enter a reported speaker, preserve that as separate content rather than replacing the authoritative actor. The event should answer who performed the system action without confusing that fact with text supplied in the request.

## Timeline reads have their own contract

TimelineAsync delegates directly to ListEventsAsync without checking whether the work order exists. An unknown work-order ID can therefore produce an empty event collection through this service path rather than work_order.not_found. A client must not infer from an empty timeline alone that the order exists but has no activity.

The queue queries filter by work-order ID and sort by their timestamp field. They return arrays of immutable records, which protects the returned collection's membership and record values from later mutation. Another append after the query does not retroactively add an element to the already returned array. This is a different ownership shape from live WorkOrder objects.

The compact store methods do not explicitly check their cancellation tokens. Passing a canceled token does not make those enqueue methods reject in the inspected implementation. A cancellation-aware history contract would require implementation changes and tests. Again, do not generalize the parts store's pre-commit cancellation policy to every module.

## Choose the audit requirement explicitly

One proposal treats the comment and its audit event as one required commit. A combined append command can validate and publish both under a shared authority, with a durable transaction for a durable deployment. Readers then observe a documented consistent relationship. A stable operation ID can make retries return the existing comment instead of appending another.

Another proposal treats the event stream as a projection derived from committed comments. The comment record carries enough identity for a projection worker to create the event idempotently, and delayed projection is visible in operational metrics. This can be valid when the product accepts eventual timeline completeness. It must not claim immediate atomic audit publication.

A best-effort event append that silently drops failures is a different and weaker policy. If chosen for noncritical diagnostics, say so. If the event is relied on for accountability, silently losing it contradicts the requirement. The word audit should trigger a concrete retention and completeness contract rather than merely a record class name.

## Guided lab and independent exercises

Use the disposable workflow lab's before-event failure wrapper to reproduce one comment and zero events. Inspect both streams and the caller exception. Then design a combined append prototype in the generated workspace or a receipt-based projection model. Keep the current application unchanged and label any durable guarantees as unimplemented until real storage tests exist.

**C10.1: Two-stream ledger.** Record normal completion, validation rejection, before-comment failure, before-event failure, and an identical retry after the latter. Include IDs, stream counts, caller outcome, and which records can be correlated reliably.

**C10.2: Audit contract.** Choose atomic pair or eventual projection semantics. Define operation identity, event-to-comment linkage, retry behavior, and a completeness metric. Explain how a reader distinguishes an unknown work order from an empty known timeline.

**C10.3: Actor evidence.** Propose separate fields for verified actor and user-supplied attribution. Describe where each is created and validated, and write one test showing that changing Author text cannot impersonate another authoritative actor under your proposed design.

## Review checkpoint

Test timeline existence separately from event filtering. A known order with no events and an unknown order currently produce the same empty collection through TimelineAsync. If the API must distinguish them, add an explicit existence decision and document its consistency boundary. Adding an event merely to prove that an order exists would mix resource identity with activity history and complicate retention.

Your history design should state whether the event is required evidence, a delayed projection, or optional diagnostics. Its publication mechanism, retry behavior, and actor fields must match that meaning. The two-stream failure trace gives you a concrete way to review that agreement rather than relying on the reassuring name AuditEvent.
