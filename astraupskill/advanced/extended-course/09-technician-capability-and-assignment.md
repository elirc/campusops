# 09. Technician capability and assignment authority

Assignment combines technician eligibility with a work-order transition. CampusOps normalizes skills, checks activity, and requires a matching skill before assigning an Open order. Those are useful rules, but they leave separate questions about who selects the requirement, who may perform the assignment, whether skill data is truly immutable, and how concurrent changes are coordinated.

Read [Technician and AssignmentService](../../../src/CampusOps.Core/Technicians/Technician.cs), [technician endpoints](../../../src/CampusOps.Api/Technicians/TechnicianEndpoints.cs), [contracts](../../../src/CampusOps.Contracts/Technicians/TechnicianContracts.cs), [the store](../../../src/CampusOps.Infrastructure/Technicians/InMemoryTechnicianStore.cs), and [technician tests](../../../tests/CampusOps.Core.Tests/Technicians/TechnicianTests.cs). This chapter uses those exact boundaries rather than assuming a full scheduling engine exists.

## Skill normalization is a local capability rule

Technician requires a nonempty ID and nonblank name. It filters blank skill strings, trims and uppercases the remaining values, and places them in a HashSet. Duplicate normalized skill names collapse into one set entry. HasSkill normalizes the requested string in the same way before checking membership.

The existing skill test supplies a padded lowercase electrical skill and checks a differently cased query. It establishes that normalization supports the intended case-insensitive match for that example. It does not establish synonym handling, certification validity, expiry dates, proficiency levels, or an externally verified qualification. Those are separate product features if needed.

An empty skill set is permitted by the constructor. A nonempty requirement then fails membership in ordinary use. The current methods also assume their enumerable and queried string are valid non-null inputs at the language boundary; nullable analysis and request binding are not substitutes for explicitly defined domain behavior for every malformed caller. Keep any null-handling proposal separate from the existing normalization rule.

## Read-only shape is not immutable storage

The Skills property is typed IReadOnlyCollection<string> but returns the private HashSet instance. That interface prevents mutation through its own members, yet the runtime object remains a HashSet. A caller holding the returned object can identify or cast the concrete collection and mutate it, changing subsequent HasSkill results.

This is a precise ownership observation, not a claim that normal JSON clients can cast a server object. It concerns callers inside the process that receive the domain instance or property. The in-memory technician store returns live Technician objects, so such a caller can reach the collection through ordinary store reads.

A proposed fix can expose an immutable collection, a copied collection, or a read-only wrapper whose ownership semantics are appropriate. Each choice has tradeoffs. A copied array prevents changes from reaching the original set, although the array itself is mutable. An immutable collection communicates stronger value semantics but adds its own representation choices. The invariant is that callers must not acquire authority to alter stored qualifications accidentally.

## Assignment follows a specific order

AssignmentService loads the technician first, rejects absence, rejects inactive state, and checks the requested skill. It then loads the work order and calls order.Assign with the technician ID. WorkOrder.Assign requires Open and a nonempty ID, sets the assignment, changes status to Assigned, and increments Version.

This ordering means an inactive technician error can occur before a missing work order is discovered. A missing skill likewise prevents the work-order lookup. Error precedence follows the source, not a universal priority among invalid conditions. If clients rely on a particular precedence, document and test it; otherwise focus on preventing effects before a rejected assignment.

The service does not accept an expected work-order version, despite WorkOrder exposing version checks for another action service. It also does not check technician workload, shift, location authorization, or overlapping assignments. A skill match is one eligibility condition, not a complete scheduling decision. Do not infer those policies from the class name AssignmentService.

## Who chooses the required skill?

AssignTechnicianRequest contains TechnicianId and RequiredSkill. The endpoint passes both directly to AssignmentService. The service compares the technician against that supplied requirement; it does not derive a requirement from the work order, asset category, or a verified task specification.

If the product requires a certified electrical technician for an electrical job, accepting any caller-selected required skill is not sufficient to enforce that requirement. A proposed design should make the job's required capability authoritative, then compare the selected technician to it. A dispatcher may be allowed to change the requirement through a separately authorized workflow, but that decision should be explicit and auditable.

