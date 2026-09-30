# CampusOps advanced: own the inventory boundary

This route takes the existing inventory repair into object ownership, concurrent commands, cancellation, retries and durability. It adds a disposable C# lab built from current source, preserving the current method signatures. You never need to paste an old store implementation over the application.

| Session | Chapter | Evidence |
|---|---|---|
| 1, 45 minutes | [Route and ownership](01-ROUTE-AND-OWNERSHIP.md) | An ownership map and capability boundary |
| 2, 60 minutes | [Stock/history trace](02-STOCK-AND-HISTORY.md) | A state ledger with a linearisation point |
| 3, 60-90 minutes | [Failure lab](03-FAILURE-LAB.md) | Five behavioral failures repaired in copied source |
| 4, 60 minutes | [Concurrency and durability](04-CONCURRENCY-AND-DURABILITY.md) | A cancellation and retry contract |
| 5, two sessions | [Exercises and capstone](05-EXERCISES-AND-CAPSTONE.md) | Eight exercises and an inventory feature proposal |
| 6, 45 minutes | [Solutions and rubric](06-SOLUTIONS-AND-RUBRIC.md) | A reviewable evidence bundle |

Python 3 and the pinned .NET 10 SDK are sufficient for the lab. It needs no API server, browser, database or external NuGet package. The app's existing integration tests and production readiness questions remain separate.

[Return to the original course](../README.md). Treat the earlier snapshots as historical reading material; use the new generator for a compiling starter.

Continue with the [extended operations workbook](extended-course/README.md) for deeper quantity proofs, ownership analysis, workflow investigations, and feature design. The completed route contains 22 chapters, 66 exercises, separate worked solutions, and two feature-capstone briefs.

## Verification of this expansion

Executed on 2026-09-29 with installed .NET SDK 10.0.302, using newly generated directories outside the application. No application suites, servers or dependency installation were run.

| Mode | Result | Exit |
|---|---|---:|
| starter | 2 passed; 5 failed | 1 |
| reference | 7 passed; 0 failed | 0 |

The starter exit is intentional: these are behavioral assertion failures, not compile errors. Both reference harnesses compile from current copied source. Existing-output and in-project-output refusal checks also passed. Application source and test files were hash-checked before and after this work and are unchanged. Database, HTTP and cross-process guarantees are not established by this lab.
