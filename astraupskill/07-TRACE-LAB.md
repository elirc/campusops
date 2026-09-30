# Trace a reservation from request to stored evidence

This lab has two modes. The automated mode runs the delivered API test through an ASP.NET test host. The manual mode starts a disposable local API and lets you observe the HTTP sequence yourself. The verification record states which mode was executed; do not treat these suggested manual commands as recorded browser evidence.

## Automated trace

From the root:

```powershell
dotnet test tests/CampusOps.Api.Tests --filter FullyQualifiedName~CreateReadReserveAndHistoryShareOneStore
```

Open [InventoryApiTests](../tests/CampusOps.Api.Tests/Inventory/InventoryApiTests.cs) while it runs. The test creates a part through HTTP, follows its Location, seeds a valid work order through the test host's store, reserves three units, reads its history, and checks that two units remain. It then rejects another reservation of three and verifies that history still contains one entry. This is an integration trace of the inventory API, not a test of the request-to-work-order conversion UI.

At a breakpoint in `PartsService.ReserveAsync`, inspect the requested identifiers and quantity. At the store's commit boundary, inspect the current snapshot and prepared replacement. After the HTTP response, read stock and history through separate routes. Explain why both routes must share one singleton adapter.

## Manual local API trace

Use a disposable instance with no valuable state. Start it with `dotnet run --project src/CampusOps.Api`; the checked-in profile listens on 5110. The following example uses PowerShell objects and JSON serialization so that GUID values are represented correctly:

```powershell
$api = 'http://localhost:5110'
$headers = @{ 'X-Demo-Role' = 'Dispatcher'; 'X-Demo-Campus' = 'North' }
$partBody = @{ sku = 'LAB-FILTER'; name = 'Lab filter'; onHand = 5 } | ConvertTo-Json
$part = Invoke-RestMethod -Method Post -Uri "$api/api/v1/parts" -Headers $headers -ContentType 'application/json' -Body $partBody
$requestBody = @{
  locationId = [guid]::NewGuid().ToString()
  assetId = $null
  summary = 'Replace the lab filter'
  description = 'Disposable request for the inventory learning trace'
} | ConvertTo-Json
$request = Invoke-RestMethod -Method Post -Uri "$api/api/v1/maintenance-requests" -ContentType 'application/json' -Body $requestBody
$triageBody = @{ priority = 'Normal' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$api/api/v1/maintenance-requests/$($request.id)/triage" -ContentType 'application/json' -Body $triageBody
$order = Invoke-RestMethod -Method Post -Uri "$api/api/v1/work-orders/from-request/$($request.id)" -Headers $headers
$reserveBody = @{ partId = $part.id; quantity = 3 } | ConvertTo-Json
$receipt = Invoke-RestMethod -Method Post -Uri "$api/api/v1/work-orders/$($order.id)/parts" -Headers $headers -ContentType 'application/json' -Body $reserveBody
Invoke-RestMethod -Uri "$api/api/v1/parts/$($part.id)" -Headers $headers
Invoke-RestMethod -Uri "$api/api/v1/work-orders/$($order.id)/parts" -Headers $headers
```

The teaching request service currently requires a nonempty location ID but does not look it up in the location store. This lab uses that existing behavior; it is not proof of location referential integrity. A production workflow needs a real location and ownership checks.

Predict `OnHand=5`, `Reserved=3`, and `Available=2`. The history should contain one receipt with quantity three and the same work-order/part IDs. Repeat the reserve call with quantity three and inspect the error response: insufficient stock should leave both surfaces unchanged. PowerShell throws for a non-success HTTP response, so capture the error deliberately if you automate that step. Do not interpret a thrown client exception as proof that state remained unchanged; perform the reads.

## Questions that make the trace useful

What happens if you repeat the successful request with quantity one? Under current behavior, it creates another reservation; there is no idempotency key. What happens after restarting the API? Memory state is lost. What happens if you remove the demo headers from the inventory request? The policy challenges the request. Why does supplying those headers not establish a real user's identity? The caller chose their values.

Finally, open the web console and locate the work-order list request in developer tools as an optional manual follow-up. It can display the newly converted order while the same API process is running, but it does not yet render inventory quantities. Use that observation to define the UI capstone's first slice rather than assuming the backend feature is already a complete browser workflow.
