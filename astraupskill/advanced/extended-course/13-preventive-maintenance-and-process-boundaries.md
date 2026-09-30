# 13. Preventive maintenance and process boundaries

Preventive maintenance turns recurring plans into triaged requests. CampusOps includes the domain service and a separate worker executable, which makes it useful for studying scheduling, recurrence, partial progress, and process-local state. This chapter distinguishes the ability to generate a request from the operational claim that the API will see that request after a background run.

Read [MaintenancePlan and its service](../../../src/CampusOps.Core/PreventiveMaintenance/MaintenancePlan.cs), [the worker loop](../../../src/CampusOps.Worker/PreventiveMaintenanceWorker.cs), [worker composition](../../../src/CampusOps.Worker/Program.cs), and [the preventive-maintenance test](../../../tests/CampusOps.Infrastructure.Tests/PreventiveMaintenance/PreventiveMaintenanceTests.cs). The exercises use fixed clocks and direct service calls; they do not require leaving a worker running for minutes.

## The plan describes recurrence, not history

MaintenancePlan requires nonempty plan, asset, and location IDs, a nonblank task, and an interval of at least one day. It trims Task, stores IntervalDays, and normalizes NextDueAt to UTC. It does not verify that the asset or location exists or that the asset remains at that location. Those references are supplied values at this boundary.

IsDue accepts equality: a plan is due when NextDueAt is at or before the supplied current instant. MarkGenerated advances NextDueAt by exactly IntervalDays from its previous value. It does not set the next date relative to the current processing time. That preserves a recurrence anchored to the prior schedule rather than allowing every late run to shift the schedule forward permanently.

The plan does not retain a list of generated occurrence IDs, requests, or processing results. Its current next-due value is the only recurrence progress shown in the object. A future audit or retry design needs explicit occurrence identity if it must prove which scheduled instance produced which request.

## Generation orders its effects

GenerateDueAsync lists plans and filters those due according to clock reads. For each selected plan it constructs a MaintenanceRequest whose summary is Preventive followed by the task, whose description identifies the plan, and whose location and asset come from the plan. It triages the request to Normal, adds it to the request store, advances the plan, and increments a count.

The request is therefore already Triaged when stored on the successful path. This differs from an ordinary submitted request awaiting manual triage. The service's generated priority is fixed at Normal in current source rather than derived from asset criticality or a plan-specific priority. Those could be future policy fields but are not implied by recurrence.

The order of add and advance matters. A failure before request insertion leaves the plan unadvanced. A failure after insertion but before advancement can leave a request while the plan still appears due. The default in-memory adapter completes synchronously, but a fault wrapper can demonstrate the latter ambiguity without changing application source.

## Missed periods are handled one per invocation

Suppose a plan is due January 1 with a thirty-day interval, and processing occurs April 1. MarkGenerated advances the scheduled date by one interval after creating one request. The plan may remain overdue. GenerateDueAsync does not contain an inner catch-up loop that generates every missed period in a single invocation.

A later invocation can generate another request and advance again. This is a particular catch-up behavior, not necessarily the product's ideal policy. Alternatives include generating one consolidated overdue job, skipping missed occurrences to the next future date, or generating each missed occurrence with a bounded batch. Each needs explicit identity and workload rules.

The existing test starts only one minute overdue with a thirty-day interval, so one advancement moves NextDueAt into the future. Its passing result does not establish how a plan several intervals behind should behave. Add a far-overdue case when reviewing recurrence semantics rather than extrapolating from the near-deadline fixture.

## Generated text can fail another domain boundary

MaintenancePlan limits neither Task length nor the generated summary length. MaintenanceRequest limits its trimmed summary to one hundred twenty characters. Prefixing a long valid plan task can therefore produce a request.summary_invalid error during generation. The plan itself was accepted, but its projection into a request fails a different constructor contract.

This is a useful composition test: each input model can be locally valid while their combination violates an output constraint. A proposed plan rule could reserve room for the prefix, or the generation policy could create a bounded summary and preserve the full task elsewhere. Silent truncation should not be chosen without considering what technicians need to identify the job.

