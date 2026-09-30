# 14. Webhook enqueue and retry state

CampusOps models webhook subscriptions and delivery attempts and provides a publisher that enqueues deliveries. It does not show an HTTP sending loop in the inspected integration implementation. This chapter separates enqueue idempotency, delivery state, secret references, and external effects so that a plausible class name does not become an unsupported integration guarantee.

Read [Webhooks](../../../src/CampusOps.Core/Integrations/Webhooks.cs), [in-memory integration stores](../../../src/CampusOps.Infrastructure/Integrations/InMemoryWebhookStore.cs), [webhook tests](../../../tests/CampusOps.Infrastructure.Tests/Integrations/WebhookTests.cs), and [webhook endpoints](../../../src/CampusOps.Api/Integrations/WebhookEndpoints.cs). Use the reserved example.test host in exercises. The new lab performs no network requests and requires no real secret.

## A subscription stores intent

WebhookSubscription requires a nonempty ID, nonblank event type, HTTPS endpoint, and nonblank secret reference name. It trims the event type and secret name and starts active. Disable changes IsActive false. The object stores a URI and a reference label, not a fetched secret value or a verified remote service identity.

Requiring HTTPS constrains the scheme but does not establish endpoint reachability, ownership, allowed network destination, or successful authentication. Those concerns belong to a proposed delivery boundary. A subscription constructor can be correct within its small contract while insufficient for a production integration configuration workflow.

EventType matching in the in-memory store uses string equality on the stored trimmed value and requested type. The publisher does not normalize the supplied event type in the same way. Case or surrounding-space differences can therefore affect subscription selection. A proposed event catalog should define normalization or exact naming deliberately rather than relying on inconsistent caller conventions.

## Enqueue claims idempotency before work

WebhookPublisher calls TryBeginAsync with a key formed from eventType, a colon, and eventId. If the key already exists, it returns zero. Otherwise it lists subscriptions, filters active ones, and adds one delivery for each. It returns the number of active subscriptions in the retrieved collection after the loop.

The in-memory idempotency store uses ConcurrentDictionary.TryAdd, so one caller wins insertion of a key within that store instance. However, the key claim is separate from delivery creation. A failure after TryBegin succeeds can leave the key recorded even when some or all deliveries were never added. A retry then returns zero instead of filling the missing work.

The existing duplicate-event test verifies the ordinary successful case: one subscription, first enqueue returns one, identical second call returns zero, and one delivery exists. It does not inject a list or add failure after the idempotency claim. A passing duplicate test is therefore not evidence that enqueue is recoverably idempotent under partial failure.

## Worked partial enqueue

Configure two active subscriptions and a store wrapper that succeeds on the first AddDeliveryAsync but throws before the second. The publisher has already claimed the event key. After the fault, one delivery exists and the method did not return its normal count. Repeating the same event key returns zero, leaving the second subscription without a delivery.

Even a failure before listing subscriptions can burn the key in the current design. This is the simplest deterministic lab case because no enumeration order is needed. The designated failure occurs after the idempotency store reports success but before any delivery insertion. A retry produces zero despite an empty delivery list.

A proposed repair can commit an event receipt and all intended delivery records together, or record a durable per-subscription progress plan that retries idempotently. The key should represent an operation outcome, not just the fact that one attempt started. Renaming TryBegin to something reassuring does not fix the missing completion protocol.

## Key composition and payload identity

The current key includes event type and event ID but not payload. Reusing that key with a different payload is silently suppressed after the first claim rather than reported as a conflicting request. A proposed receipt contract should compare meaningful payload identity if conflicting reuse is an error.

Delimiter concatenation also needs a defined input grammar. If event type and ID may contain the delimiter, different pairs can produce the same combined string. A structured key or unambiguous encoding avoids that ambiguity. The model currently does not constrain eventId or eventType to a delimiter-free catalog at the publisher boundary.

Do not solve this by making a new random event ID for every retry. That removes deduplication and can create duplicate external effects. The producer should retain a stable event identity for one logical event, while a genuinely new event receives a new identity. This is the same intent distinction as inventory receipts, now applied to fan-out delivery.

## Delivery state is a small local machine

