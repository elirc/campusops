# 11. Procurement thresholds and approval evidence

Procurement introduces money, vendor eligibility, and approval decisions into CampusOps. The implementation is deliberately compact: a service creates a quote and purchase request, submits it against a configured threshold, and stores the result. This chapter distinguishes the approval rule that exists from the purchasing, quote-expiry, and identity rules that a reader might incorrectly infer from the model's names.

Read [Procurement](../../../src/CampusOps.Core/Procurement/Procurement.cs), [procurement stores](../../../src/CampusOps.Infrastructure/Procurement/InMemoryProcurementStores.cs), [the threshold test](../../../tests/CampusOps.Core.Tests/Procurement/PurchaseRequestTests.cs), and [procurement endpoints](../../../src/CampusOps.Api/Procurement/ProcurementEndpoints.cs). All examples use invented vendors and amounts. The exercises are software-contract analysis, not advice about a real purchasing policy.

## Vendor validity has a narrow implementation

Vendor requires a nonempty ID, nonblank name, and a contactEmail string containing an at sign. It trims the name, trims and lowercases the email, and starts active. That check is a minimal teaching rule rather than proof that the address is deliverable or belongs to an authorized supplier. A string containing an at sign can still be unsuitable for a production contact workflow.

Deactivate sets IsActive false. ProcurementService checks that state before creating a request. Deactivation does not automatically reject existing pending requests or erase previously accepted quotes. If the product requires a purchasing hold on existing work, that is a separate workflow with its own timing and audit requirements.

The in-memory vendor store retains live references and assigns by ID. Its methods do not explicitly observe their cancellation tokens. These choices differ from the inventory store, so a canceled procurement request cannot inherit inventory's no-effects cancellation contract merely because both APIs accept a token. Inspect the actual call path when making that claim.

## Quote data is not quote verification

VendorQuote is a record with ID, vendor ID, reference, amount, and ValidUntil. ProcurementService constructs it using a new ID and a UTC-normalized validity instant. The record itself contains no validation method. PurchaseRequest validates a positive quote amount, but current creation does not compare ValidUntil with the current time.

Therefore, a quote whose validity instant is already in the past can still be used by the inspected CreateAsync path when the other checks pass. That is a precise current behavior, not a recommendation for purchasing. A proposed expiry rule needs an authoritative clock, an equality policy, and a decision point before committing the request or approval.

The reference string is also carried without a nonblank rule in VendorQuote construction. A business requirement that every quote have a supplier reference needs explicit validation. Naming a field Reference provides a place to store evidence; it does not establish that valid evidence was supplied.

## The threshold comparison includes equality

PurchaseRequest starts Draft. Submit accepts only Draft and compares Quote.Amount greater than approvalThreshold. Amounts above the threshold move to PendingApproval. Amounts at or below it move to Approved and set ApprovedBy to policy:auto. Program constructs ProcurementService with a threshold of one thousand decimal units.

The existing test covers 999 and 1001 around a threshold of 1000. Equality is a useful missing boundary case: 1000 is automatically approved under the strict greater-than comparison. A test that checks only values well below and well above would also pass if a mistaken greater-than-or-equal implementation sent equality for manual review.

The model does not validate the threshold itself as positive. A configured negative threshold would send every positive quote above it into PendingApproval. Whether that configuration should be rejected at startup is a proposed policy. Keep configuration validation separate from the positive amount rule on PurchaseRequest.

## Worked purchase trace

Vendor V is active. Work order W is a nonempty GUID. A caller supplies amount 1000, a reference, a validity instant, and justification Replace failed pump seal. The service loads V and checks activity, constructs the quote, constructs the request, and submits it. The request becomes Approved with policy:auto, then the store adds it.

Change only the amount to 1001. The request becomes PendingApproval and ApprovedBy remains null. A later Approve call requires PendingApproval and a nonblank approver string, trims the approver, and moves to Approved. Reject also requires PendingApproval and moves to Rejected. Neither operation creates an order or receives stock in the inspected model.