The failure occurs before request insertion and plan advancement in the current order. The plan remains due and can fail repeatedly on later sweeps. An operational design should surface that persistent validation problem rather than treating it as a transient storage error worth retrying indefinitely.

## Time reads have more than one role

The service calls clock.UtcNow during due filtering and again when constructing each request. It does not capture one shared instant for the sweep. A sequence clock can therefore expose different eligibility and submission instants. The differences may be small with SystemClock but remain part of the current behavior.

Decide whether a generated request should record processing time, scheduled due time, or both. Current SubmittedAt uses the processing clock. The description carries plan ID but not a structured scheduled occurrence instant. A durable recurrence design benefits from recording both the due occurrence identity and actual generation time so delays are measurable and retries are unambiguous.

Avoid using the displayed timestamp alone as a deduplication key. Two occurrences can be processed at the same instant in a batch, while one occurrence can be retried at different instants. Plan identity plus scheduled occurrence is a more meaningful proposed key when the policy treats each schedule occurrence as distinct.

## The worker waits before its first sweep

PreventiveMaintenanceWorker creates a one-minute PeriodicTimer and waits for ticks before calling the service. It awaits each invocation and logs only when the returned count is positive. There is no immediate generation call before the first wait and no local exception handler around the service call in the shown loop.

A one-minute timer is not a guarantee that every due request appears within one minute. Processing duration, startup, scheduling delay, failures, and invalid plans affect completion. Host-level behavior after an unhandled service exception requires its own configuration and runtime evidence; do not claim the loop automatically retries every failure simply because a timer exists.

A proposed resilient worker should separate persistent invalid-plan failures from transient infrastructure failures and record enough context for recovery. It also needs a policy for successful prefixes if one plan fails later in a sweep. Logging only the returned aggregate count cannot describe a method that exited before returning it.

## Separate executables have separate stores

Worker Program registers its own in-memory plan store and request store. The API Program independently registers its request store. These singleton instances live in different host processes when the executables run separately. A request generated inside the worker's memory is not automatically visible in the API's memory.

The worker composition also does not seed plans or expose an API for adding them in this file. The presence of the executable proves a scheduling component exists, not that it shares the application's data or has configured work to process. A real integration plan needs shared durable storage or another explicit transport between the processes.

This is a particularly important teaching boundary. A same-process service test passes real store instances to the generator and can observe generated requests immediately. That test does not exercise the two-process deployment topology. Keep its useful result while refusing to promote it into a claim that the API and worker already form one durable system.

## Guided investigation and exercises

Use the existing test's fixed-clock setup as a starting point. Add equality, far-overdue, long-task, and before-add failure cases. Record the generated request's status and fields as well as plan advancement. For process integration, write a design test that would start two disposable processes against a shared store, but do not report it executed unless you build that environment.

**C13.1: Recurrence policy.** Compare one-occurrence-per-sweep, skip-to-future, and bounded catch-up policies for a plan three intervals overdue. Choose one proposal and define occurrence identity, resulting request count, and next due instant.

**C13.2: Projection failure.** Construct a valid plan whose generated request summary is too long. State the current failure and unchanged state, then propose a validation or projection policy that preserves useful task information.

**C13.3: Deployment bridge.** Design how the separate API and worker share plans and generated requests. Include restart behavior, duplicate occurrence prevention, and a test that crosses the actual process boundary rather than only calling the service in one test process.

## Review checkpoint

For an operational handoff, include one concrete overdue plan example with its old due instant, processing instant, generated request ID, and next due instant. That compact record lets a reviewer distinguish schedule advancement from processing delay and verify that the chosen catch-up policy is actually represented.

Your preventive-maintenance contract should connect recurrence identity, generated-request validity, progress publication, and deployment topology. A timer and a passing local generation test are valuable components, but they do not supply the missing shared data and recovery boundary by themselves.
