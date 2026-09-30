# 6. Solution guide and review rubric

Read this after attempting each exercise. The important part is which observation makes a claim defensible. The same implementation can be correct for one boundary and insufficient for a stronger one.

C1 requires copies on both ingress and egress. AddAsync retains a snapshot so later mutation of the caller's original cannot affect storage. GetAsync and ListAsync copy again so consumers cannot obtain stored mutable instances. A new array around live objects is not enough. Part.Snapshot is shallow; if a mutable reference field is added, revisit the design rather than treating MemberwiseClone as permanent deep-copy protection.

C2 accepts exactly the remaining integer capacity and rejects one unit beyond it. Check the subtraction form before adding to avoid overflowing while evaluating the guard itself. Reserved remains unchanged during Receive. Available follows OnHand minus Reserved. An exception-only test could miss a guard placed after the mutation or arithmetic that wrapped before the check.

C3 checks the cancellation token inside the shared gate before any mutation. A request cancelled while waiting will be rejected when it enters. Cancellation after a committed operation is a different event from cancellation before commit; do not add a late check that falsely implies no effect. The test should inspect both dictionaries or their public read methods.

C4 belongs in an explicit application rule with a stable error contract. Construct Completed through the legal assign, start and complete sequence; Cancelled can be reached through the supported cancellation method. A status check followed by a separate reservation still permits an interleaving. A robust cross-module solution needs a version or transaction strategy, while a teaching prototype may document the weaker promise.

C5 needs a request receipt coupled to the inventory commit. A separate deduplication lookup followed by an unlocked reservation races. A replay with identical normalised payload returns the stored receipt. Changed payload conflicts. Duplicate Part IDs remain a creation rule and should never be "fixed" by silently replacing quantities. Choose a scope and expiration policy before adding a dictionary of keys.

C6 filters by WorkOrderId and reconciles across both orders. The immutable record and unique IDs make the trace readable. With only reservations, the total quantity equals Reserved. Once release or consumption exists, a reservation history alone no longer describes current reserved stock. Introduce explicit event types or stateful receipts and update reconciliation accordingly.

C7's unsafe design lets two callers observe the same availability before either commits. A gate at that point makes the interleaving reproducible. The shipped shared gate serialises the complete decision. A database version should use a conditional update and one transaction; a client read followed by a plain update repeats the logical mistake across processes.

C8 distinguishes rollback from acknowledgement loss. Before commit, a failed receipt insert should roll back the stock change. After commit, durable state must remain, and the caller needs a way to discover it. An exception is not a reliable indicator of whether a write exists. Independent connections and a reopened database make the claimed persistence boundary explicit.

For the capstone, a useful state machine includes idle, validating, pending, succeeded, rejected, forbidden and uncertain-result states. Keep the same request identity during uncertainty. Refresh both stock and receipt history after resolution. Permission must be established on the server from trustworthy identity and resource scope; a hidden button or selected work-order row is not an authorization boundary.

| Dimension | 0 | 1 | 2 |
|---|---|---|---|
| Ownership | Live objects leak | One copy boundary | Ingress and egress explained and tested |
| Invariants | Success path only | Quantity assertions | Stock, history and identities reconciled |
| Failure lab | Old files overwrite app | Compiling red without diagnosis | Disposable five-red/seven-green evidence |
| Cancellation/retries | All failures mean no effect | Some cases separated | Before/after commit and replay specified |
| Durability | Lock called durable | Database plan only | Independent connection/restart evidence scoped |
| Feature handoff | UI happy path | Error states | Accessible uncertainty and ownership contract |

Aim for ten of twelve with no zero in invariants or durability. Have a reviewer explain why the starter competition and cancelled-reservation checks remain green while five ownership/capacity checks fail. If that distinction is unclear, revisit the test contracts before adding more feature code.
