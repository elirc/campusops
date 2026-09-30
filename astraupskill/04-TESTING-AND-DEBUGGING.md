# Prove the promise at the right boundary

A test suite becomes useful when you can explain which failure each test detects. This feature uses domain tests for arithmetic, infrastructure tests for ownership and concurrency, and HTTP tests for routing, dependency wiring, and access policies. Running only one layer would leave important promises untested.

## Start with the fastest useful test

From the root:

```powershell
dotnet test tests/CampusOps.Core.Tests --filter FullyQualifiedName~Inventory
dotnet test tests/CampusOps.Infrastructure.Tests --filter FullyQualifiedName~Inventory
dotnet test tests/CampusOps.Api.Tests --filter FullyQualifiedName~Inventory
```

The core overflow test should report a domain code and unchanged state. If it fails because no exception was thrown, inspect the order of validation and assignment. If it fails with an unrelated arithmetic exception, inspect whether the API can map that exception into a stable business response. A generic failure is less useful to a client than a documented domain code, but do not catch every exception and hide programmer defects.

The isolation tests deliberately mutate objects returned by the public store API. They then read again. This verifies ownership, not the name of a helper method. A test that merely checks `Snapshot()` was called would be coupled to the implementation and could pass while nested mutable fields still leaked.

## Make concurrent tests actually compete

[ConcurrentReservationTests](../tests/CampusOps.Infrastructure.Tests/Inventory/ConcurrentReservationTests.cs) starts 64 tasks behind one asynchronous gate. Releasing the gate lets scheduled requests compete for five units. It asserts five successful receipts, five unique history records, zero available stock, and equality between reserved quantity and the sum of history quantities. The test uses no arbitrary sleep.

The original test called methods that completed synchronously while constructing a sequence of tasks. `Task.WhenAll` waits for tasks; it does not retroactively make already-completed work simultaneous. Learn to inspect where execution actually yields. The new gated test creates contention opportunities, but a passing stress scenario is still not a mathematical proof of every thread schedule. The private-lock design and shared-read boundary provide the reasoning that complements the test.

## Inspect cancellation and failure state

Cancellation tests use a token cancelled before the store operation and assert both state surfaces afterward. Testing only for `OperationCanceledException` is insufficient: an implementation could mutate and then throw. Similarly, insufficient-stock tests verify no extra history. Good negative-path tests usually need an outcome assertion and an unchanged-state assertion.

The API tests distinguish missing identity (401), the wrong demo role (403), missing resources (404 for part GET), and domain rejection (the current mapper uses 400). They also follow the actual Location header. Hard-coding a guessed GET URL would fail to verify whether the server advertises a usable location.

## Diagnose the local browser separately

If the web console shows an API error, inspect the network request before rewriting React state. Check that the API launch profile is running on 5110, the Vite proxy points there, and the request includes the teaching headers. A production build does not provide that proxy. Then check the response contract: the work-order endpoints map enum values to strings used by the frontend filters.

The existing web test covers combined query/status filtering; the production build checks TypeScript and bundling. Neither proves browser navigation, accessibility, or a live proxy request. A manual browser session or browser automation is a separate verification step. The hook also has more lifecycle scenarios than the current tests cover; stale responses and retry cancellation are useful future frontend exercises.

After a focused fix, run `dotnet test CampusOps.slnx --configuration Release`, then the web test and build commands in the course README. Save the meaningful output and explain the limits. A mid-level review report says which contracts passed and which environments were exercised, instead of presenting a test count as universal proof.