WebhookDelivery stores delivery ID, event ID, subscription ID, endpoint, payload, attempt count, next attempt instant, and status. The enum's default value is Pending. MarkSucceeded sets Succeeded and increments Attempts. MarkFailed increments Attempts, sets DeadLetter when the count reaches the supplied maximum, or schedules a later attempt using an exponential minutes expression.

The methods do not guard against calling success or failure after a terminal state. MarkSucceeded can be called on a DeadLetter delivery, and MarkFailed after Succeeded does not necessarily reset status to Pending before scheduling. These are direct method observations to test if a stronger delivery lifecycle is proposed. A sending worker would need to enforce valid transitions or the domain should own them explicitly.

MaximumAttempts is supplied to each failure call and is not validated as positive in the method. A zero value makes the first failure dead-letter immediately. Large attempt counts can also raise date-range or numerical concerns when computing a future timestamp. Keep the supported retry policy bounded and validated rather than assuming every integer configuration is meaningful.

## Backoff is scheduling, not execution

After the first nonterminal failure, Attempts is one and the delay is two minutes. After the second, the delay is four minutes, provided the maximum has not been reached. The next instant is based on the supplied failure time, not the previous NextAttemptAt. Tests should use fixed instants and assert the exact sequence for the intended maximum.

Setting NextAttemptAt does not wait, send HTTP, sign a payload, or acknowledge a receiver. The inspected code models when another component might attempt delivery. Do not describe the application as already retrying webhooks over the network without finding and testing the sender component.

Likewise, SecretName remains on the subscription and is not copied into WebhookDelivery. A proposed sender must decide how it resolves the relevant secret, handles rotation, and signs or authenticates requests without exposing values in logs. The course uses references only and never asks learners to place real credentials in fixtures.

## External effects need their own idempotency

Even a perfectly atomic enqueue can lead to duplicate delivery attempts when a receiver processes a request but its response is lost. A retrying sender cannot infer remote nonexecution from a timeout. The event or delivery identity should let the receiver deduplicate under an agreed protocol when the product requires it.

This is separate from suppressing duplicate EnqueueAsync calls. Producer-side deduplication controls creation of delivery records. Receiver-side deduplication controls repeated external effects. A design claiming exactly-once behavior must state the actual participating protocol and failure assumptions; the current local dictionaries do not provide that guarantee.

For a bounded capstone, specify at-least-once attempts with stable identity and idempotent receiver behavior rather than promising an impossible certainty from one success flag. Record pending, succeeded, and dead-letter outcomes with enough evidence to reconcile ambiguous cases. The operational runbook should explain manual replay without generating a new logical event accidentally.

## Guided investigation and exercises

Use a failing subscription-list wrapper after a real idempotency claim to reproduce an empty queue followed by duplicate suppression. Separately test the delivery object's attempt sequence and terminal-state calls with fixed values. Both experiments remain entirely in memory and send no request to the example URI.

**C14.1: Enqueue ledger.** Record first success, duplicate success retry, failure after key claim, and partial two-subscription insertion. Include key state, delivery count, returned count or exception, and what the next retry can do.

**C14.2: Retry policy.** Specify maximum attempts, first two backoff delays, terminal transitions, and payload identity. Propose guards for invalid configuration and calls after terminal state, with unchanged-state assertions on rejection.

**C14.3: Delivery boundary.** Design a sender/receiver contract with stable event identity, secret-reference resolution, and ambiguous-response handling. Separate enqueue completion from actual delivery and avoid claiming that the current model already performs HTTP retries.

## Review checkpoint

Also decide what disabling a subscription means for work already queued. The publisher filters activity when creating deliveries, but an existing delivery retains its endpoint and has no direct live IsActive check in this model. A sender proposal must choose whether queued work is canceled, preserved, or reviewed after disablement. Test that policy independently from the simpler rule that an inactive subscription receives no newly enqueued delivery.

Your integration design should distinguish claimed intent, queued work, attempted transport, acknowledged outcome, and remote effect. Each boundary needs its own evidence and retry rule. The current code is a useful teaching model precisely because those boundaries can be inspected without hiding them behind a network client.
