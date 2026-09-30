# Follow one feature across the codebase

Begin with [the composition root](../src/CampusOps.Api/Program.cs). This is where concrete adapters are assigned to interfaces and routes enter the application. A class named `SqliteLocationStore` existing somewhere in the solution does not prove the running API uses SQLite. Registration is the evidence: the teaching API selects memory stores. The health endpoint also declares that the default is non-durable.

## Files to open in order

| File | Responsibility | Reading question |
| --- | --- | --- |
| [Inventory contracts](../src/CampusOps.Contracts/Inventory/PartContracts.cs) | Request and response shapes | Which values can a caller supply, and which are server-generated? |
| [Part endpoints](../src/CampusOps.Api/Inventory/PartEndpoints.cs) | HTTP routing, policy, response mapping | What happens for a missing part or insufficient stock? |
| [Part and PartsService](../src/CampusOps.Core/Inventory/Part.cs) | Stock rules and reservation use case | Which checks must happen before a write? |
| [Memory inventory adapter](../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs) | State ownership and one commit boundary | Can a caller mutate internal state after a read? |
| [Work orders](../src/CampusOps.Core/WorkOrders/WorkOrder.cs) | Work-order identity and lifecycle | What does checking existence prove, and what does it leave open? |
| [Demo authentication](../src/CampusOps.Api/Security/DemoAuthentication.cs) | Header-to-principal teaching adapter | Why can this demonstrate policy behavior without proving real identity? |
| [Domain exception mapping](../src/CampusOps.Api/Errors/DomainExceptionHandler.cs) | Business errors to Problem Details | Why is the current business rejection HTTP 400? |

A reserve request follows this path:

```text
POST /api/v1/work-orders/{id}/parts
  -> Operations authorization policy
  -> ReservePartRequest: partId, quantity
  -> PartsService: identifiers, positive quantity, existing work order
  -> IClock: current time supplied to the operation
  -> IPartStore.ReserveForWorkOrderAsync
  -> one inventory lock: prepare new stock + reservation, commit both
  -> PartReservation response
```

A subsequent GET of the part returns current quantities. A GET of the work order's parts returns its reservation records. Both interfaces resolve to the same `InMemoryPartStore` singleton. Registering two independent instances would make a successful write invisible to the history reader even though both classes compile and each method appears correct. The API lifecycle test checks this composition promise.

## Understand the surrounding modules

[Maintenance requests](../src/CampusOps.Core/Requests/MaintenanceRequest.cs) begin as submitted, become triaged, and can then be converted into work orders. [Work-order endpoints](../src/CampusOps.Api/WorkOrders/WorkOrderEndpoints.cs) expose conversion and actions such as starting or completing work. That workflow explains why a reservation needs an existing work order. The new existence check does not enforce every lifecycle rule; deciding whether completed orders may receive parts is an exercise with an explicit acceptance contract.

[Operations reporting](../src/CampusOps.Core/Reporting/OperationsReportService.cs) reads parts along with assets and work orders. Detached part reads therefore protect more than one HTTP endpoint. Reporting should observe data without becoming an accidental write path. The current all-parts snapshot is internally consistent for inventory, but a report spanning several modules is not a database-wide transactional snapshot.

[CampusOpsDbContext](../src/CampusOps.Infrastructure/Data/CampusOpsDbContext.cs) models locations and assets with keys, a unique index, and a foreign key. Read it as a persistence lesson and a starting point for a future inventory adapter. The [worker composition root](../src/CampusOps.Worker/Program.cs) selects its own memory adapters; running a second process does not magically share the API's dictionaries.

## Locate the browser boundary

The [web API client](../web/CampusOps.Web/src/api/client.ts) fetches work orders with demo headers. The [hook](../web/CampusOps.Web/src/hooks/useWorkOrders.ts) handles loading, errors, and cancellation. [App.tsx](../web/CampusOps.Web/src/App.tsx) renders the table and filters. Trace those files before claiming that backend inventory changes appear in the UI. They currently do not: an inventory screen and its mutation/error states are learner work.

Draw two diagrams of your own: one for reservation state inside a single API process, and another for API plus worker processes. Label every dictionary owner. If you cannot explain which process owns a piece of state, you are not yet ready to claim persistence or concurrency guarantees for it.
