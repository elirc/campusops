# Verification record

The CampusOps improvements were verified on September 8, 2026 in an isolated copy of the original project. Backend verification used .NET SDK 10.0.302 and a locked restore. Web verification used Node 22.16.0, npm 10.9.2, and `npm.cmd ci` against the existing lockfile. No package versions or application lockfiles were intentionally changed. Original project delivery excludes restored packages, node_modules, build output, temporary reports, and test databases.

## Recorded results

The unchanged backend baseline passed 28 tests: 17 core, 7 infrastructure, and 4 API. Five new regression tests then failed against the original implementation, establishing the overflow, object-ownership, duplicate-ID, and cancellation defects described in the worked-change chapter. Their failures were retained separately from the final passing reports.

After the implementation, the full backend suite passed 42 tests: 18 core, 16 infrastructure, and 8 API. No test failed or was skipped. The infrastructure suite includes the existing SQLite location/asset adapter test using a shared open in-memory connection. That is real SQLite execution, but it is not evidence that inventory is persisted or that data survives a process restart.

```powershell
dotnet test CampusOps.slnx --no-restore --configuration Release --logger trx
npm.cmd test --prefix web/CampusOps.Web -- --maxWorkers=2
npm.cmd run build --prefix web/CampusOps.Web
```

The existing web filtering test passed, and the TypeScript/Vite production build completed successfully. The build produced the application bundle without changing source dependencies. The production bundle check does not run a live browser or prove the development proxy's network route; the proxy correction was also checked against the API's actual launch profile, which specifies port 5110.

## Scope of the evidence

The new API tests exercise create/read/reserve/history behavior through an ASP.NET test host, including following the returned Location, retaining history after insufficient stock, rejecting missing work orders, and distinguishing unauthenticated from forbidden inventory requests. The concurrency test releases 64 tasks against five available units and verifies that successful receipts, history quantity, and stored stock agree. Cancellation tests check unchanged state as well as the exception.

The automated trace in the course runs a test included in that final suite. The manual PowerShell workflow and optional browser inspection were not executed as part of this verification. No production identity provider, deployment, cross-process inventory store, inventory database migration, or live browser inventory editor was verified.

The delivered memory operation is atomic for ordinary calls within its shared singleton store. It remains non-durable and has no idempotency key. The service checks work-order existence but does not impose terminal-status rules or campus ownership. Demo headers remain caller-controlled. Exercises proposing durable storage, retry semantics, stronger workflow rules, and a browser editor are learning work still to be completed by the learner, not claims about delivered runtime features.
