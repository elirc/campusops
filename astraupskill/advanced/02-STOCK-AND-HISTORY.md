# 2. Reconcile quantities with the event that explains them

Use one part with OnHand 3, Reserved 0 and Available 3. A reservation request asks for two units for work order W at fixed time T. In [ReserveForWorkOrderAsync](../../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs), the lock begins before cancellation and input checks. A missing part or insufficient stock returns null without creating history.

| Point | Stored OnHand | Stored Reserved | History count | Candidate |
|---|---:|---:|---:|---|
| Before request | 3 | 0 | 0 | none |
| After validation | 3 | 0 | 0 | copied Part |
| After candidate Reserve | 3 | 0 | 0 | Reserved 2 |
| After reservation record prepared | 3 | 0 | 0 | Part plus immutable receipt |
| Inside committed update | 3 | 2 | 1 | stored |
| After return | 3 | 2 | 1 | receipt returned |

The implementation inserts a reservation record and then assigns the updated part while holding the gate. There is no await, external callback or second store between these writes. Other methods using that gate cannot observe the intermediate dictionary state. This is an in-process logical commit for normal execution, not a claim of survival under process termination or out-of-memory failure.

~~~mermaid
flowchart TD
    A[Request enters shared gate] --> B[Check cancellation and identities]
    B --> C{Positive quantity and available stock?}
    C -- no --> D[Reject or return null with no writes]
    C -- yes --> E[Copy Part and validate candidate]
    E --> F[Create immutable reservation receipt]
    F --> G[Insert receipt and replace stored Part]
    G --> H[Release gate and return result]
~~~

Now send two further one-unit requests. The first succeeds and reaches Reserved 3. The next returns null, so the service translates it to part.stock_insufficient. The store intentionally conflates missing part and insufficient stock into that null outcome. If a future API needs to distinguish them, change the contract explicitly; do not guess based on a racy extra read after failure.

For this reserve-only scenario, reconcile Stored Reserved with the sum of reservation quantities. That identity is valuable in the lab because no consumption or release operation participates. It is not a universal accounting rule once you add consumption, cancellation or release history. A richer ledger must distinguish created, released and consumed quantities rather than summing every old reservation forever.

Trace cancellation in three positions: before entry, while waiting for the lock, and after mutation. The token is checked inside the lock before commit, so a cancellation that arrives while waiting is noticed when the caller acquires the gate. No cancellation check is placed after the mutation to turn a completed reservation into an apparent no-effect cancellation. A network response can still be lost independently.

Finally consider duplicate requests. Every successful call currently creates a fresh reservation ID. Two identical HTTP requests can both reserve if stock permits. The duplicate-ID guard in AddAsync prevents replacement of an existing Part; it is not idempotency for reservation requests. Put those two duplicate concepts in separate columns in your notes.

Your trace is complete when it records quantities, history, return value and observation point for each path. Do not merely count "three successes"; show that three returned receipts correspond to three unique records and three reserved units.
