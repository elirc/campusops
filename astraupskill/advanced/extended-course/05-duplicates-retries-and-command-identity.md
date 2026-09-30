# 05. Duplicates, retries, and command identity

Two requests with identical fields can mean either two legitimate allocations or one allocation retried after a lost response. The current parts command cannot distinguish those intentions because it has no caller-supplied operation identity. This chapter separates duplicate entity protection from logical request idempotency and develops a concrete receipt proposal tied to CampusOps inventory.

Read [PartReservation and store contracts](../../../src/CampusOps.Core/Inventory/Part.cs), [InMemoryPartStore](../../../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs), and [the duplicate-ID test](../../../tests/CampusOps.Infrastructure.Tests/Inventory/PartStoreIsolationTests.cs). The proposed receipt types and behaviors are design work for a disposable capstone; they are not features already present in the application.

## Duplicate part identity is already protected

AddAsync uses the Part GUID as its dictionary key and rejects another Part with that same ID. This protects an existing stock aggregate from replacement. The current test adds five units, attempts to add a different object with the same ID and one hundred units, and verifies part.already_exists plus unchanged OnHand.

That is a meaningful duplicate rule, but it answers a different question from retrying ReserveForWorkOrderAsync. The reservation command generates a fresh reservation GUID internally for each accepted call. Repeating work-order ID, part ID, quantity, and timestamp does not reuse that generated identity. If enough stock remains, each call can commit another allocation and record.

SKU normalization is a third concern. Trimming and uppercasing a SKU makes representations consistent, but the dictionary is not keyed by SKU and does not reject another ID using the same normalized SKU. Keep these three questions separate: may an aggregate ID be replaced, may a business SKU identify several aggregates, and does a repeated command represent the same operation?

## Worked retry ambiguity

Start with ten filters available. A client sends a command reserving two for work order W. The store commits and returns a record, but imagine the transport response is lost after that return. The client does not know whether the reservation happened. It sends the same fields again, and the current store can reserve another two.

The final aggregate has four reserved and six available, with two records for W. Every local stock/history invariant remains correct: each accepted command has a matching record. The user-intent invariant may be wrong if the second request was only a retry. This is a subtle but important result: atomicity does not automatically provide idempotency.

If the original stock had been only two, the retry would instead fail for insufficient stock. That failure still would not explain whether the first request committed or another caller used the stock. A client cannot reliably infer its prior outcome from current availability. It needs a stable way to identify the logical operation.

## A proposed operation key

Add an operation ID supplied once for a logical reservation intent and retained across retries. Scope the key to an appropriate tenant, actor, or application boundary so unrelated callers cannot accidentally collide. Store a canonical representation or hash of the meaningful request fields alongside the outcome. Identical retries can then return the original receipt, while conflicting reuse can reject.

The payload comparison must include work-order ID, part ID, and quantity. Decide whether the requested timestamp is client input or server-selected observation; do not compare a freshly generated server timestamp on each retry as though it were part of the caller's stable intent. An actor context may also affect authorization and key scope without belonging directly in the public DTO.

Define what happens when a receipt exists but the request fields differ. Silently returning the old allocation would mislead the caller about which request succeeded. Creating a second allocation under the same key would defeat uniqueness. A defined conflict response is usually the clearest proposed behavior, and it should leave the original receipt and stock unchanged.

## The receipt belongs to the commit set

The current gate coordinates Part and PartReservation. A same-process receipt extension could store the operation outcome under that same authority, preparing the record and receipt before publication. A duplicate key lookup and publication must be coordinated together so two simultaneous first attempts do not both decide the key is absent.

A receipt written only after stock and reservation publication leaves another failure gap. If that write fails, the allocation exists but the retry lookup has no record of it. A durable implementation should commit stock, allocation, and receipt together when possible. If they cannot share a transaction, it needs an explicit recovery protocol rather than a hopeful final dictionary assignment.

The receipt should name the accepted reservation ID and enough outcome data to reconstruct a trustworthy response. Returning the same immutable reservation record can be suitable for an in-memory prototype. A durable design must specify storage representation, retention, and authorization for outcome lookup. The mere presence of an operation ID field is not the full feature.

