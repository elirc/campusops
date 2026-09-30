# 15. API contracts and client observations

CampusOps has dedicated contract records for several resources, direct domain responses for others, and a domain exception handler that maps rule failures into problem details. This chapter reviews what a client can actually observe: accepted fields, response projections, resource locations, action semantics, and error categories. It connects those transport details to the consistency boundaries established earlier.

Read [part endpoints](../../../src/CampusOps.Api/Inventory/PartEndpoints.cs), [work-order endpoints](../../../src/CampusOps.Api/WorkOrders/WorkOrderEndpoints.cs), [procurement endpoints](../../../src/CampusOps.Api/Procurement/ProcurementEndpoints.cs), [request endpoints](../../../src/CampusOps.Api/Requests/MaintenanceRequestEndpoints.cs), and [DomainExceptionHandler](../../../src/CampusOps.Api/Errors/DomainExceptionHandler.cs). The deliverable is a client contract inventory, not a wholesale API rewrite.

## Request shape controls caller influence

CreatePartRequest supplies SKU, name, and initial OnHand. The endpoint generates a new Part ID and delegates addition to the store. ReservePartRequest supplies PartId and Quantity while the work-order ID comes from the route. The service selects the reservation instant through IClock. These choices define which facts the caller proposes and which facts the server supplies.

WorkOrderActionRequest carries ExpectedVersion and a possible reason, while the action is a route segment. The service compares exact action strings. A client must retain the observed version for a meaningful stale-state check, but the existing service does not provide an atomic expected-version mutation boundary. The API field expresses intent without supplying the missing coordination automatically.

Maintenance-request triage accepts a priority string and parses it case-insensitively into RequestPriority. A parsing success is not necessarily the same as membership in a catalog of named values when enum parsing accepts numeric representations. A proposed strict contract should validate defined values explicitly and test request behavior rather than rely on the type name alone.

## Response records are chosen views

PartResponse includes ID, SKU, name, OnHand, Reserved, and Available. It exposes the derived availability alongside its components, which is convenient for clients. The endpoint maps a detached part returned by the current store, so later store mutations do not alter that already-created response object. This does not mean the client may use its old availability as an authoritative future reservation decision.

WorkOrderResponse selects ID, request ID, title, priority and status strings, blocked reason, completion instant, and version. It does not expose every domain property, such as location, optional asset, or technician ID in this mapping. A client feature needing those values requires a deliberate contract extension rather than assuming serialization of the full entity.

Several procurement endpoints return domain objects directly. Their public properties therefore participate in the response shape under the application's serializer configuration. Adding a public property can affect clients without editing the endpoint. An explicit response record can stabilize the projection, but migrating to it should preserve or intentionally version existing fields.

## Creation locations need follow-up evidence

Part creation returns a location under /api/v1/parts/{id}, and the endpoint file maps a matching item GET. A client can therefore be tested with create, capture Location, and follow the URI under the required Operations policy. The test should use the actual returned header rather than reconstructing a route independently and missing a formatting defect.

Work-order conversion also returns a location containing the order ID, but the inspected WorkOrderEndpoints file maps a collection GET and action routes rather than an item GET at that location. A formatted URI does not prove a retrievable resource route exists. Audit the full route set before promising a read-after-create journey.

Vendor creation similarly emits a resource location while the shown procurement routes do not provide a corresponding vendor GET. The design can add a read route with proper scope or document the narrower identifier meaning, but the client contract should not leave the next action to guesswork. A passing creation test alone cannot establish navigability.

## Rule errors share one transport category

DomainExceptionHandler recognizes DomainException and writes status 400 with a title, detail message, and code extension. It returns false for other exception types. Thus quantity errors, transition errors, version conflicts, and service-thrown not-found rules can share the same HTTP status under this handler.

Direct endpoint branches can return different statuses. Part GET returns NotFound for a null lookup. Procurement approval returns NotFound for a missing request. A work-order service lookup may instead throw work_order.not_found and receive the handler's 400 mapping. A client should rely on the documented operation contract and code rather than assume all missing records follow one universal status.

