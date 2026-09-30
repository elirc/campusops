# 12. Service-level time and pause accounting

Service-level rules convert priority into a deadline, allow paused time to extend that deadline, and mark overdue work escalated. CampusOps represents these decisions with a small policy and mutable service-level object. This chapter studies equality, time ordering, repeated transitions, and the relationship between a service-level record and the work order it describes.

Read [ServiceLevel](../../../src/CampusOps.Core/ServiceLevels/ServiceLevel.cs), [service-level tests](../../../tests/CampusOps.Core.Tests/ServiceLevels/ServiceLevelTests.cs), [service-level endpoints](../../../src/CampusOps.Api/ServiceLevels/ServiceLevelEndpoints.cs), and [Program](../../../src/CampusOps.Api/Program.cs). Use fixed DateTimeOffset values in exercises. No waiting for real deadlines or changing the machine clock is needed.

## Priority policy is a supplied mapping

ServiceLevelPolicy accepts an IReadOnlyDictionary from RequestPriority to TimeSpan. DueAt normalizes the opening instant to UTC and adds the target duration. A missing priority mapping throws sla.target_missing. Program supplies five days for Low, two days for Normal, eight hours for High, and one hour for Emergency.

These are elapsed durations, not business-calendar calculations. The implementation does not exclude weekends, campus closures, or overnight hours. A statement that Emergency work is due within one working hour would therefore add a policy not present in the source. If working calendars are required, they need an explicit calendar authority and boundary tests.

The policy stores the supplied dictionary reference through its constructor parameter. A read-only interface does not necessarily mean the underlying mapping cannot be changed elsewhere. A proposed immutable configuration snapshot can prevent runtime target changes through external aliases. First decide whether dynamic policy updates are a feature or an accidental possibility.

## Deadline equality is not overdue

WorkOrderServiceLevel stores a work-order ID and UTC due instant. EscalateIfOverdue returns false when paused, already escalated, or now is less than or equal to DueAt. It escalates only strictly after the deadline, recording the supplied current instant in UTC and returning true.

Test just before, exactly at, and just after DueAt. Equality is especially important because preventive-maintenance due selection uses a different comparison in its own model. Similar words such as due and overdue do not imply identical operators. The business meaning should determine the operator, and the test should make that choice visible.

The object does not validate that WorkOrderId is nonempty or that a corresponding order exists. It also does not know whether that order is Completed or Cancelled. A service-level record can be escalated based on its own state even if the related workflow would make escalation unnecessary under a richer policy. That relationship needs another boundary.

## Pause stores an instant, not a duration

Pause rejects an already paused record and stores the supplied instant in UTC. While paused, EscalateIfOverdue returns false regardless of how far now is beyond the due instant. Resume requires a stored pause instant, adds the difference between resume and pause to DueAt, and clears the paused state.

For example, a deadline is 11:00. Pause at 10:30 and resume at 12:00. The elapsed pause is ninety minutes, so the new deadline is 12:30. This is the arithmetic demonstrated by the existing pause test. It preserves the amount of unpaused time remaining under the simple elapsed-time policy.

Pause does not record a reason, actor, or event history, and only one active pause instant is retained. Repeated pause/resume cycles accumulate deadline extensions, but the object does not retain the detailed intervals afterward. A report explaining why a deadline moved would need additional audit evidence in a proposed design.

## Backward time exposes an unchecked rule

Resume does not reject an instant earlier than the stored pause time. A negative duration therefore moves DueAt earlier rather than later. This can be demonstrated directly with fixed values and no clock manipulation. The current behavior is an arithmetic consequence of the method, not a claim that the application should accept backward chronology.

A proposed chronology guard should validate before changing DueAt or clearing the pause. Its rejection should leave the original deadline and paused state intact. A test checking only the thrown rule could miss a method that subtracts first and then rejects. Apply the unchanged-state discipline learned in the inventory chapters.