For a teaching API, caller-supplied skill can be a convenient demonstration input. The correct current claim is that assignment checks the supplied skill against the technician. The stronger claim that assignment enforces the work order's true required qualification is unsupported until the requirement is modeled and trusted.

## Route grouping does not imply shared policy

The assignment endpoint is mapped directly on endpoints at a work-orders path from TechnicianEndpoints. It is not mapped through the authorized work-order group created in WorkOrderEndpoints. A similar URL prefix does not automatically inherit another group's RequireAuthorization metadata. The inspected assignment mapping itself does not add an authorization requirement.

This is source evidence about endpoint construction, not a runtime security test of every configured host. A complete policy review should inspect global fallback configuration and execute representative request tests. Program configures named policies, but merely defining a policy does not apply it to every route with a matching noun in its URL.

A proposed correction should decide the intended actor policy and apply it at the actual mapping boundary. It should also consider resource scope, such as whether a dispatcher may assign across campuses. This chapter does not modify the endpoint; it teaches how to avoid assuming that route naming or nearby code supplies authority checks automatically.

## Worked eligibility change

Technician T is active and has HVAC. Order W is Open. Assignment checks T and finds the requested skill, then pauses before changing W. Another operation deactivates T. The original assignment can still proceed based on its earlier observation because the service has no shared coordination with technician activity changes.

Decide whether the desired policy allows an assignment accepted just before deactivation to complete. If eligibility is a decision-time rule, document that time. If it must hold at commit, coordinate the technician's eligibility version and work-order transition. A second unsynchronized read narrows a gap but does not guarantee that deactivation cannot occur immediately afterward.

The same reasoning applies to changing qualifications in a richer model. An immutable skill snapshot can make observation safer, but it does not decide whether a later qualification change revokes an in-flight assignment. Ownership of the data and timing of the policy are related yet distinct design questions.

## Duplicate assignment and persistence

After one successful assignment, WorkOrder status is Assigned. A sequential second assignment rejects because Assign requires Open. The first TechnicianId and incremented version remain. There is no reassignment method in the current WorkOrder model. A proposed reassignment feature needs its own permitted states, actor authority, and audit story rather than invoking Assign as though it were an unrestricted setter.

The current store returns live orders, so the mutation is visible without a save. A detached or durable adapter would need an explicit assignment persistence command. If assignment also records an audit event, that event must be coordinated or its eventual-delivery meaning documented. The absence of an explicit save is a clue about ownership, not proof of durable persistence.

For simultaneous assignments, local status guards are not a complete expected-version command. The stronger design from chapter seven can be extended to assignment, but it must include the technician requirement decision and the chosen eligibility timing. A copied unit test that only calls Assign twice sequentially cannot prove every concurrent schedule.

## Guided lab and independent exercises

In the disposable workflow lab, create a technician and inspect the runtime collection exposed by Skills. Mutate it through a concrete collection reference and observe HasSkill. Label this as a current ownership limitation. Then replace the exposure only in your copied source and add a regression assertion that external collection changes do not alter the technician's qualifications.

**C09.1: Capability model.** Separate normalized skill membership, verified certification, workload, and actor permission. State which current methods address each concern and propose an authoritative source for a work order's required capability.

**C09.2: Assignment trace.** Record success, inactive technician, missing skill, non-Open order, and repeated assignment. Include status, version, TechnicianId, and error precedence where relevant. Mark concurrency and persistence claims that these sequential traces do not establish.

**C09.3: Boundary review.** Design request tests for the actual assignment mapping and a domain test for skill collection ownership. Explain why a URL prefix and an IReadOnlyCollection type each provide less authority protection than a casual reading suggests.

## Review checkpoint

Deactivation changes IsActive but does not automatically unassign existing orders. A workforce policy may permit a technician's historical assignments to remain while preventing new work, or it may require a reassignment queue. Neither behavior should be inferred from the boolean setter alone. A proposed reassignment queue needs its own owner, failure handling, and actor evidence, especially when several orders are affected.

Your design should distinguish the qualification data, the job requirement, the acting user's authority, and the coordinated assignment transition. These are four separate obligations. A correct skill comparison is useful, but it cannot silently supply the other three.
