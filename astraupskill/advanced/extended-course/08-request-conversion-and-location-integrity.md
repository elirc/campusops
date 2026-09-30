# 08. Request conversion and location integrity

A maintenance request becomes a work order through a short service method, but that conversion spans two records and carries location and asset references into operational work. This chapter traces the conversion's failure boundary and compares referential checks across request and asset creation. The goal is to distinguish locally valid fields from a verified relationship in the campus model.

Read [MaintenanceRequest](../../../src/CampusOps.Core/Requests/MaintenanceRequest.cs), [WorkOrderService.ConvertAsync](../../../src/CampusOps.Core/WorkOrders/WorkOrder.cs), [AssetService](../../../src/CampusOps.Core/Assets/Asset.cs), [Location](../../../src/CampusOps.Core/Locations/Location.cs), and [request tests](../../../tests/CampusOps.Core.Tests/Requests/MaintenanceRequestTests.cs). Use invented campus and asset IDs; no real facilities data is required.

## Request validation is local

MaintenanceRequest requires nonempty request and location IDs. Summary must be nonblank and at most one hundred twenty characters after trimming. Description must be nonblank. The constructor trims text and normalizes SubmittedAt to UTC. AssetId is optional, and the constructor does not load either a location or an asset.

MaintenanceRequestService.SubmitAsync depends only on the request store. It constructs a request and adds it. Therefore, the current service validates that LocationId is not empty, not that it names an existing Location record. It also does not verify that an optional asset exists or belongs to the supplied location. Those are proposed referential policies, not hidden constructor behavior.

This distinction is important for API clients. A syntactically valid nonempty GUID can still be an unknown location. If the product needs all requests anchored to known facilities, the enforcement boundary must query or otherwise validate that relationship. A DTO containing a LocationId field does not establish the relationship by itself.

## Triage controls conversion eligibility

A request starts Submitted with Normal priority. Triage accepts only Submitted, sets the supplied priority, and moves to Triaged. Repeating triage rejects request.not_submitted. MarkConverted accepts only Triaged and moves to Converted. The enum also names Cancelled, but this class does not expose a cancellation method in the inspected source.

The existing tests verify triage changes priority and status and that a second triage rejects. They do not establish priority membership validation for every possible enum value or a complete cancellation workflow. A method receiving an enum can still be called with an unnamed numeric value unless it explicitly validates membership. Keep direct domain behavior separate from request parsing behavior.

Conversion copies the request's location, asset, priority, and summary into the new work order. These values become immutable properties there. That provides a stable operational snapshot of the request's selected fields, but it does not retroactively verify that the referenced location or asset was valid at submission or conversion.

## Conversion mutates the source first

ConvertAsync loads the request, calls MarkConverted, constructs a new WorkOrder, and then calls orders.AddAsync. With the default request store's live references, the request status change is immediately visible. If the work-order addition fails before storage, the request remains Converted without a corresponding added order in the controlled experiment.

This is a concrete validation-versus-persistence distinction. MarkConverted correctly rejects an untriaged request before any order creation. But a valid triaged request can still encounter a later store failure after its status changed. The method does not coordinate both records in one transaction and does not restore the request status on failure.

A retry can then reject request.not_triaged because Converted no longer satisfies MarkConverted. The client sees a second error even though the first call failed to create the expected work order. A recovery design needs an operation outcome or a relationship lookup that can distinguish completed conversion from partial state. Simply retrying the same service call is not a complete recovery policy.

## Worked conversion ledger

Begin with request R, status Triaged, priority High, location L, asset A, and summary Repair leak. The work-order store is empty. After MarkConverted, R is Converted and no work order is stored. Construction creates local order W with a new ID, R's identity and copied fields, Open status, and version zero. After a successful add, W is stored and returned.

Now place a designated exception before the add writes. The local W may have been constructed, but the store remains empty. R stays Converted through its live reference. Record both the local candidate and durable-or-stored observation separately. A generated GUID for W is not evidence that another caller can retrieve it.

