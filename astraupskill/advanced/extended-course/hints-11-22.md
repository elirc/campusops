# Hints for policy, integration, and capstones

For procurement, inspect the strictness of the threshold comparison and then list fields that are stored without being validated. An expiry timestamp and a quote reference can exist without governing a decision. Approval actor text and route role are separate pieces of evidence. Receiving goods is another workflow, not a consequence of Approved status.

For service levels, compute the signed duration used by Resume. A normalized UTC instant can still precede the pause. Then distinguish equality at DueAt from strictly overdue and inspect whether the batch captures time once or reads it repeatedly. A state marker for escalation does not deliver a notification.

For preventive maintenance, advance from the old scheduled instant, not from the processing clock. A far-overdue plan may remain due after one generation. Compare Task length with the generated request summary constraint. Finally, draw two boxes for the API and worker processes and place each singleton registration inside its own box.

For webhooks, mark the first persistent-in-memory effect: the idempotency key is claimed before subscription listing. A retry after failure can therefore be suppressed without any delivery. Treat enqueue, transport attempt, and remote processing as three different outcomes. The delivery state's methods need their own terminal-transition review.

For API and authorization exercises, inspect the actual mapping builder and response projection. A similar URL prefix does not inherit another group's policy. A response location does not prove a corresponding GET exists. A verified actor must come from trusted identity evidence, while target campus and attribution strings remain request data to validate.

For SQLite, follow a mutable asset from row reconstruction through ChangeStatusAsync and look for the missing write. A returned object and a fresh database context are different observers. For reporting, compare the predicates used by OpenWorkOrders and WorkByPriority, and remember that detached parts and live work orders have different observation behavior.

For fault experiments, state the seam and initial state before implementing the wrapper. Catch the designated fault and inspect every relevant record. A passing limitation observation should be replaced deliberately when a copied implementation is repaired, rather than preserved as a desired business rule.

For the inventory capstone, write the allocation-balance equation before designing tables. Receipts and business effects must agree under one commit or explicit recovery protocol. For the operational-events capstone, keep occurrence, command, event, delivery, and attempt identities distinct. Each answers a different retry question.

For final review, label evidence as source-observed, executed, designed, or proposed. A complete instructional route can include unimplemented capstone assignments without pretending those features shipped. The handoff should make the next implementation boundary obvious and preserve the original application.