## Rejections and retries need policy

Decide whether a rejected insufficient-stock attempt is remembered under its operation ID. If it is remembered forever as a final rejection, a later retry after replenishment still returns the original rejection. If it is not remembered, the same operation can later succeed as stock changes. Both policies have product consequences and should not be left to incidental implementation order.

One coherent proposal records successful outcomes and conflicting key use while treating certain pre-commit business rejections as uncommitted attempts. Another records all terminal decisions for audit and requires a new operation ID for a new attempt after rejection. The key is to tell clients when they are resolving an old outcome versus expressing a new intent.

Cancellation before the store accepts a command likewise needs a clear policy. If no receipt or business effect was published, a retry can be a first attempt under the same ID. Cancellation or response loss after completion should resolve to the committed receipt. This connects idempotency directly to the cancellation boundary from the previous chapter.

## Retention changes semantics

An operation record kept only in memory disappears on restart, along with current stock in the teaching default. A future durable receipt can survive restart, but it also needs a retention rule. Deleting receipts too soon can cause an old retry to be treated as a new allocation. Keeping them indefinitely may be unnecessary or operationally expensive.

Define a supported retry horizon and what the server returns when an operation ID is outside it. A client should not be told that an expired receipt proves the operation never happened. If business records retain the operation identity, a later lookup may still resolve the result; if not, the outcome may require reconciliation. Document that distinction in the proposed API contract.

Do not use a random request correlation header as the authoritative idempotency key unless the product explicitly defines and validates that role. Diagnostic identifiers and business operation identifiers have different uniqueness, trust, and lifetime requirements. Reusing one field casually can create confusing behavior across proxies, clients, and retries.

## Duplicate intent can still be legitimate

A technician may intentionally request two additional filters after an earlier reservation. The new command can have identical part and work-order IDs and quantity while carrying a new operation ID. The system should allow it if stock and policy permit. Deduplicating only on the tuple of business fields would incorrectly suppress legitimate repeated work.

Likewise, two operators may independently reserve supplies for the same work order. Whether that should be allowed is a workflow policy, not something idempotency can infer. A proposed per-work-order quantity cap or approval step addresses that need. Idempotency prevents repeating one identified command; it does not replace planning or allocation policy.

This distinction is useful in review language. Say "identical operation identity replays one outcome" rather than "duplicate reservations are impossible." The latter is ambiguous and may prohibit valid operations. Precise naming avoids implementing an overly broad uniqueness constraint that later has to be worked around.

## Guided lab: show both meanings

In a disposable reference workspace, reserve two units twice with the same fields and enough stock. Assert two distinct reservation IDs, two records, and four reserved units. Name the check as an observation that current commands are additive, not an assertion that every future API retry should behave this way.

Then sketch a receipt wrapper prototype with a stable key and immutable request payload. Test identical retry, conflicting reuse, two concurrent first attempts, and a new key with the same business fields. Keep the prototype's same-process scope explicit. Do not claim that a wrapper outside the existing commit authority provides atomic receipt publication unless your implementation actually coordinates the entire set.

## Independent exercises

**C05.1: Identity taxonomy.** Compare duplicate Part ID, duplicate SKU, repeated reservation fields, and repeated operation ID. For each, state current behavior where implemented and a proposed policy where absent. Include one legitimate repeated reservation that should not be suppressed.

**C05.2: Receipt contract.** Define first success, identical retry, conflicting reuse, pre-commit rejection, interrupted response, and expired receipt. State which outcomes are stored and how stock, allocation, and receipt remain consistent under your chosen boundary.

**C05.3: Client recovery.** Write a client flow after a reservation timeout. Explain how the same operation identity resolves uncertainty and why checking current Available alone cannot identify the earlier command's outcome.

## Review checkpoint

Your design should preserve the existing stock/history atomicity while adding a distinct logical-operation guarantee. It should allow new legitimate allocations, reject conflicting identity reuse, and explain retention and failed attempts. Those decisions make idempotency a concrete feature rather than a vague instruction to ignore duplicates.