Clock normalization does not prevent this issue. Both instants can be correctly normalized to UTC while still being chronologically reversed. Likewise, a monotonic elapsed-time source and a wall-clock timestamp serve different purposes. A future design should state whether pause accounting uses recorded business instants, elapsed runtime duration, or an audited operator correction.

## Escalation is a state marker, not delivery

EscalatedAt being set means this object has recorded one escalation decision. It does not send a notification, create a task, or enqueue a webhook in the inspected service. Those side effects require additional integration. A dashboard may show escalation state while no person has actually received a message.

Subsequent EscalateIfOverdue calls return false because EscalatedAt is already set. Pause and Resume do not clear that field, so changing the deadline later does not reset escalation eligibility automatically. Decide whether that behavior fits the product or whether a separate acknowledged, resolved, or reset lifecycle is required.

A proposed notification mechanism should commit an escalation event or outbox record consistently with the state change. If it merely sends after setting EscalatedAt and then fails, a retry may skip the already escalated record and never deliver the message. This is a concrete integration consequence of the one-time marker.

## The batch reads time per item

ServiceLevelService lists records and invokes EscalateIfOverdue(clock.UtcNow) for each. It does not capture one shared now before the loop. Under a clock that advances, later items can be evaluated against later instants. This differs from a batch that promises one snapshot threshold for the whole sweep.

A sequence clock in a disposable test can distinguish the policies. Give two records the same deadline and return equality for the first read and a later instant for the second. The current loop can escalate the second but not the first, depending on controlled list order. If the product wants a shared threshold, capture it once and test that semantic outcome.

The service counts successful state changes and returns the count. It does not record which items changed in the result, nor does it coordinate an entire sweep as one transaction. A failure from the store before listing prevents processing; a failure introduced during a richer per-item operation could leave a successful prefix. Define operational evidence accordingly.

## Endpoint input is not work-order truth

The start endpoint accepts Priority and OpenedAt in its request body, computes DueAt from those supplied values, and adds a WorkOrderServiceLevel for the route ID. It does not load the work order to derive its actual priority or opening time. A caller can therefore supply values that differ from the referenced order under the current path.

A stronger design can derive policy inputs from authoritative order state, or explicitly support an authorized override with a reason. The two meanings should not be mixed. An ordinary start command should not silently act as a deadline-override interface just because the DTO exposes those fields.

The mapping itself has no explicit authorization call in its inspected file. Route policy review belongs in the later authorization chapter, but it already matters to the interpretation of supplied time and priority. A valid date value is not evidence that the caller may choose the governing deadline.

## Guided investigation and exercises

Construct a service-level object with a fixed deadline and test equality, paused suppression, valid resume extension, backward resume, and repeated escalation. Record state after every rejected or false-returning action. Then design an audit record that could explain each legitimate deadline extension without retaining unnecessary personal information.

**C12.1: Temporal matrix.** Specify before/equal/after deadline results and a ninety-minute pause. Add backward resume as a current observation and a proposed regression rule, with unchanged-state assertions for the proposal.

**C12.2: Batch clock.** Design a two-item sequence-clock test that distinguishes per-item time from one captured threshold. Control list order and explain what the returned count means under each policy.

**C12.3: Escalation integration.** Propose a durable escalation event and notification handoff. Explain why setting EscalatedAt before an unrecorded send can lose delivery on retry, and distinguish deadline inputs from authorized overrides.

## Review checkpoint

Consider policy changes after a service-level record is created. The object stores a computed DueAt rather than a live reference to a priority target. Updating the policy dictionary later does not automatically recompute that existing deadline. A product may intentionally preserve the original commitment, or it may permit an authorized recalculation with a recorded reason. Those are different contracts. A test should create a deadline, change the proposed policy, and verify the selected behavior without assuming that configuration changes rewrite historical obligations. Recording the policy version can make that decision explainable to operators.

Your service-level contract should name the calendar model, equality rule, pause chronology, escalation lifecycle, and authoritative source of inputs. Those details make the deadline meaningful. A timestamp field and a count of escalations do not by themselves establish a complete operational response process.
