# Learner guide

CampusOps is a university facilities API (ASP.NET Core minimal APIs on .NET 10) with a small React/TypeScript console. A maintenance request is submitted, triaged and converted into a work order; technicians are assigned and move it through a workflow; parts are reserved against it. This guide is the map: what lives where, how to run and test it, and the order to read it in. The deep, exercise-based material is in [astraupskill/](astraupskill/README.md).

## The six projects

| Project | Role | Start reading at |
| --- | --- | --- |
| `src/CampusOps.Api` | composition root, HTTP endpoints, demo authentication, error mapping | `Program.cs` (90 lines: every registration and every `Map...Endpoints` call) |
| `src/CampusOps.Core` | domain objects, invariants and services, one folder per module | `WorkOrders/WorkOrder.cs` |
| `src/CampusOps.Infrastructure` | in-memory stores (the default) and SQLite stores (not wired into the API) | `Inventory/InMemoryPartsStores.cs` |
| `src/CampusOps.Contracts` | request/response records shared by the endpoints | `WorkOrders/WorkOrderContracts.cs` |
| `src/CampusOps.Worker` | separate process that generates preventive-maintenance requests every minute | `PreventiveMaintenanceWorker.cs` |
| `web/CampusOps.Web` | Vite + React console that lists and filters work orders | `src/domain/filterWorkOrders.ts` |

Dependency direction is Api → Core/Contracts/Infrastructure, Infrastructure → Core. Core references nothing else in the solution, which is what lets its tests run without a web host.

## Modules at a glance

Each row is one vertical slice. Paths are relative to `src/` and `tests/`.

| Module | Domain + service (Core) | Endpoints (Api) | Default store (Infrastructure) | Tests |
| --- | --- | --- | --- | --- |
| Locations | `Locations/Location.cs` | `Locations/LocationEndpoints.cs` | `Locations/InMemoryLocationStore.cs` | `Core.Tests/Locations`, `Api.Tests/Locations` |
| Assets | `Assets/Asset.cs` | `Assets/AssetEndpoints.cs` | `Assets/InMemoryAssetStore.cs` | `Core.Tests/Assets` |
| Requests | `Requests/MaintenanceRequest.cs` | `Requests/MaintenanceRequestEndpoints.cs` | `Requests/InMemoryMaintenanceRequestStore.cs` | `Core.Tests/Requests` |
| Work orders | `WorkOrders/WorkOrder.cs`, `WorkOrderHistory.cs` | `WorkOrders/WorkOrderEndpoints.cs` | `WorkOrders/InMemoryWorkOrderStore.cs`, `InMemoryWorkOrderHistoryStore.cs` | `Core.Tests/WorkOrders`, `Infrastructure.Tests/WorkOrders` |
| Technicians | `Technicians/Technician.cs` (includes `AssignmentService`) | `Technicians/TechnicianEndpoints.cs` | `Technicians/InMemoryTechnicianStore.cs` | `Core.Tests/Technicians` |
| Inventory | `Inventory/Part.cs` (includes `PartsService`) | `Inventory/PartEndpoints.cs` | `Inventory/InMemoryPartsStores.cs` | `Core.Tests/Inventory`, `Infrastructure.Tests/Inventory`, `Api.Tests/Inventory` |
| Preventive maintenance | `PreventiveMaintenance/MaintenancePlan.cs` | none (Worker only) | `PreventiveMaintenance/InMemoryMaintenancePlanStore.cs` | `Infrastructure.Tests/PreventiveMaintenance` |
| Service levels | `ServiceLevels/ServiceLevel.cs` | `ServiceLevels/ServiceLevelEndpoints.cs` | `ServiceLevels/InMemoryServiceLevelStore.cs` | `Core.Tests/ServiceLevels` |
| Procurement | `Procurement/Procurement.cs` | `Procurement/ProcurementEndpoints.cs` | `Procurement/InMemoryProcurementStores.cs` | `Core.Tests/Procurement`, `Api.Tests/Security` |
| Integrations (webhooks) | `Integrations/Webhooks.cs` | `Integrations/WebhookEndpoints.cs` | `Integrations/InMemoryWebhookStore.cs` | `Infrastructure.Tests/Integrations` |
| Reporting | `Reporting/OperationsReportService.cs` | `Reporting/ReportingEndpoints.cs` | reads the other stores | `Infrastructure.Tests/Reporting` |

