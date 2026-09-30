# 4. Specify the boundary before choosing a mechanism

The current inventory adapter implements both IPartStore and IPartReservationStore. [Program.cs](../../src/CampusOps.Api/Program.cs) forwards those interfaces to the same concrete singleton. If each interface resolved to a separate instance, writes and reads could disagree even when each dictionary was perfectly locked. Correct dependency lifetimes are part of the feature's consistency contract.

A lock establishes mutual exclusion among code using the same gate in one process. It does not protect a second API instance, the separate worker process, or a browser's stale state. The stock and history dictionaries disappear when the process stops. The existing SQLite example handles locations and assets; it does not secretly persist inventory. Keep these statements visible in every deployment proposal.

Consider cancellation as a protocol. Before commit, cancellation should stop work without an effect. After commit, a response may fail to reach the caller. That uncertainty cannot be eliminated by another token check. For a future durable command, store a request receipt and allow the caller to query or safely retry. A cancellation response should not be interpreted as proof of no write unless the implementation actually provides that guarantee.

Now distinguish three duplicates: duplicate Part identity on AddAsync; duplicate reservation request after a lost response; duplicate receipt identifier. The current adapter rejects the first, generates IDs for the third, and does not deduplicate the second. A future idempotency key should be scoped to a work order and verified actor or another justified domain boundary. Same key plus different part or quantity must not replay an unrelated receipt.

A durable adapter needs one database transaction containing the conditional stock update, reservation insertion and idempotency receipt. Put availability in the update predicate and inspect the affected-row count. A count of zero means no matching row met the condition; it does not automatically tell you whether the part was missing or stock exhausted. Lock contention is yet another outcome and should not be mislabeled as a business shortage.

For a competing-connection test, use separate connections and separate DbContexts against a temporary database file. Sharing one DbContext across concurrent tasks tests unsupported context usage. An in-memory connection held open can be useful for small tests, but it does not demonstrate restart durability. To claim durability, commit, close every context and connection, then read from a newly opened connection or process.

Ownership checks also span boundaries. PartsService currently verifies that a work order exists; it does not reject terminal statuses. If you add that rule, checking a mutable work order before a separate inventory commit leaves a possible transition race. Decide whether version checks, a shared transaction or a deliberately weaker contract handles concurrent completion. Do not say "we checked it" as though the observation freezes time.

Failure injection should occur at named points: before conditional update, after update but before receipt insert, before commit, and after commit but before response. For each point, predict stock, history and replay result. A read from a fresh context is stronger persisted-state evidence than inspecting a tracked entity, which may be stale after a direct update.

A good handoff separates tested single-process atomicity, proposed database coordination and unproven cross-process behavior. This makes the next experiment obvious and prevents the word "transactional" from hiding missing tests.
