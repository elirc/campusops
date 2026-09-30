# 17. SQLite substitution and durable contracts

CampusOps contains SQLite adapters for locations and assets, while the default API registers in-memory stores. This chapter examines what those adapters actually persist, how their ownership differs from live in-memory objects, and why replacing one registration is not enough to make every workflow durable. The goal is a reviewable substitution plan grounded in the current interfaces.

Read [SqliteStores](../../../src/CampusOps.Infrastructure/Data/SqliteStores.cs), [CampusOpsDbContext](../../../src/CampusOps.Infrastructure/Data/CampusOpsDbContext.cs), [the SQLite test](../../../tests/CampusOps.Infrastructure.Tests/Data/SqliteStoreTests.cs), and [AssetService](../../../src/CampusOps.Core/Assets/Asset.cs). Do not install a database package or change application registrations for this chapter. Source analysis and a precise test design are sufficient unless you choose a separate disposable integration milestone.

## The durable model covers two record types

CampusOpsDbContext exposes Locations and Assets. Location rows contain identity and campus/building/room labels. Asset rows contain identity, tag, name, category, location ID, and status string. The model defines primary keys, a unique location-label tuple, a unique asset tag, and a foreign key from asset to location.

It does not contain the inventory, reservation, work-order, request, procurement, service-level, or webhook records discussed in other chapters. Therefore, the existence of this context is not evidence that those modules are durable. A production persistence proposal must inventory the whole related record set rather than treating a database project as a system-wide capability flag.

Program selects in-memory adapters for the default API. The readiness endpoint says durable is false. Preserve that description when explaining the teaching deployment. A future SQLite configuration needs explicit registrations, lifetimes, schema creation, and workflow adaptation rather than an assumption that adapters are selected because their files exist.

## Reads reconstruct detached domain objects

SqliteLocationStore and SqliteAssetStore use AsNoTracking queries and create new domain instances from rows. Locations are effectively immutable in the current domain model. Assets are mutable, so reconstruction produces a detached object whose later mutation does not automatically update the row through this adapter.

The current IAssetStore interface exposes Add, Get, and List, with no Save or state-transition command. AssetService.ChangeStatusAsync gets an Asset and invokes a domain method, relying on the in-memory store's live reference for visibility. Substituting SqliteAssetStore under that service does not add a persistence call; the changed detached asset can be returned while a later read still reconstructs the old stored status.

This is a concrete contract mismatch, not a generic criticism of detached objects. The parts store deliberately uses snapshots plus explicit mutation commands. AssetService currently uses live-object mutation without a persistence command. A durable adapter needs the service contract to express how the change is committed, not merely a different implementation of GetAsync.

## Worked status-change experiment design

Create a location and Active asset through the SQLite adapters in a disposable database. Call AssetService.ChangeStatusAsync to mark the asset OutOfService. The returned object can show OutOfService because the domain method ran. Then load the asset through a fresh context and inspect its stored status.

Under the inspected adapter contract, no update was issued by that service path, so the design predicts the stored row remains Active. Execute this with the real provider before reporting it as a runtime result. The source establishes the missing write path; the integration experiment confirms its observable effect and catches assumptions about tracking.

A test asserting only the returned object's status would pass and miss the persistence gap. This makes the scenario a useful application of the evidence method: choose an observer that distinguishes mutation from durable publication. A second variable referencing the returned object is not a stronger observer.

## Constraints differ from in-memory behavior

The database model's unique indexes reject duplicate asset tags and duplicate campus/building/room tuples under the database's comparison semantics. In-memory stores use GUID keys and do not implement those same business-key constraints. A workflow tested only in memory can accept input that a SQLite write later rejects.

Normalization affects the stored values before constraints apply. Asset tags are uppercased and trimmed. Location room is uppercased, while campus and building are trimmed but not uppercased. Do not assume a universal case-insensitive uniqueness policy for every label without inspecting database collation and testing the intended provider behavior.

Foreign-key enforcement adds another boundary for asset location integrity in the database schema. AssetService already checks location existence before creation, but a concurrent change or direct adapter caller may still rely on the database constraint. Translate expected persistence conflicts into a defined application result if the public contract requires it; do not expose arbitrary provider exception text as the business interface.

