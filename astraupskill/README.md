# CampusOps: own a reliable inventory feature

CampusOps is a facilities-management modular monolith. A maintenance request becomes a work order; technicians perform the work; parts support that work. This course teaches how a junior engineer moves from implementing individual endpoints to owning a feature's consistency, failure behavior, setup, and evidence. The concrete feature is reserving inventory for an existing work order. You will read the actual implementation, reproduce its former defects, explain the repair, and extend it through increasingly independent exercises.

Start with basic C# classes, async methods, HTTP requests, and simple tests. You do not need previous distributed-systems experience. Whenever this course discusses a database transaction or idempotency, it explains the problem before proposing an abstraction. The repository's existing fifteen-sprint curriculum remains useful for breadth; this folder supplies the detailed vertical walkthrough and practice that those short sprint outlines leave to the reader.

## Your route through the course

| Step | Read and do | Evidence you should produce | Estimate |
| --- | --- | --- | --- |
| 1 | [Map the codebase](01-CODEBASE-MAP.md) and run the baseline commands | A request diagram with real files and storage lifetimes | 45–60 min |
| 2 | [Understand the concepts](02-CONCEPTS.md) | Examples of invariants, atomicity, snapshots, cancellation, and access control | 60–75 min |
| 3 | Do **Exercise 0** in [practice](05-PRACTICE.md) — generate the signature-compatible disposable starter and watch five checks fail | Your starter output with five failing checks, and one sentence per failure naming the caller who could cause it | 45 min |
| 4 | [Study the worked repair](03-WORKED-CHANGE.md) | A before/after explanation grounded in a failing scenario you have now seen | 60 min |
| 5 | [Test and debug](04-TESTING-AND-DEBUGGING.md) | A focused passing test and a useful failure investigation | 45–60 min |
| 6 | [Exercises 1–3](05-PRACTICE.md) | Small reviewable changes with acceptance criteria | 1 session each (ex. 3 is a design-then-build, budget two) |
| 7 | [Exercise 4a, then 4b](05-PRACTICE.md) | A durable adapter, then a competing-connection test | 1 session each — these are the hard ones |
| 8 | [Exercise 5](05-PRACTICE.md), the UI capstone | A working inventory screen with real error states | 2–3 sessions |
| 9 | [Use the review guide](06-SOLUTIONS-AND-REVIEW.md) after attempting them | Tradeoff notes and evidence of independent reasoning | 45 min |
| 10 | [Trace the feature](07-TRACE-LAB.md) | A reproducible request sequence and observed stock/history values | 45 min |

"One session" means two to three focused hours. Chapters 01–04 plus Exercise 0 are a full day. Exercises 1 and 2 are the ones to do next; 3, 4a/4b and 5 are each a real piece of work and are meant to be attempted over weeks, not in one sitting. If you stall on 4a for more than a session, read the skeleton in [the review guide](06-SOLUTIONS-AND-REVIEW.md) — it is there for exactly that.

The [verification record](VERIFICATION.md) separates executed checks from suggested manual experiments. Do not treat an exercise's proposed behavior as already delivered.

## Glossary

Terms this course uses before it defines them.

- **Invariant** — a promise that is true after *every* operation, not just the one you are writing. "Reserved never exceeds OnHand" is an invariant; a check inside one endpoint is not.
- **Commit boundary** — the span inside which a group of writes either all happen or none do. Here it is one `lock (_gate)` in `InMemoryPartStore`; in a database it is one transaction.
- **Atomicity** — the "all or nothing" property of a commit boundary. The bug this delivery fixed was two writes with no shared boundary: stock could be reserved with no history row.
- **Detached read / snapshot** — returning a *copy* of stored state so the caller cannot change storage by mutating what it was handed. `Part.Snapshot()` does this; EF Core's `AsNoTracking()` is the database equivalent.
- **Live reference** — the opposite: the object you got back *is* the stored object. The old store returned these, which is why a caller could quietly change stock.
- **Idempotency key** — a caller-supplied identifier that lets the server recognise a retry of the same logical request and return the original result instead of doing the work twice. Exercise 3.
- **Oversell** — two concurrent requests both pass an availability check and both commit, so more stock is reserved than exists. The thing exercise 4b tries to cause.
- **Conditional update** — putting the availability check inside the `UPDATE ... WHERE` predicate so the database decides, and reading the affected-row count to learn whether you won.
- **Affected-row count** — what `ExecuteUpdateAsync` returns. `0` means the predicate did not match; it is an outcome, not an error.
- **Policy (authorization)** — a named rule attached to routes with `.RequireAuthorization(SecurityPolicies.Operations)`. It answers "may this caller use this endpoint", not "does this caller own this record" — CampusOps has no ownership field on parts, and this course says so repeatedly rather than pretending otherwise.
- **Terminal status** — a work-order state from which no further transitions are allowed (`Completed`, `Cancelled` in `WorkOrderStatus`). Exercise 2 turns this into a rule.

## Get a working development baseline

From the repository root, use the SDK selected by `global.json` and restore its locked package versions:

```powershell
dotnet restore CampusOps.slnx --locked-mode
dotnet test CampusOps.slnx --configuration Release
npm.cmd ci --prefix web/CampusOps.Web
npm.cmd test --prefix web/CampusOps.Web
npm.cmd run build --prefix web/CampusOps.Web
```

On macOS or Linux, drop the `.cmd`: `npm ci --prefix web/CampusOps.Web`, `npm test --prefix web/CampusOps.Web`, `npm run build --prefix web/CampusOps.Web`. The `dotnet` commands are identical on every platform. Node 20 or newer; the .NET SDK version is pinned by `global.json`, so `dotnet --version` should report what that file selects before you go any further.

The pre-change files in [snapshots](snapshots/README.md) are historical reading material. **Exercise 0 in [practice](05-PRACTICE.md) now uses the [disposable starter](advanced/03-FAILURE-LAB.md), generated from current source, to reproduce behavioral failures** — do that first; it takes under an hour and it is the difference between believing the story and having seen it.

For interactive exploration, start `dotnet run --project src/CampusOps.Api` in one terminal and `npm.cmd run dev --prefix web/CampusOps.Web` in another. The checked-in API launch profile uses port 5110, and the Vite development proxy now points there. If you intentionally override the API URL, update the proxy or configure the web client's base URL consistently. The production Vite build does not include a development proxy.

## What you are learning on top of

This delivery fixes the inventory reservation boundary, detached reads, duplicate-ID replacement, quantity overflow, cancellation before writes, missing-work-order reservations, inventory access-policy wiring, readable part locations, and the local proxy mismatch. Inventory storage remains in one process's memory. The SQLite example covers locations and assets; it does not persist inventory. The separate worker has its own memory stores. A role supplied in demo headers is a teaching mechanism, not verified identity. Parts have no campus ownership field, so role checks do not establish campus isolation.

The React console currently reads and filters work orders. It is not an inventory editor. Building that editor is a deliberate capstone. Your mid-level milestone is being able to state these boundaries accurately, choose a small next change, protect it with the right tests, and explain what evidence still remains before deployment.

## Continue with the advanced feature-ownership pack

Read the [six-chapter advanced route](advanced/README.md) after the original walkthrough, or begin with its [safe disposable failure lab](advanced/03-FAILURE-LAB.md) when you want a compiling red-first exercise. It adds source-grounded traces, failure injection, eight exercises, a feature capstone and a separate [solution rubric](advanced/06-SOLUTIONS-AND-RUBRIC.md). The lab never overwrites application source; historical snapshots are for comparison.
