# ADR-001: start as a modular monolith

CampusOps uses one backend deployment with module-oriented code inside `CampusOps.Core`. Facilities workflows share transactions and evolve together. In-process calls keep failure modes visible to a learner.

Revisit only when a module has independent ownership and release cadence, measured scaling needs, and a clear data boundary that outweighs network and operational costs.