## Stored enum names create compatibility obligations

Asset status is persisted with ToString and reconstructed with Enum.Parse. Renaming an enum member can make existing stored strings incompatible with a newer reader unless a migration or stable mapping handles them. Compilation alone cannot reveal that data compatibility problem.

A proposed migration can preserve stable storage codes independent of internal symbol names, accept old and new names during a staged rollout, or transform rows under controlled deployment. Choose an approach based on whether old and new application versions can run simultaneously. Rollback must also consider whether the old binary can read rows written by the new one.

The same issue can affect response strings when endpoints use Status.ToString. Storage and API representations may have different consumers and versioning timelines. A single internal rename can therefore require two separate compatibility reviews. Explicit mappings make those contracts easier to control.

## Context round trips are not restart proof

The existing SQLite test opens an in-memory connection, creates schema and data through one context, then reads an asset through another context using the same open connection. It proves that insertion survives a context boundary and that the asset can be reconstructed with its expected tag.

It does not close and reopen a file-backed database or restart a process. The database's lifetime is tied to the in-memory connection in that fixture. This is still valuable evidence, but describe it as a cross-context test rather than claiming machine-crash or process-restart durability from it.

A proposed durable deployment test should use an explicitly owned temporary database file, create records in one process, terminate normally or at a controlled fault boundary according to the claim, and read from another process. Such a test needs cleanup discipline and a precise crash model. It is not necessary for every constructor rule or documentation change.

## Choose a mutation contract deliberately

One option adds an explicit asset SaveAsync with expected version. Another adds narrow commands such as ChangeStatus and Move that perform conditional updates. A unit-of-work design can coordinate related asset and audit changes in one transaction. Each option should specify missing-row behavior, duplicate identity, concurrency conflict, and response-after-commit semantics.

A general Save without expected state can overwrite newer data from a stale detached object. Narrow commands can preserve unrelated fields but still need lifecycle validation under the coordinated read/write boundary. An ambient tracking design can work, but it should not remain an undocumented assumption hidden behind an interface that sometimes returns detached objects.

For inventory, preserve the existing paired stock/reservation contract when adding durable storage. Two separate Save calls would weaken the current authority if they can commit independently. The durable capstone should provide one transaction over the matching records and any receipt rather than translating the dictionaries into unrelated CRUD methods.

## Guided substitution review

Make a table for every store interface used by one chosen workflow. Record selected implementation, ownership, mutation publication, cancellation behavior, uniqueness, and persistence scope. Mark which cells would change under SQLite. Then identify service methods that rely on live-reference mutation and need explicit adaptation before a registration change.

This review is often cheaper than discovering the mismatch after deployment. It also prevents overcorrection: immutable Location reconstruction may already fit its read-only contract, while mutable Asset status requires more work. Different domain shapes justify different adapter obligations even when their interface names look similar.

## Independent exercises

**C17.1: Observer test.** Design the returned-object versus fresh-context asset status experiment. State the predicted current result and the regression expectation after a proposed persistence command is implemented.

**C17.2: Compatibility matrix.** Compare duplicate IDs, duplicate tags, duplicate location labels, and missing location references across the current in-memory and SQLite boundaries. Mark provider-specific comparison behavior as requiring actual integration evidence.

**C17.3: Durable plan.** Specify the registrations, lifetimes, schema, mutation contracts, and tests needed to make one complete workflow durable. Explain why adding only the existing location and asset adapters cannot make inventory reservations or worker-generated requests durable.

## Review checkpoint

Include a migration rehearsal with a small existing dataset, not only an empty-database creation test. Existing duplicate labels, unknown status strings, or inconsistent location references can fail new constraints. A reviewable plan states how those records are detected and resolved before enforcing the proposed schema, without silently discarding user data to make migration succeed.

Your substitution plan should preserve business behavior across ownership changes and name the exact durable record set. A database-backed read is not enough when the service has no write contract, and a cross-context insertion test is not a restart test. Those distinctions keep persistence claims useful and defensible.
