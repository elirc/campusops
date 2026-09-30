# CampusOps extended operations workbook

This workbook expands the [advanced inventory route](../README.md) into a substantial source-grounded course. It contains 22 chapters, 66 independent exercises, separate hints and worked solutions, and two complete feature-capstone briefs. The course exceeds 37,000 new prose words excluding fenced code. Capstone implementations remain learner projects; their instructional requirements and review criteria are complete.

CampusOps offers an important starting point: the current parts store already protects stock and reservation records together within one process and returns detached part snapshots. The course will analyze what that boundary establishes, why its dependency registrations matter, and which guarantees do not extend automatically to other modules or another process.

## Published route

| Chapter | Engineering question | Deliverable |
|---|---|---|
| [01: Quantity invariants](01-stock-algebra-and-boundaries.md) | Which arithmetic relationships must every stock command preserve? | A stock ledger and boundary proof |
| [02: Snapshot ownership](02-snapshot-ownership-and-store-authority.md) | Who owns mutable stock and its matching reservation history? | An ownership diagram and composition review |
| [03: Reservation commit](03-reservation-commit-and-history.md) | Which changes become visible together? | Private-candidate and publication ledger |
| [04: Cancellation](04-cancellation-before-commit.md) | When does the store accept responsibility for an outcome? | Token observation and outcome table |
| [05: Duplicates and retries](05-duplicates-retries-and-command-identity.md) | Is this a new allocation or an old request? | Operation identity and receipt proposal |
| [06: Inventory concurrency](06-concurrency-and-observable-invariants.md) | Which invariants survive competing requests? | Assertion map and authority review |
| [07: Work-order versions](07-work-order-lifecycle-and-version-evidence.md) | What does a successful version check protect? | Transition table and interleaving |
| [08: Conversion and references](08-request-conversion-and-location-integrity.md) | Where do locally valid records form inconsistent relationships? | Conversion fault and reference-policy design |
| [09: Assignment authority](09-technician-capability-and-assignment.md) | Who defines capability, requirement, and permission? | Qualification and actor boundary map |
| [10: Audit history](10-comments-audit-and-partial-history.md) | What happens between two history appends? | Two-stream ledger and audit contract |
| [11. Procurement thresholds and approval evidence](11-procurement-thresholds-and-evidence.md) | Where does approval policy depend on evidence? | Worked investigation and independent exercises |
| [12. Service-level time and pause accounting](12-service-level-time-and-pause-accounting.md) | How do pauses change a deadline? | Worked investigation and independent exercises |
| [13. Preventive maintenance and process boundaries](13-preventive-maintenance-and-process-boundaries.md) | What state does a separate worker share? | Worked investigation and independent exercises |
| [14. Webhook enqueue and retry state](14-webhook-enqueue-and-retry-state.md) | What happens after an enqueue failure? | Worked investigation and independent exercises |
| [15. API contracts and client observations](15-api-contracts-and-client-observations.md) | What does an HTTP response establish? | Worked investigation and independent exercises |
| [16. Role policy and campus scope](16-role-policy-and-campus-scope.md) | Which routes enforce actor and campus policy? | Worked investigation and independent exercises |
| [17. SQLite substitution and durable contracts](17-sqlite-substitution-and-durable-contracts.md) | What changes when an adapter returns detached objects? | Worked investigation and independent exercises |
| [18. Operational reports and consistent meaning](18-operational-reports-and-consistent-meaning.md) | Which observations belong in one operational report? | Worked investigation and independent exercises |
| [19. Fault experiments and safe learning copies](19-fault-experiments-and-safe-learning-copies.md) | How can a fault experiment remain isolated? | Worked investigation and independent exercises |
| [20. Capstone: durable inventory lifecycle](20-capstone-durable-inventory-lifecycle.md) | How would durable inventory preserve its invariants? | Worked investigation and independent exercises |
| [21. Capstone: recoverable operational events](21-capstone-recoverable-operational-events.md) | How would operational events recover after failure? | Worked investigation and independent exercises |
| [22. Evidence review and course handoff](22-evidence-review-and-course-handoff.md) | What evidence makes a course handoff trustworthy? | Worked investigation and independent exercises |
| [Hints](hints-01-02.md) | Which observation will expose a mistaken assumption? | Focused clues before solutions |
| [Solutions and rubric](solutions-01-02.md) | What makes an answer correct and appropriately scoped? | Worked reasoning and review criteria |
| [Hints for chapters 03-10](hints-03-10.md) | Which boundary is easy to confuse? | Targeted clues |
| [Solutions for chapters 03-10](solutions-03-10.md) | Which answers agree with the actual source? | Worked traces and proposed contracts |
| [Hints for chapters 11-22](hints-11-22.md) | Where should the investigation start? | Targeted clues |
| [Solutions for chapters 11-22](solutions-11-22.md) | Which answers separate observation from proposal? | Worked answers and capstone rubrics |

Use the existing [compatible disposable lab](../03-FAILURE-LAB.md) when an exercise requires source changes. Its generator copies current source and introduces checked, signature-compatible defects outside the application. Do not restore historical snapshots over current source. Preserve all application and user changes.

The [disposable observation labs](lab/README.md) provide six workflow checks and four policy checks using current copied domain code and real selected in-memory stores. Its limitation observations are deliberately distinguished from desired future regression contracts.

## Using the completed route

Read chapters 01-06 for the inventory boundary, 07-10 for workflow ownership, 11-18 for policy and integration, and 19-22 for experiments, capstones, and assessment. Try the exercises before opening their separate solutions. Both capstones include required evidence and evaluation criteria; they propose extensions instead of claiming those features are implemented in CampusOps.

All ten checks in the two new disposable suites passed against copied current source. Existing-output and in-project-output refusal checks protect the source workspace. These bounded observations do not establish HTTP authorization, database restart durability, or cross-process coordination. Keep those verification layers separate when implementing the capstones.

Start with C# references, interfaces, tasks, exceptions, integer arithmetic, and basic collection knowledge. Learn unfamiliar syntax while following one small operation rather than reading the entire repository without a question. Every exercise should leave an artifact another engineer can inspect: initial state, call sequence, expected result, actual observation if executed, and a specific limitation.