## One request, end to end

Follow this once with the API running (`dotnet run --project src/CampusOps.Api`, port 5110 per `Properties/launchSettings.json`). OpenAPI is served at `/openapi/v1.json` in Development.

1. **Create a location**: `POST /api/v1/locations` with `{ "campus": "North", "building": "Science", "room": "101" }`. Requires headers `X-Demo-Role: Manager` and `X-Demo-Campus: North`; the endpoint also checks that the body's campus matches the header (`CampusAuthorizer.EnsureAccess` in `Security/DemoAuthentication.cs`).
2. **Submit a request**: `POST /api/v1/maintenance-requests` with the location id, a summary and a description. No headers needed. Note that `MaintenanceRequestService` only depends on its own store, so it does not check that the location exists.
3. **Triage it**: `POST /api/v1/maintenance-requests/{id}/triage` with `{ "priority": "High" }`. A second triage is rejected (`request.not_submitted`).
4. **Convert it**: `POST /api/v1/work-orders/from-request/{requestId}` (role `Dispatcher` or `Manager`). `MarkConverted` refuses an untriaged request (`request.not_triaged`).
5. **Assign a technician**: create one with `POST /api/v1/technicians` and `{ "name": "Sam", "skills": ["HVAC"] }`, then `POST /api/v1/work-orders/{id}/assignment` with `{ "technicianId": "...", "requiredSkill": "hvac" }`. `AssignmentService` rejects unknown or inactive technicians and missing skills; skill matching ignores case and surrounding spaces (`SkillMatching_IsCaseInsensitive`).
6. **Move it through the workflow**: `POST /api/v1/work-orders/{id}/start`, then `.../complete`, each with `{ "expectedVersion": N }` where `N` is the `version` from the previous response. A stale version returns `work_order.version_conflict`; an illegal move returns `work_order.transition_invalid` (see `WorkOrder.cs`).
7. **Comment and read the timeline**: `POST /api/v1/work-orders/{id}/comments`, then `GET .../timeline`.

Domain errors are `DomainException`s with a stable code; `Errors/DomainExceptionHandler.cs` turns them into problem-details responses.

Two things you will notice on this trace, and should be able to explain:

- **Authorization is per route, and uneven.** `Operations` (Dispatcher or Manager) protects the work-order group and inventory; `Manager` protects location creation, procurement, webhooks and reporting. Assets, maintenance requests, technicians, the assignment route and the service-level routes have no `RequireAuthorization` at all; the assignment route is mapped in `TechnicianEndpoints.cs` outside the protected work-order group. Chapter 16 of the extended course is built on this.
- **The timeline only contains comments.** `WorkOrderHistoryService.CommentAsync` is the only code that creates an `AuditEvent`; `WorkOrderService.ActAsync` changes status without recording one.

The checked-in `src/CampusOps.Api/CampusOps.Api.http` still contains the template's `GET /weatherforecast/` request, which does not exist in this API. Write your own requests from the steps above.

## Run and test

The SDK is pinned by `global.json` (10.0.302, roll forward to latest patch). CI (`.github/workflows/ci.yml`) runs exactly this, so matching it locally means no surprises in a PR:

```powershell
dotnet restore CampusOps.slnx --locked-mode
dotnet format CampusOps.slnx --verify-no-changes --no-restore
dotnet build CampusOps.slnx --configuration Release --no-restore
dotnet test CampusOps.slnx --configuration Release --no-build

npm ci --prefix web/CampusOps.Web
npm run build --prefix web/CampusOps.Web
npm test --prefix web/CampusOps.Web
```

Narrower runs while you work:

```powershell
dotnet test tests/CampusOps.Core.Tests                                   # domain rules only, fastest
dotnet test CampusOps.slnx --filter "FullyQualifiedName~Inventory"       # one module across all three test projects
dotnet test CampusOps.slnx --filter "FullyQualifiedName~PartStoreIsolationTests"  # one class
```

On Windows PowerShell, if `npm` is blocked by script policy, use `npm.cmd`.

Test inventory (static count, 2026-10-06): 39 test methods, 42 executed cases once the three `[Theory]` rows are expanded: Core 18, Infrastructure 16, Api 8. The web project has one Vitest test. [TESTREADME.md](TESTREADME.md) explains what each layer is for.

