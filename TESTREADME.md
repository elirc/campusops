# Testing guide: why these tests exist

CampusOps tests protect decisions that matter to technicians, dispatchers, managers, integrators, and operators. They were added so a junior engineer can change a larger codebase without relying on memory or fear.

## Layers

| Project | Proves | Examples |
| --- | --- | --- |
| `CampusOps.Core.Tests` | business rules in isolation | workflow transitions, SLA arithmetic, approval thresholds |
| `CampusOps.Infrastructure.Tests` | adapter and multi-object behavior | SQLite mapping, append-only history, atomic stock, webhook idempotency |
| `CampusOps.Api.Tests` | HTTP composition and security | contracts, 401 vs 403, correlation/readiness |
| `CampusOps.Web` Vitest | TypeScript presentation logic | combined work-order filters |

Use the cheapest boundary that proves the promise. A plain object test is better than an HTTP test for a state transition; a real SQLite test is better than a mock for database behavior.

## Good habits

- Arrange only meaningful state, act through a public behavior, and assert observable outcomes.
- Test just below, exactly at, and just above important limits.
- Inject time and synchronize concurrency tests; never use arbitrary sleeps.
- Commit a bug counterexample before its fix when practical.
- Keep one reason to fail per test, while allowing several assertions that describe one result.
- Treat flakes as product defects, not invitations to add retries.
- Read a failure message before opening implementation code.

Coverage is a clue, not a target. Avoid tests coupled to private methods, mocks of every class, and assertions that merely repeat assignments.

Run `dotnet test CampusOps.slnx` and `npm test --prefix web/CampusOps.Web`.
