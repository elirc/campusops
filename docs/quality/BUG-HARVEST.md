# Sprint 14 bug harvest

Three integration failures become executable counterexamples:

1. Competing part reservations could both read the same availability. `TryReserveAsync` now owns compare-and-change atomically in the adapter.
2. A retried external event could enqueue duplicate deliveries. An idempotency store accepts an event key once.
3. Two operators could act on stale work-order state. Commands now carry an expected version and reject conflicts.

Webhook delivery records retry state and dead-letter after a configured maximum. Secrets are referenced by name, never stored in subscriptions. No distributed lock or message broker is added; a production durable adapter would implement these same contracts with database constraints and an outbox transaction.
