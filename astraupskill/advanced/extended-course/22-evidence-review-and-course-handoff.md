# 22. Evidence review and course handoff

CampusOps now provides a complete extended route from stock arithmetic to operational recovery design. The final chapter assembles the artifacts into a handoff another engineer can review without relying on your confidence or memory. It also defines what completing the workbook means: coherent instruction, attempted exercises, separate solutions, reproducible labs, and honest evidence boundaries, not an unrequested rewrite of the application.

Use [the course route](README.md), [workflow lab](lab/README.md), [Program](../../../src/CampusOps.Api/Program.cs), and [the original advanced route](../README.md). Keep the existing source and test changes owned by the user intact. Your final deliverable should explain both the guarantees already present and the proposals that would extend them.

## Four labels prevent overclaiming

Label a finding source-observed when it follows from inspected implementation. Label an experiment executed when its command actually ran and its output was recorded. Label an unrun test designed. Label a desired change proposed. A detailed diagram or expected result does not change the category by itself.

For example, inventory snapshot ownership is supported by source and existing tests, while the new workflow lab executes additive retry and partial-history observations. A durable inventory receipt design remains proposed unless implemented in a disposable prototype. A route policy matrix read from source is not the same as a executed request matrix under a running test host.

These labels make the handoff stronger, not weaker. They let a reviewer decide which evidence is sufficient for the next action and which boundary still needs implementation or testing. Vague claims that everything is verified hide the most important distinction: verified for what purpose and under which environment?

## Preserve the strongest current guarantee accurately

The parts store owns its mutable state, copies inputs and reads, rejects duplicate aggregate IDs, checks cancellation before publication, and coordinates stock with reservation records under one gate. Program resolves both relevant interfaces to the same singleton instance. This is a substantial same-process boundary and should be described as such.

Do not dilute it into the generic statement that in-memory code cannot be safe. It protects meaningful invariants within its scope. Equally, do not inflate it into durable, cross-process, or logical retry guarantees. Repeated accepted commands are additive, and a separate process has separate memory under the teaching defaults.

A good engineering review recognizes both the achieved property and the missing extension. That habit avoids unnecessary rewrites and focuses future work on concrete requirements, such as durable lifecycle receipts or active-work-order coordination, rather than on fashionable architecture labels.

## Explain module differences instead of averaging them

Work-order stores return live objects and expose a separate version comparison. History uses separate queues. Procurement mutates live request objects. Service levels use their own time rules. SQLite adapters reconstruct detached assets without an explicit status-save path in the current interface. These differences are central to the course and should remain visible in the final report.

Do not write that CampusOps universally snapshots, universally honors cancellation, or universally persists mutations. Each statement is true only for particular paths. An evidence table organized by workflow and adapter makes those distinctions readable. A single capability checkbox for the whole repository often hides more than it explains.

The same applies to authorization. Some groups require Operations or Manager, while other mappings have no explicit requirement in the inspected composition. Campus checks are localized, and the demo scheme trusts supplied headers. A source-specific policy inventory is more useful than a broad secure-or-insecure verdict that cannot guide implementation.

## Build an evidence index

Create one row for each major claim: quantity bounds, snapshot ownership, paired reservation publication, cancellation acceptance point, duplicate command behavior, work-order version scope, conversion failure, skill ownership, audit completeness, threshold equality, service-level chronology, recurrence identity, webhook enqueue recovery, API response shape, policy scope, adapter persistence, and report consistency.

Each row should name the source symbol, relevant test or lab, expected observation, actual result if executed, and a limitation. Link to detailed chapter artifacts rather than repeating whole explanations. The index should allow a reviewer to locate the strongest evidence quickly and identify where a proposal still depends on untested assumptions.

Include source provenance for executed labs. The generators' maps identify copied application bytes and intentional starter defects. Record the suite and output path as well. A result from an older copy may remain useful historical evidence, but it should not be silently applied to changed current source without checking the relevant differences.

## Review the experiments as experiments

