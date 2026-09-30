# 1. Who owns the object, and who may change it?

Read [Part.cs](../../src/CampusOps.Core/Inventory/Part.cs), [InMemoryPartsStores.cs](../../src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs), and [PartEndpoints.cs](../../src/CampusOps.Api/Inventory/PartEndpoints.cs). Keep three meanings of ownership separate. Memory ownership asks who can mutate the stored instance. Domain ownership asks which operation controls a stock transition. Access ownership asks which campus or user may act on a resource. A detached snapshot solves only the first.

Prerequisites are C# classes and records, async methods, collection basics, and integer arithmetic. If you have not used locks, first trace one reservation with no competition. The lock is not magic syntax that makes every related domain operation atomic; its protection covers only participating operations over the same shared state.

Draw the initial object graph. A caller constructs Part P. AddAsync stores P.Snapshot, not P. GetAsync returns another snapshot. ListAsync copies every stored part into the returned array. Thus a caller may call Reserve on the value it received without changing inventory storage. The copy is currently sufficient because fields are values or immutable strings. If a future Part contains a mutable list, MemberwiseClone would share that list; "we already copy" would cease to be a complete argument.

The dictionary is private and all reads and mutations use the same gate. Merely returning a read-only collection would not protect its mutable elements. Conversely, an immutable reservation record can safely be shared when its own members are also immutable values. Explain both observations using the actual types rather than treating every collection wrapper as equivalent.

Now follow the service path. PartsService checks cancellation, nonempty IDs, positive quantity and work-order existence before asking the part store to reserve. The store itself rechecks its own input rules and availability. It does not query a work-order store. That separation makes direct adapter tests small, but it also means a fabricated nonempty work-order ID can pass a direct store call. Only the service supplies the existence prerequisite.

Access control is another boundary. The API uses SecurityPolicies.Operations. That policy decides whether a caller can use the endpoint in this demo. Part has no CampusId or owner field, so a role check alone cannot establish tenant isolation. Do not describe the UI's current work-order filter as access enforcement. A future multi-campus feature needs server-side resource ownership decisions tied to verified identity.

Before running anything, write an ownership table for the original Part, a Get result, a List element, and a PartReservation result. For each, state whether changing it can affect stored stock. Then write a second table for callers: direct store code, PartsService, API endpoint and browser. Name which preconditions each supplies and which it assumes.

Your baseline deliverable is a sequence of six observations: copied on insertion, copied on read, duplicate IDs rejected, cancellation checked before writes, stock and history coordinated inside one gate, and no process-restart durability. These are specific promises you can test. "The store is safe" is too broad to reveal a missing assumption or guide a reviewer.

Advance when you can explain how a mutable object can escape through an apparently read-only list, and why fixing that escape does not grant any user permission to reserve the part.