Moving MarkConverted after AddAsync would shift the partial-failure window rather than eliminate it. The order could be added before the request transition fails or another caller converts the same request. A correct stronger design must coordinate the relationship, not merely swap the two effectful statements and declare the operation atomic.

## A proposed conversion authority

One proposal gives a store or unit of work responsibility for converting a request exactly once, creating the linked order, and recording a receipt in one coordinated commit. A uniqueness rule on the converted request identity can prevent two orders from representing the same intended conversion when that is the product policy. An expected request version can detect stale eligibility decisions.

The result should distinguish a first conversion, an identical retry of a completed conversion, and an incompatible request state. A completed retry can return the existing order rather than attempt another MarkConverted. If the application permits several work orders from one request, that is a different policy requiring an explicit conversion or split identity rather than a blanket uniqueness constraint.

For a same-process prototype, the authority must own both relevant collections or coordinate their operations safely. For a durable design, the records need a real transaction or a justified recovery protocol. A catch block that manually changes a private status back would bypass the domain model and still fail under crashes or competing writers.

## Asset creation checks a different relationship

AssetService.CreateAsync does load the location and rejects location.not_found before constructing an Asset. That is stronger referential evidence than the request submission path. The Asset constructor also rejects empty asset and location IDs and blank tag, name, or category. It normalizes the tag and trims descriptive fields.

Asset.MoveTo, however, only rejects Retired status before assigning the supplied location ID. It does not reject Guid.Empty or load a destination. There is no move service in the inspected AssetService that adds such a lookup. A direct method call can therefore produce a location value that the constructor would reject. This is a bounded domain observation, not proof of an exposed HTTP move route.

A proposed move workflow should validate destination existence, actor authority, and any work-order implications. Moving an asset does not automatically rewrite historical requests or work orders that copied an earlier location. Decide whether those records represent historical location, current location, or both. The answer affects reporting and technician dispatch, not just one GUID assignment.

## Location labels and identity

Location validates campus, building, and room strings and a nonempty ID. It trims all three and uppercases Room. DisplayName combines the labels. Those labels are useful for display but do not prove uniqueness across records. Two Location objects can have the same labels under different IDs unless another boundary prevents it.

Avoid using DisplayName as a hidden foreign key. A campus label can change in a larger product, and formatting choices should not determine whether an asset belongs to a location. The current model uses GUID references, which separates identity from presentation. A future location-editing feature must decide how historical displays are reconstructed without confusing label updates with identity changes.

The in-memory location store checks cancellation on add and get but not in its compact ListAsync implementation. This is another reminder that similarly shaped interfaces can have different token behavior. The continuation's broader cancellation review should inspect each actual path rather than transfer the parts store's policy automatically.

## Guided lab and independent exercises

Use a throwing work-order store in a disposable workspace to reproduce Converted request plus no stored order. Catch only the designated fault, then read the request again through its real in-memory store. Separately call MoveTo with an empty ID on a nonretired Asset to characterize the local method boundary. Neither observation changes the original application.

**C08.1: Conversion outcomes.** Specify untriaged rejection, successful conversion, and before-add failure. Record request status, local order existence, stored order count, and retry behavior for each. Explain why reversing effect order is not a complete transaction design.

**C08.2: Referential policy.** Propose request submission rules for known location, optional asset existence, and asset-location agreement. Decide whether an asset move after submission changes the request's meaning. Name the authority and observation time for those decisions.

**C08.3: Move contract.** Design a safe persisted asset-move command with destination validation and an explicit historical-location policy. Distinguish the current direct method from the proposed service and identify which tests belong at each boundary.

## Review checkpoint

Your relationship map should show where IDs are merely nonempty, where records are actually looked up, and where state spans two stores without a shared commit. That map prevents a locally valid request or asset from being mistaken for a fully consistent facilities workflow.
