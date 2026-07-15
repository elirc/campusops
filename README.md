# CampusOps

CampusOps is a fifteen-sprint teaching repository for learning business-heavy C#/.NET API development and React/TypeScript integration through a university facilities platform.

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

Read [LEARNER-GUIDE.md](LEARNER-GUIDE.md) and the [curriculum specification](docs/curriculum/SPEC.md).