The workflow lab uses current copied source and real selected in-memory adapters. Its version check is a domain-method interleaving, not an HTTP race. Its cancellation test chooses public before-and-after boundaries, not an exact internal instruction. Its designated faults occur before order insertion and event append. Those details belong next to the pass count.

The later policy observations, if executed, likewise remain in-memory and network-free. A past quote, backward resume, or burned idempotency key can be demonstrated without starting the API. That makes the experiment inexpensive and precise, but it does not establish transport binding, authorization, database rollback, or worker process integration.

Do not rerun broad suites merely to make a documentation report look more impressive. Choose checks appropriate to changed artifacts and unresolved claims. If only course navigation changes after a successful harness run, link and count checks may be enough. A changed generator or harness, however, needs a fresh bounded execution and isolation verification.

## Capstone completion has stages

The inventory capstone's design is complete when equations, records, command identity, transaction sets, concurrency cases, and recovery semantics agree. A model implementation demonstrates those decisions under its own authority. A durable implementation adds actual storage evidence, and a restart milestone adds a process boundary. These are distinct review verdicts.

The operational-events capstone follows the same discipline for occurrence generation, conversion, outbox publication, and delivery. A stable identity map and failure trace are required even before infrastructure code. Optional choices such as a particular broker or external identity provider do not block a complete feature brief, but they cannot be claimed as implemented integrations.

When handing off a prototype, state the exact milestone and remaining work. A reviewer should not have to inspect every file to discover that the database is simulated or the sender uses a fake handler. Explicit scope makes a bounded prototype credible and useful as a basis for further engineering.

## A practical oral review

Explain one stock reservation from input validation through candidate preparation, paired publication, and detached readback. Ask a peer to challenge you with cancellation, duplicate submission, and a terminal work order. Your answer should distinguish implemented guarantees from policies that would require another authority.

Then explain request conversion and webhook enqueue under their designated failures. Predict what remains after the exception and what a retry does. These paths differ from inventory, so a generic answer about transactions or thread safety is insufficient. If you cannot name the stored record that resolves an outcome, mark the recovery gap explicitly.

Finally, explain a report metric and a permission decision. Which records contribute to WorkByPriority? Which mapping applies Manager? Which field proves actor identity? These questions connect technical mechanisms to user meaning. A system can preserve stock perfectly while still presenting misleading counts or trusting supplied attribution.

## Turn findings into a disciplined backlog

Prioritize changes by a concrete trigger and invariant. A small change might validate backward service-level resume before mutation. A medium change might introduce explicit asset persistence commands and adapter tests. A larger change might implement durable inventory receipts or recoverable event publication. Each item should name its acceptance observer and deployment scope.

Avoid backlog entries consisting only of reliability, security, or scalability. Those words do not explain what behavior should change. Likewise, do not automatically implement every limitation observed in a teaching project. Choose changes according to the intended product and authorization, while preserving the course's accurate description of current behavior.

Source preservation is part of the handoff. The course compares the established ninety-six-file application and test baseline and keeps new teaching files separate. That is a scoped claim about those files, not a claim that every unrelated file on the machine was monitored. Use precise language for tooling evidence just as you do for application behavior.

## Independent exercises

**C22.1: Evidence index.** Produce twelve rows spanning inventory, workflow, time, integration, API, authorization, persistence, and reporting. Label each source-observed, executed, designed, or proposed and state one specific limit.

**C22.2: Peer challenge.** Record three counterexamples raised during an oral review and revise your model or proposal. Include at least one lost-response case and one cross-process claim that a local lock cannot establish.

**C22.3: Handoff plan.** Select three future changes of different sizes. Give each a trigger, invariant, owner, acceptance evidence, and scope. Explain which optional capstone integrations remain deferred without describing the completed workbook as unfinished.

## Completion standard

The route is complete when its chapters and separate solutions support a coherent learning progression, its labs reproduce their named observations safely, and its final report avoids unsupported claims. Preserve the artifacts and choose the next implementation boundary deliberately. The transferable skill is the ability to connect a business promise to the exact code and evidence that can enforce it.
