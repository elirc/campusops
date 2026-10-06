# Over-engineering review

CampusOps is deliberately larger than BorrowBox, but it remains one backend deployment. Module folders organize change; they are not pretend microservices.

## Complexity that earns its place

- Public contracts (`src/CampusOps.Contracts`) are separate because both the React console and external integrations consume the API. Note the React client keeps its own TypeScript copies in `web/CampusOps.Web/src/api/types.ts`; the C# records do not generate them, so the two can drift.
- Store interfaces let the in-memory teaching defaults, the SQLite adapters (`Infrastructure/Data/SqliteStores.cs`, locations and assets only, not registered in the API) and the atomic parts store sit behind the same Core contracts.
- `IClock`, idempotency, optimistic versions, and atomic reservation each answer a reproduced failure mode.
- A separate worker is justified by scheduled work that must not depend on an HTTP request. Today it does one job: `PreventiveMaintenanceWorker` generates due maintenance requests once a minute. Webhook deliveries are modelled (`WebhookDelivery` with attempts and dead-lettering in `Core/Integrations/Webhooks.cs`) and enqueued, but nothing sends them yet; a delivery loop would be the worker's second job.
- Append-only audit events explain operational changes without event sourcing the whole system.

## Deliberately avoided

No microservices, Kubernetes, service mesh, CQRS framework, MediatR, AutoMapper, generic repository, distributed lock, custom rules engine, or message broker. No abstraction exists solely because another project might someday need it.

## Process overhead

Fifteen PRs (#61 to #75 in the git history, one per sprint) plus their planning milestones and issues are heavier than this product needs. They exist to rehearse scoping, CI, review, and release discipline. A delivery team could combine several slices.

The Contracts project and row/domain mapping add cost. They are justified here by real consumers and business-rich objects. For an internal one-screen CRUD tool, direct EF entities and fewer projects could be better.

## Evidence that would justify more architecture

- Add a durable outbox transaction when webhook delivery must survive process failure.
- Add a broker when measured delivery volume or independent consumers require asynchronous decoupling.
- Split a module only for independent ownership, release cadence, scale, and a clear data boundary.
- Add caching only after measurements identify a bottleneck and invalidation rules are understood.

Prefer readable business code and another focused regression over another framework.

*Checked against the code on 2026-10-06: the worker, contracts and store descriptions above were corrected to match what is registered in `src/CampusOps.Api/Program.cs` and `src/CampusOps.Worker/Program.cs`.*
