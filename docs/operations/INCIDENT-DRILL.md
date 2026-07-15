# Incident drill: duplicated emergency work

Two active emergency work orders appear for one external event.

1. Assign an incident owner and record detection time.
2. Determine whether duplicates continue; disable only the affected subscription if necessary.
3. Preserve event IDs, idempotency records, delivery attempts, work-order versions, and correlation IDs.
4. Identify the authoritative work with operations; cancel duplicates without deleting history.
5. Reproduce with a deterministic idempotency regression.
6. Repair the owning durable boundary and verify replay, retry, and dead-letter behavior.
7. Write a blameless timeline and assign measurable prevention work.

Success is protected operations plus a verified prevention, not merely restarting a service.
