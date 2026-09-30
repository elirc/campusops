# Worked change: preserve stock and its explanation together

The problem was visible across several small methods. `Part.Reserve` guarded available quantity, and `TryReserveAsync` locked a part. Those local protections made the feature look safe. But `PartsService` then called a separate reservation store. The feature's true promise was larger than either method: every successful reservation should have both a stock effect and a matching history record.

## First establish counterexamples

Before changing production code, five new tests ran against the old implementation and failed. [StockCapacityTests](../tests/CampusOps.Core.Tests/Inventory/StockCapacityTests.cs) showed that receiving above integer capacity did not reject the operation. [PartStoreIsolationTests](../tests/CampusOps.Infrastructure.Tests/Inventory/PartStoreIsolationTests.cs) showed four distinct ownership failures: mutating an inserted object changed stored stock; mutating a fetched or listed object changed it; adding an existing ID silently replaced stock; and a cancelled add still wrote data.

These are useful regressions because they assert observable promises rather than private implementation details. A future SQL adapter might implement copying differently, yet callers should still be unable to change persisted stock merely by modifying a returned object.

## Move the transaction into the adapter contract

The old two-call pattern was conceptually:

```csharp
var part = await parts.TryReserveAsync(partId, quantity, token);
await reservations.AddAsync(receipt, token);
```

The new [store interface](../src/CampusOps.Core/Inventory/Part.cs) exposes `ReserveForWorkOrderAsync`, whose contract includes both effects. The separate history interface is read-only. There is no public append method that allows normal callers to invent a reservation record independently of stock. The service verifies inputs and work-order existence, obtains time from `IClock`, and delegates one commit operation.

Inside [the memory adapter](../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs), a private gate protects all inventory state. The method checks cancellation, identifiers, quantity, part existence, and available stock. It then prepares a detached updated part and a reservation receipt. Only after those operations succeed does it publish the reservation and replacement part while still holding the gate. The lock also protects reads, so ordinary concurrent callers see a completed state.

This design deliberately uses one coarse lock for a small teaching store. It trades maximum throughput for an understandable consistency boundary. Do not replace it with many locks until you can explain lock ordering, history reads, and cross-part operations. Catastrophic process failure or allocation exhaustion is outside the memory adapter's durability promise.

## Wire the same object to both interfaces

[Program.cs](../src/CampusOps.Api/Program.cs) registers the concrete `InMemoryPartStore` once and resolves both `IPartStore` and `IPartReservationStore` from that singleton. This matters because two separately constructed stores would hold separate dictionaries. A class implementing two interfaces does not force the dependency-injection container to share instances; the registrations must express that lifetime.

The [API lifecycle test](../tests/CampusOps.Api.Tests/Inventory/InventoryApiTests.cs) creates a part, follows its Location, reserves stock for a seeded work order, reads history through HTTP, and verifies the remaining quantity. That crosses routing, service construction, interface registration, and state storage. It catches a composition mistake that a pure domain test cannot.

## Repair the surrounding feature contract

Part creation already returned a Location URL, but no matching GET-by-ID route existed. The new route makes the response navigable and returns 404 for an unknown part. Reservation history is now readable through the work-order parts route. The inventory routes require the same Operations policy as the work-order workflow. This intentionally changes previously unprotected inventory access to require demo identity; it is documented rather than hidden as an incidental refactor.

The local web proxy now targets port 5110, matching the checked-in API launch profile. This fixes a setup mismatch, not an inventory UI: the browser still displays work orders. No package versions or lockfiles were changed.

Review the result by asking what could still fail. A response can be lost after commit, memory disappears on restart, multiple API processes do not share state, and a completed work order is not currently rejected by this feature. Each remaining boundary has a concrete exercise in [the practice plan](05-PRACTICE.md).
