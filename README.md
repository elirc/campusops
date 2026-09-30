# CampusOps

CampusOps is a fifteen-sprint teaching repository for learning business-heavy C#/.NET API development and React/TypeScript integration through a university facilities platform.

Start the detailed [inventory ownership and CRUD upskilling course](astraupskill/README.md) for source walkthroughs, worked repairs, tests, exercises, and review guidance.

## Architecture

```text
CampusOps.Api             HTTP composition and security
CampusOps.Core            business modules and use cases
CampusOps.Infrastructure  in-memory, SQLite, and integration adapters
CampusOps.Contracts       stable public request/response contracts
CampusOps.Worker          scheduled maintenance and delivery work
CampusOps.Web             React and TypeScript operations console
```

The backend is a modular monolith. Module boundaries organize code without adding network calls, distributed transactions, or separate deployments.

```powershell
dotnet restore CampusOps.slnx --locked-mode
dotnet run --project src/CampusOps.Api
npm ci --prefix web/CampusOps.Web
npm run build --prefix web/CampusOps.Web
```

## Course map

| Sprints | Theme | Outcome |
| --- | --- | --- |
| 1–5 | Operational foundations | locations, assets, requests, work orders, technicians |
| 6–10 | Reliability and time | audit history, SQLite, stock, worker scheduling, SLAs |
| 11–13 | Integration surfaces | procurement, authorization, React dashboard |
| 14–15 | Engineering ownership | idempotency, concurrency, reporting, operations, release |

Development OpenAPI is at `/openapi/v1.json`. Protected operations use teaching-only `X-Demo-Role` and `X-Demo-Campus` headers; coverage varies by module. Inventory now requires the Operations policy. These caller-controlled headers are not production identity. The API default is non-durable; `/health/ready` reports that fact.

Read [LEARNER-GUIDE.md](LEARNER-GUIDE.md), the [curriculum specification](docs/curriculum/SPEC.md), [TESTREADME.md](TESTREADME.md), and [OEReadMe.md](OEReadMe.md).

The API launch profile and Vite development proxy both use port 5110. Inventory reservations commit stock and history together in one shared memory store; this is not durable or shared across processes. The React console reads work orders; the inventory editor is a documented course exercise.