## Reading order

The repository was built in fifteen sprints, one PR each (#61 to #75). Read a sprint's short brief, then its feature commit, then its tests. `git show --stat <sha>` shows exactly which files a sprint touched.

| Sprint | Brief | Feature commit | Focus while reading |
| --- | --- | --- | --- |
| 1 | [locations and API foundation](docs/sprints/sprint-01.md) | `6c51b4f` | `Program.cs`, a minimal vertical slice, problem details |
| 2 | [asset lifecycle](docs/sprints/sprint-02.md) | `1972134` | status transitions as methods, not setters |
| 3 | [intake and triage](docs/sprints/sprint-03.md) | `4237c65` | state guards with error codes |
| 4 | [work-order workflow](docs/sprints/sprint-04.md) | `1bad186` | the state machine and `Version` |
| 5 | [technician assignment](docs/sprints/sprint-05.md) | `67158bb`, fix `3ed8379` | why assignment moved into a policy service |
| 6 | [comments and audit history](docs/sprints/sprint-06.md) | `fced2ba` | append-only history |
| 7 | [SQLite persistence](docs/sprints/sprint-07.md) | `3532464` | `Data/SqliteStores.cs`, row ↔ domain mapping |
| 8 | [parts and stock](docs/sprints/sprint-08.md) | `93335bc` | on-hand / reserved / available |
| 9 | [preventive maintenance](docs/sprints/sprint-09.md) | `8a9d2b2` | the worker, injected `IClock` |
| 10 | [SLA clocks](docs/sprints/sprint-10.md) | `a952c38` | pause accounting |
| 11 | [procurement](docs/sprints/sprint-11.md) | `b0c66d1` | approval threshold (1000, set in `Program.cs`) |
| 12 | [roles and campus permissions](docs/sprints/sprint-12.md) | `a5ca371` | demo authentication and its limits |
| 13 | [React dashboard](docs/sprints/sprint-13.md) | `496d214` | `web/CampusOps.Web` |
| 14 | [webhooks and bug harvest](docs/sprints/sprint-14.md) | `d41f1f1` | idempotency, concurrency tests |
| 15 | [reporting and operations](docs/sprints/sprint-15.md) | `38b8d88` | health/readiness, correlation id |

The final commit (`7d50d34`) added the inventory-reservation repair and the course material in `astraupskill/`.

After the sprints, read in this order:

1. [astraupskill/README.md](astraupskill/README.md): the main course, built around inventory reservation (commit boundaries, detached snapshots, access policy).
2. [astraupskill/advanced/README.md](astraupskill/advanced/README.md): the advanced pack and its disposable failure lab.
3. [astraupskill/advanced/extended-course/README.md](astraupskill/advanced/extended-course/README.md): 22 chapters that apply the same questions to every other module.
4. The supporting docs: [ADR-001](docs/architecture/ADR-001-modular-monolith.md), [threat model](docs/security/THREAT-MODEL.md), [runbook](docs/operations/RUNBOOK.md), [bug harvest](docs/quality/BUG-HARVEST.md), and [OEReadMe.md](OEReadMe.md) on what was deliberately not built.

## For every sprint

1. Read the brief and write down the one rule it adds.
2. Find where that rule is enforced (domain method, service, store or endpoint) and why there.
3. Predict two failure cases, then find the tests that cover them, or notice that none do.
4. Run that test project, then the whole solution.
5. Write one sentence on what the sprint deliberately did not abstract.

## Known limits to keep in mind

- **Everything is in memory by default.** `/health/ready` says so (`durable = false`). The SQLite stores in `Infrastructure/Data/SqliteStores.cs` cover locations and assets, are exercised by `SqliteStoreTests`, and are not registered in `Program.cs`.
- **The worker has its own stores.** It registers its own in-memory plan and request stores (`src/CampusOps.Worker/Program.cs`), so requests it generates are invisible to the API process.
- **Stores differ in what they hand back.** The parts store returns copies (`Part.Snapshot()`); the work-order store returns the stored object itself, which is why `ActAsync` can change status without a save call. Both are deliberate teaching points; know which one you are in before you change a store.
- **Demo headers are not identity.** Any caller can send `X-Demo-Role: Manager`.
