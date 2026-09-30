# 5. Exercises and a reservation feature capstone

Use the disposable lab for source-level experiments. Keep each exercise's contract in a short note before changing code. These are proposed learner extensions, not claims that the original application already contains them.

## C1 - Trace both alias directions
Repair input isolation and read isolation separately. Acceptance checks mutation of the object supplied to AddAsync, the object returned by GetAsync, and an element returned by ListAsync. Explain why copying only on reads leaves the original input as a back door.

## C2 - Capacity boundaries
Start at int.MaxValue minus two. Receiving two succeeds; receiving three in a fresh fixture rejects without mutation. Reserve one unit before a separate receive operation and show that receiving changes OnHand and Available but not Reserved. Assert part.quantity_overflow rather than any exception.

## C3 - Cancellation checkpoints
Test cancelled AddAsync and cancelled reservation. Predict the existing collection contents as well as the response. Then describe cancellation while waiting for the gate. Acceptance identifies the precommit check and explains why there is no postcommit cancellation rejection in the adapter.

## C4 - Work-order lifecycle
Propose a rule that Completed and Cancelled orders cannot receive more reservations. Use [WorkOrder.cs](../../src/CampusOps.Core/WorkOrders/WorkOrder.cs) to construct legal transitions instead of assigning a private status. Cover one allowed state and both terminal states. State the race with a concurrent completion and do not claim a cross-module transaction from a preliminary read.

## C5 - Duplicates have different meanings
Keep duplicate Part ID rejection. Add a design test for a replayed reservation command with a caller request key. Same scope, key and payload should return the original receipt; a changed quantity should conflict. A repeated request without a key remains a separate command. Write down your key normalisation and retention policy.

## C6 - History filtering and reconciliation
Reserve one unit for W1 and two for W2 from the same part. Each history query must return only its work order's receipts. Reconcile the sum across both histories with Reserved. Explain why the equality needs adjustment if your feature later supports consumption or release.

## C7 - Force a losing race
Run sixteen competitors for three units and assert exactly three receipts and zero available units. Then sketch a deterministic unsafe read-then-write adapter using a gate between the read and write. Acceptance explains the stale observation and the needed conditional update without relying on an arbitrary sleep.

## C8 - Make a failure outcome explicit
For a future database adapter, inject an exception after stock update and before reservation insertion. Acceptance requires a fresh-connection read showing both tables unchanged after rollback. Add the contrasting after-commit lost-response case, where state exists and a retry must discover the original receipt. Do not require an empty database after every thrown exception.

## Capstone - Inventory reservation console with reliable retries

Build a proposed reservation screen for an existing work order: stock list, part detail, quantity input, pending state, receipt and history. Reuse the current endpoint contracts until your request-key extension is deliberately introduced. The current React console focuses on work orders; this is new learner work, not a hidden finished inventory UI.

Keep numeric entry as text until validation. Empty, fractional, negative and excessive values need distinct handling. After a successful command, refresh stock and history together or update them from an authoritative receipt. After a timeout, preserve the request key and show an uncertain-result state; generating a new key immediately can reserve twice.

Deliver the screen state machine, accessibility checklist, server-side ownership policy proposal and command contract. Acceptance includes keyboard use, readable error text, preserved inputs, a forbidden response, an exhausted-stock response, and replay after a simulated lost response. Client-side disabled buttons are helpful but are not concurrency enforcement.

Keep durable implementation as a separate milestone unless you can add conditional updates, transactional receipts and competing independent-connection evidence. Explain that in-memory inventory resets on restart and that a role check does not establish campus ownership. A polished UI must communicate the actual system boundary instead of disguising it.
