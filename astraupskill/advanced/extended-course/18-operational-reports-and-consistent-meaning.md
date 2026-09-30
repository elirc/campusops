# 18. Operational reports and consistent meaning

OperationsReportService combines assets, work orders, and parts into a summary and exports work-order CSV. Its results are useful only when users understand what is counted and when the underlying observations occur. This chapter analyzes metric definitions, mixed ownership, export structure, and the difference between a live dashboard and an audit snapshot.

Read [OperationsReportService](../../../src/CampusOps.Core/Reporting/OperationsReportService.cs), [the work-order store](../../../src/CampusOps.Infrastructure/WorkOrders/InMemoryWorkOrderStore.cs), [the parts store](../../../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs), and [report tests](../../../tests/CampusOps.Infrastructure.Tests/Reporting/OperationsReportTests.cs). Use a small invented dataset with deliberately different statuses and quantities. Large data is unnecessary to expose semantic mistakes.

## Define each count before interpreting it

Assets is the count returned by the asset store, without filtering by Active, OutOfService, or Retired. OpenWorkOrders counts orders whose status is neither Completed nor Cancelled, so Assigned, InProgress, and Blocked all contribute alongside Open. BlockedWorkOrders counts Blocked specifically. LowStockParts counts parts whose Available is less than or equal to two.

WorkByPriority groups every returned work order by Priority.ToString, including Completed and Cancelled orders. It is not restricted to the same set as OpenWorkOrders. A dashboard can therefore show a priority total larger than its open-work count without an arithmetic bug. The two metrics answer different questions in current source.

Write these definitions into the client-facing report contract. Labels such as open work or low stock often acquire informal interpretations. If a manager expects priority groups only for active work, change the query or label deliberately and add tests. Do not silently reinterpret the data in presentation code while leaving the service contract ambiguous.

## Low stock is based on availability

A part can have ten units on hand and nine reserved, leaving one available and qualifying as low stock. Another part with two on hand and zero reserved also qualifies. The report intentionally uses availability rather than physical stock in the inspected implementation. This makes allocation demand visible but does not directly measure supplier replenishment needs.

A proposed reorder policy might consider expected incoming stock, consumption rate, lead time, or a part-specific minimum. None of those fields appears in this count. Treat the threshold of two as the current simple reporting rule, not a complete procurement recommendation. The earlier procurement model does not automatically feed expected receipts into Available.

Boundary tests should cover Available one, two, and three, plus a high-OnHand/high-Reserved example. A test using only an empty part can establish one obvious low-stock case while missing the choice of Available over OnHand. Deliberately contrasting those two quantities makes the requirement discriminating.

## Three store calls do not form one snapshot

SummarizeAsync awaits assets, then work orders, then parts. A command can change data between those reads. The result therefore combines observations made at different moments unless the adapters provide a stronger shared read contract, which the current service does not request.

Ownership also differs. Parts are detached snapshots copied under their gate. Work orders and assets are live domain references in their default stores. Their fields can change after the collection is obtained and before the summary evaluates them. The collection's membership can be fixed while its members' state is still changing.

An immutable OperationsSummary freezes the values after construction, but it does not make the earlier observations simultaneous. A dashboard may accept this best-effort view. An audit report claiming one coherent operational instant needs an explicit snapshot strategy, such as a database read boundary or a materialized projection with a known version.

## Worked mixed-state report

Begin with two work orders: W1 InProgress and W2 Blocked. The service obtains their live references. Another operation completes W1 before OpenWorkOrders is counted, while W2 resumes before BlockedWorkOrders is counted. Depending on the controlled schedule, the report can reflect changes that occurred after the list call.

This scenario should be described as an allowed observation risk from live references unless an instrumented experiment forces a specific sequence. Do not claim an exact mixed result was executed merely because the source permits interleaving. A test model can expose the distinction between membership snapshot and field snapshot with explicit gates or a controlled enumerable.

For a stronger proposal, copy the required scalar work-order fields under a defined authority and derive all metrics from that projection. Copying each object independently without coordination still has an observation interval, so state the promised level precisely. The product may need a consistent per-store snapshot rather than an expensive system-wide transaction; choose based on use.

