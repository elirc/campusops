# 19. Fault experiments and safe learning copies

A useful failure lab changes one condition at a known boundary and records the resulting state. CampusOps now provides several contrasting examples: inventory prepares a paired commit under one authority, conversion mutates a request before adding an order, history appends two streams separately, and webhook enqueue claims a key before creating deliveries. This chapter turns those observations into a repeatable experimental method while preserving the original project.

Read [the original generator](../lab/generate_lab.py), [the compatible mutation recipe](../lab/recipe.json), [the workflow generator](lab/generate_workflow_lab.py), and [workflow instructions](lab/README.md). The new experiments should run in explicitly named external outputs. Historical snapshots remain reading material; they are not an instruction to overwrite current application files.

## Ask a narrow question first

Replace "is conversion reliable?" with "what request status remains if order insertion throws before storing?" Replace "does idempotency work?" with "what happens after the event key is claimed and subscription listing fails?" These questions name a successful prefix, an exact fault, and the state needed to interpret the result.

Write initial state before implementation. A conversion experiment needs one Triaged request and an empty work-order store. A history experiment needs one existing order and empty comment/event streams. A webhook experiment needs a fresh idempotency store so an old key cannot accidentally make the test skip its intended fault seam.

Then predict both caller outcome and state. The expected designated exception is only half the experiment. You must also query the affected records through the appropriate observer. A test can reach the right exception and still miss the most important side effect if it never inspects the request, stock, or history afterward.

## Preserve the relevant collaborator behavior

A fault wrapper should change only the seam needed for the question. The workflow lab's order-add wrapper throws before addition while delegating reads to a real in-memory store. Its history wrapper delegates comment append and queries but throws before event append. This preserves the ownership and observation behavior relevant to those cases.

If a wrapper clones objects that the real store returns live, it changes the experiment. That may be useful as an explicit detached-adapter model, but it should not be reported as evidence about the current default. Likewise, a fake transaction that automatically restores every object assumes the consistency property you may be trying to test.

Review doubles as executable specifications. Check cancellation forwarding, identity filtering, ordering, and whether a thrown exception occurs before or after storage. A class called FailingStore is too vague when those differences determine the result. Prefer names such as BeforeEventFailure that explain the boundary directly.

## Catch the intended fault, not every failure

Use a dedicated InjectedFailure type for deliberate infrastructure faults. A test should fail if it instead encounters a null reference, wrong domain rule, or malformed fixture. Catching Exception and declaring success can hide a broken experiment behind the expected idea that something failed.

Counters can confirm that the intended seam was reached. For conversion, one AddAttempts count proves the service reached the injected order-add boundary. State assertions then establish the partial outcome. The counter is instrumentation; it does not replace the business observation. Keep those roles distinct in the report.

Domain failures need their own expected code. For example, invalid stock quantity should produce part.quantity_invalid before publication. A generic exception assertion would accept unrelated failures and weaken the test. For current limitation observations, name the limitation explicitly so future repairs can replace the assertion with a desired regression contract.

## Before-effect and after-effect faults differ

A before-add wrapper leaves no new record. An after-add wrapper delegates the write and then throws. Both make the service call fail, but their retry implications differ. In the after-add case, the caller lacks confirmation of an effect that happened. This is a local model of ambiguous response, not a proof of durable crash behavior.

For webhook enqueue, a failure immediately after key claim can leave zero deliveries and a consumed key. For conversion, a before-order-add failure leaves the request Converted. For comments, a before-event failure leaves the comment. These traces demonstrate that different service orders create different recovery obligations even when all use Task-returning store methods.

Inventory's current combined command deliberately avoids an external fault seam between its two publication writes. Do not inject an arbitrary callback into that interval and then claim the unmodified implementation normally calls external code there. An instrumented copied variant can teach why the boundary is designed that way, but it must be labeled as a changed model.

## Safe generation is part of the evidence

The original generator requires a new absolute output outside the application, reads current selected source, validates mutation anchors, and records hashes. It creates local build configuration and a project without external package references. These choices make the lab compatible with current signatures and keep dependency setup from becoming the main exercise.

The workflow extension reuses that output validation and adds selected real adapters with hashes. Its Program template supplies the observations. This preserves a clear distinction between application source, deliberate starter mutations, and harness code. A reviewer can inspect SOURCE-MAP.json to understand provenance rather than guessing which historical snapshot was executed.

The filesystem guards are intended for ordinary learner mistakes, not a hostile local actor racing path changes. Do not describe them as a security sandbox. The practical guarantee is that documented commands target a known new disposable directory and the tests confirm refusal of existing or in-project destinations.

## Build results need correct classification

The original starter is expected to compile and then fail five behavioral checks. The original reference is expected to pass seven. The extended workflow suite expects six passes, including observations of current limitations. A missing SDK, source-drift refusal, or compilation error is not an intended behavioral failure.

When adding your own cases, update the expected total. A report quoting the baseline count after changing Program obscures what actually ran. Record the output path, suite, source map, command, exit code, and named results. A short exact run record is more useful than an unqualified statement that all tests passed.

Keep generated outputs until review is complete. They preserve the exact copied source and harness behind the result. Cleanup is a separate manual operation using the printed path, not an automatic deletion step embedded in a learning command. No cleanup is required to prove the behavior.

## Deterministic scheduling and bounded waits

Use explicit gates when an experiment needs two callers to reach a read boundary before either proceeds. A shared start signal is useful for contention tests, while a deeper gate can force a particular stale observation. Avoid Thread.Sleep as the primary explanation of a race; machine load can make the result intermittent or hide the overlap entirely.

Every artificial wait should have a bounded timeout and an informative failure. If one caller rejects before reaching the gate, the test should say which expected arrival was missing. A hang does not teach the invariant and can waste review time. The gate must match a path the fixture guarantees under the chosen implementation.

The workflow version observation uses a deliberately ordered domain-method sequence instead of pretending to reproduce an HTTP race. That is valid evidence for the narrow claim that separate successful comparisons do not reserve later mutation authority. Stronger service or deployment claims need their own experiments at those boundaries.

## Guided experiment design

Choose one of procurement, service levels, preventive generation, or webhook enqueue. Build a two-case comparison where only the relevant boundary changes. Examples include threshold equality versus just above, resume before versus after pause, a near-due versus several-periods-overdue plan, or successful versus failed subscription listing after key claim.

State whether the experiment uses actual copied source, an instrumented variant, or a proposed implementation. Then identify the next stronger claim it does not prove. This habit makes even a small console harness useful for engineering review without inflating it into database, authorization, or restart evidence.

## Independent exercises

**C19.1: Fault catalogue.** Define six named seams across conversion, history, webhook enqueue, and a proposed durable inventory command. For each, record before/after effect, observer, expected exception, and final state.

**C19.2: Harness audit.** Review the workflow generator's path checks, source copying, provenance, and expected outcomes. Propose one improvement to evidence clarity without introducing application writes, package installation, or destructive cleanup.

**C19.3: Discriminating pair.** Implement or specify one paired boundary experiment from the guided section. Explain which plausible wrong implementation it rejects and mark any unexecuted case as a design rather than a passing result.

## Review checkpoint

For each laboratory repair, retain one deliberately wrong variant that the new regression rejects. This can be a short documented mutation rather than another permanent suite. It demonstrates that the assertion is discriminating and prevents a superficially green test from surviving the very defect the exercise was intended to address.

Your experiment should be small enough to explain completely and strong enough to distinguish a real alternative. Explicit seams, faithful collaborators, source provenance, and bounded execution make the result reviewable. Those same habits will keep the capstone prototypes honest about the guarantees they actually implement.
