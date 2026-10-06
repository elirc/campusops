# Testing guide: why these tests exist

CampusOps tests protect decisions that matter to technicians, dispatchers, managers, integrators and operators. They exist so a junior engineer can change a larger codebase without relying on memory or fear.

## Layers

| Project | Proves | Cases | Examples (file → test) |
| --- | --- | --- | --- |
| `tests/CampusOps.Core.Tests` | business rules on plain objects, no host | 18 | `WorkOrders/WorkOrderTests.cs` → `OpenWork_CannotComplete`; `ServiceLevels/ServiceLevelTests.cs` → `PausedTime_ExtendsDueDateAndPreventsEscalation`; `Procurement/PurchaseRequestTests.cs` → `Submit_UsesApprovalThreshold` (999 approved, 1001 pending, threshold 1000) |
| `tests/CampusOps.Infrastructure.Tests` | store behaviour and multi-object promises | 16 | `Inventory/ConcurrentReservationTests.cs` → `CompetingReservations_CommitMatchingStockAndHistory`; `Inventory/PartStoreIsolationTests.cs` → `ReadsAreDetachedFromStoredState`; `Integrations/WebhookTests.cs` → `DuplicateEvent_IsEnqueuedOnce`; `Data/SqliteStoreTests.cs` → `Stores_PersistLocationAndAssetAcrossContexts` |
| `tests/CampusOps.Api.Tests` | HTTP composition, contracts and security | 8 | `Inventory/InventoryApiTests.cs` → `InventoryRequiresOperationsRole` (401 without headers, 403 for Technician); `Operations/HealthTests.cs` → `Readiness_IsHonestAndEchoesCorrelationId` |
| `web/CampusOps.Web` (Vitest) | TypeScript presentation logic | 1 | `src/domain/filterWorkOrders.test.ts` → combined query + status filter |

Counts are executed cases (each `[InlineData]` row of a `[Theory]` counts once), from a static count of the source on 2026-10-06: 39 test methods, 42 .NET cases. They match the 42 recorded in [astraupskill/VERIFICATION.md](astraupskill/VERIFICATION.md). Re-count with `dotnet test` if the suite has changed.

Use the cheapest boundary that proves the promise. A plain object test is better than an HTTP test for a state transition; a real SQLite test is better than a mock for database behaviour. Note the limits of the SQLite test: it uses one shared `:memory:` connection and covers only the location and asset stores, so it proves mapping and cross-context reads, not durability across a restart, and not inventory.

## Run them

```powershell
dotnet test CampusOps.slnx                                              # everything
dotnet test tests/CampusOps.Core.Tests                                  # one layer
dotnet test CampusOps.slnx --filter "FullyQualifiedName~WorkOrders"     # one module, all layers
dotnet test CampusOps.slnx --filter "FullyQualifiedName~ConcurrentReservationTests"  # one class
npm test --prefix web/CampusOps.Web                                     # Vitest (npm.cmd on locked-down Windows PowerShell)
```

CI runs `dotnet format --verify-no-changes` before building, so run it locally too or a formatting-only difference will fail your PR.

## Good habits, with examples from this suite

- **Arrange only meaningful state, act through a public behaviour, assert observable outcomes.** `CancelledReservationChangesNeitherStockNorHistory` asserts both stored stock and history, not just that an exception was thrown.
- **Test just below, at and above important limits.** `Submit_UsesApprovalThreshold` uses 999 and 1001 around a threshold of 1000. Exactly 1000 is not tested; reading `Procurement.cs` to find out which side it falls on is a good first exercise.
- **Inject time; synchronize concurrency tests; never sleep.** `PreventiveMaintenanceTests` passes a `FakeClock` record implementing `IClock`. `ConcurrentReservationTests` releases its competing tasks from one `TaskCompletionSource` instead of hoping they overlap. No test in the repository calls `Thread.Sleep` or `Task.Delay`.
- **Commit a bug counterexample before its fix when practical.** The five inventory regressions described in [astraupskill/03-WORKED-CHANGE.md](astraupskill/03-WORKED-CHANGE.md) were run red against the old store first.
- **One reason to fail per test**, while allowing several assertions that describe one result.
- **Treat flakes as product defects**, not invitations to add retries.
- **Read the failure message before opening implementation code.** Domain errors carry stable codes (`work_order.transition_invalid`, `part.quantity_invalid`); tests assert the code, so the message tells you which rule fired.

Coverage is a clue, not a target. Avoid tests coupled to private methods, mocks of every class, and assertions that merely repeat assignments.

## Gaps worth knowing about

These are observations from reading the suite, useful as starting points for new tests:

- Of the routes without `RequireAuthorization` (assets, maintenance requests, technicians, assignment, service levels), none has a test that pins whether it should be protected.
- No test covers the work-order timeline after a status change; the timeline currently only records comments.
- Several Core tests use `DateTimeOffset.UtcNow` directly (for example the service-level tests). They compare against a start time they captured, so they are stable, but they are not using the `IClock` seam the services provide.