## Ordering and priority are visible behavior

InMemoryWorkOrderStore orders by descending enum Priority and then OpenedAt. With the declared enum values, Emergency sorts before High, Normal, and Low. Equal priority and opening time have no explicit identity tie-breaker in that store query. A stable export or page contract may need one if deterministic order across such ties matters.

Priority names are also used as report dictionary keys and CSV values. Renaming a member can alter consumer-facing strings. A proposed stable reporting schema can map internal values to documented codes. Do not assume an internal refactor is invisible to downstream spreadsheets or automated consumers.

There is no pagination in the inspected report service. It loads collections and builds the output string in memory. That is reasonable for bounded teaching data, but a larger export requirement should define maximum size, streaming, snapshot semantics, and cancellation behavior rather than extrapolate scalability from a small fixture.

## CSV syntax and consumer behavior differ

ExportWorkCsvAsync writes Id, Title, Priority, Status, and Version. It escapes Title by quoting values containing comma, quote, carriage return, or newline and doubling embedded quotes. The other fields have controlled types whose ordinary string representations do not require the same free-text treatment.

A title containing a newline creates a CSV field spanning physical lines when correctly quoted. Counting newline characters is therefore not a valid logical row-count assertion. Use a parser or exact expected structure in a focused exercise. Include embedded quotes and commas together to catch escaping that handles one character but not their combination.

Spreadsheet interpretation is a separate contract. A syntactically valid title beginning with a formula-like marker can still be interpreted by a spreadsheet application. The Escape method addresses delimiters, not every downstream evaluation rule. If the intended export is opened interactively in spreadsheets, define a separate safe-cell policy and test that consumer requirement explicitly.

## Reports should not imply hidden linkage

The summary's Assets count does not tell how many assets have open work orders. Its LowStockParts count does not tell which work orders caused the reservations. WorkByPriority does not identify overdue service levels. These may be useful future reports, but they require joins and definitions not present in the current aggregate.

A product designer should resist adding explanatory labels that imply those missing relationships. For example, calling LowStockParts urgent purchases suggests a procurement decision the report has not made. Use names that match the query, then design richer metrics with their own evidence and data requirements.

The same discipline applies to audit use. A current summary may support a rough operational overview while being unsuitable as proof that a specific request was converted, a purchase approved, or a webhook delivered. Those questions require identity-level records and outcome evidence, not aggregate counts alone.

## Guided dataset and exercises

Create one asset in each status, work orders spanning all lifecycle states and priorities, and parts with availability around the threshold. Compute expected metrics manually before running a disposable model. Then compare a report built from copied scalar rows with one using live work-order references under a controlled mutation.

**C18.1: Metric ledger.** Define a dataset where WorkByPriority totals exceed OpenWorkOrders and a high-OnHand part is low stock. Explain why both results follow current definitions rather than indicating a counting error.

**C18.2: Snapshot proposal.** Choose best-effort dashboard, per-store snapshot, or cross-store snapshot semantics. Specify the observation boundary, report timestamp meaning, and a test that distinguishes the chosen guarantee from the current sequence of reads.

**C18.3: Export contract.** Design cases for multiline title, quote/comma combinations, equal sort keys, and formula-like title. Separate CSV syntax, deterministic ordering, and spreadsheet interpretation into distinct requirements and evidence.

Keep that interpretation note beside the exported artifact so a later reviewer can reproduce the grouping decision.

## Review checkpoint

Attach an interpretation note to any proposed exported report: selection criteria, observation time meaning, inclusion of terminal work, and whether quantities reflect available or physical stock. This note is part of the artifact's usability, especially when a spreadsheet is forwarded without the application around it. A report consumer should not need to inspect C# code to discover that priority groups include completed jobs. If a later query changes that definition, version the report meaning or communicate the change explicitly. Stable column names alone do not preserve a stable analytical question.

Your report contract should let a user explain every count and its observation scope. It should avoid treating a projection as a transaction, an aggregate as an audit record, or a delimiter escape as a complete consumer-safety policy. Clear meaning is the first performance and correctness requirement for operational reporting.