The enum includes Ordered, but PurchaseRequest exposes no method that transitions to Ordered. An enum value is not evidence of an implemented ordering workflow. Likewise, approval does not call Part.Receive or create an inventory receipt. A proposed purchase-to-stock feature must coordinate those steps explicitly instead of assuming that approval means physical goods arrived.

## Work-order identity is not looked up

PurchaseRequest requires a nonempty WorkOrderId. ProcurementService depends on vendor and purchase stores, not IWorkOrderStore, and therefore does not check whether the supplied work order exists. The current guarantee is local identity validation rather than referential integrity to an operational job.

A proposed procurement policy may require a known nonterminal work order, approved budget, and compatible location. Decide which facts must remain true at commit and which are historical decision evidence. A lookup followed by an unrelated add can still race with a work-order status change, so a stronger policy needs coordination or an explicitly accepted observation-time rule.

This is a recurring pattern in CampusOps: some services validate references through collaborators, while others accept nonempty IDs. The course does not impose one universal choice. It asks you to make the requirement explicit and test the boundary that actually enforces it.

## Approval actor and route authority differ

The procurement route group requires Manager. The approve endpoint loads a request and calls Approve with body.Approver. The domain method checks only nonblank text. An authorized manager can therefore supply an attribution string that is not automatically derived from the authenticated principal in this path.

A production audit proposal should record a verified actor separately from any supplied display name or on-behalf-of attribution. The route's role gate controls who can invoke the operation; it does not transform request text into trustworthy identity. This distinction mirrors the comment-history lesson while applying it to an approval decision with different business consequences.

The endpoint returns NotFound when the request is missing, while domain transition errors pass through the domain exception handler. A client should distinguish absence from an invalid approval state. Repeating approval on an already Approved request rejects rather than replaying a receipt, which matters after an interrupted response.

## Persistence and retry boundaries

The purchase store returns live objects, so Approve changes its stored state without an explicit save. A detached durable adapter would need an update contract. The current endpoint's simple mutation is not proof that approval survives process restart or that simultaneous approve and reject commands are atomically arbitrated.

CreateAsync generates a fresh purchase ID each time. Repeating identical fields can create another request because no operation identity deduplicates creation. A proposed approval receipt should define identical retry, conflicting decision, and the authoritative actor. If approval and an external ordering message must agree, their commit or delivery protocol needs separate design.

Do not solve a retry problem by silently accepting every call to Approve regardless of current state. A repeated approval might be the same logical request or a conflicting action after rejection. Stable operation identity and recorded outcome make that distinction possible; status alone may not explain the history.

## Guided investigation and exercises

In a disposable copied domain harness, test amounts 999, 1000, and 1001 against the configured threshold. Add a past ValidUntil value and observe that Submit's threshold decision is unchanged. Name the latter a current-policy observation, not a desired expiry rule. Then design a fixed-clock service test for a proposed expiry check before request insertion.

**C11.1: Decision table.** Cover below, equal, and above threshold; inactive vendor; nonpositive amount; blank justification; and an expired quote. State which layer rejects each implemented case and which condition currently remains unchecked.

**C11.2: Approval evidence.** Propose a receipt containing verified actor, request version, decision, and operation identity. Explain how approve-versus-reject competition and an identical retry should behave without treating supplied Approver text as authoritative identity.

**C11.3: Receiving boundary.** Design a purchase-to-inventory receipt command that records delivered quantity and updates stock. Distinguish approval, ordering, delivery, and receipt, and identify the shared records needed to prevent duplicate receiving.

## Review checkpoint

Keep the currency assumption visible as well. Amount is decimal, but VendorQuote has no currency field in this model. Comparing every amount to one threshold assumes a common unit chosen elsewhere. A future multicurrency feature cannot safely compare raw numbers without a defined conversion source, effective date, rounding policy, and retained evidence. The present exercise should use one invented unit consistently and avoid suggesting that decimal arithmetic alone supplies those financial semantics. This is a schema and policy question, not a reason to add a live exchange-rate dependency to the teaching lab.

Your procurement model should state exactly what the threshold decides and what it leaves undecided. A positive amount, active vendor, and nonempty work-order ID do not establish current quote validity, verified approval identity, durable ordering, or received inventory. Those are coherent future features when given explicit boundaries and evidence.
