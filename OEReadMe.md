# Over-engineering review

CampusOps is deliberately larger than BorrowBox, but it remains one backend deployment. Module folders organize change; they are not pretend microservices.

## Complexity that earns its place

- Public contracts are separate because both React and external integrations consume the API.
- Store boundaries support teaching defaults, SQLite, atomic operations, and future integration adapters.
- `IClock`, idempotency, optimistic versions, and atomic reservation each answer a reproduced failure mode.
- A separate worker is justified by scheduled and delivery work that must not depend on an HTTP request.
- Append-only audit events explain operational changes without event sourcing the whole system.

## Deliberately avoided

No microservices, Kubernetes, service mesh, CQRS framework, MediatR, AutoMapper, generic repository, distributed lock, custom rules engine, or message broker. No abstraction exists solely because another project might someday need it.

## Process overhead

Fifteen PRs, fifteen milestones, and sixty issues are heavier than this product needs. They exist to rehearse scoping, CI, review, and release discipline. A delivery team could combine several slices.

The Contracts project and row/domain mapping add cost. They are justified here by real consumers and business-rich objects. For an internal one-screen CRUD tool, direct EF entities and fewer projects could be better.

## Evidence that would justify more architecture

- Add a durable outbox transaction when webhook delivery must survive process failure.
- Add a broker when measured delivery volume or independent consumers require asynchronous decoupling.
- Split a module only for independent ownership, release cadence, scale, and a clear data boundary.
- Add caching only after measurements identify a bottleneck and invalidation rules are understood.

Prefer readable business code and another focused regression over another framework.