A proposed taxonomy could map conflict, absence, validation, and forbidden resource scope more distinctly. Before changing it, identify clients and tests relying on current status and code behavior. Preserve stable machine-readable information and avoid requiring clients to parse human message text to decide whether to reload, correct input, or retry.

## Binding failures are a separate layer

Malformed JSON, an invalid route GUID, or an unreadable date can fail before the service's domain logic runs. The custom handler does not establish every framework-binding response. A test that sends malformed input and sees failure cannot claim it exercised part.quantity_invalid unless it verifies that specific path and code.

Use a layered test plan. Domain tests cover detailed quantity and state rules cheaply. A few request tests establish binding, routing, authorization, and response mapping. Adapter tests establish actual persistence. Repeating every arithmetic case through an HTTP host adds cost without necessarily improving evidence, while omitting request tests leaves important transport assumptions unverified.

The new course labs call copied domain and service code directly. They intentionally do not establish HTTP behavior. Their passing results should appear in a different section of the evidence report from proposed request tests. This separation keeps the course useful without pretending that every client contract has already been executed.

## A result can be correct but ambiguous to the caller

An inventory reservation can commit and return a record internally while the network response is lost. Retrying current fields creates another allocation if stock permits. A client receiving a timeout therefore cannot assume no reservation exists. A proposed operation receipt gives the client a stable outcome-discovery path.

A work-order conversion failure can leave the request Converted before an order was stored. A subsequent retry may report request.not_triaged. The second error does not explain the first partial outcome. A polished problem-details envelope improves presentation but does not repair the relationship or create an operation identity.

Design client flows around these semantics. Distinguish correcting invalid input, reloading stale resource state, resolving an interrupted operation, and submitting a new intent. A generic retry button that always sends a new request may be inappropriate for additive stock commands or procurement creation. The transport contract should make the safe next action understandable.

## Projection and enumeration timing

Some collection endpoints pass a Select projection over returned objects rather than materializing all response records immediately in the endpoint body. Serialization can enumerate that projection later. With live work-order objects, values can change between the store's list operation and mapping. With detached part snapshots, the fields are stable after the store returns.

This difference follows ownership, not just response type. Materializing a DTO array sooner can narrow observation timing but does not create a cross-store transaction. If a report promises one consistent snapshot, define the data-read boundary rather than assuming that immutable response records make their source observations atomic.

A proposed receipt should represent what an operation committed, while a resource response may represent current state at a later read. Those are different views. A returned work order that has already changed again under concurrency can confuse a client expecting an immutable record of its own action unless the API contract explains the distinction.

## Guided investigation and exercises

Build a table for part creation, reservation, work-order action, conversion, procurement approval, and timeline lookup. Record request fields, trusted server inputs, authorization metadata, success shape, missing-record behavior, and interrupted-result risk. Include which observations are source-inspected and which have executed request tests.

**C15.1: Client matrix.** Specify create-follow-location tests for one supported item route and one proposed missing route. Include the required principal and expected result, and explain why a syntactically valid Location header is insufficient evidence.

**C15.2: Response evolution.** Propose explicit purchase-request and vendor response records. Identify required fields, fields to omit, and compatibility tests. Explain how direct domain serialization couples future domain additions to the public contract.

**C15.3: Error action.** Map invalid quantity, stale version, missing part, forbidden campus, and interrupted reservation to safe client next steps. Separate current status mappings from a proposed richer taxonomy and avoid blanket retries for every failure.

## Review checkpoint

When reviewing compatibility, distinguish adding an optional response field from changing the meaning of an existing one. A client may tolerate extra JSON properties yet still break if Available begins to mean OnHand or Version stops advancing as documented. Contract tests should check representative meanings and transitions, not only that deserialization succeeds. Schema shape and business semantics require complementary evidence.

Your contract inventory should let a client author understand the next valid action without reading domain source. It should identify where the current API is intentionally simple, where routes or projections need extension, and where a transport failure cannot reveal the business outcome. Those distinctions support the authorization review next.
