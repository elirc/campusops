# Pre-change snapshots

These are byte copies of the six files this delivery changed, exactly as they were **before** the repair. They exist so you can reproduce the failing state yourself instead of taking chapter 03's word for it — see Exercise 0 in [05-PRACTICE.md](../05-PRACTICE.md).

| Snapshot | Live file it corresponds to |
| --- | --- |
| `Part.before.cs.txt` | `src/CampusOps.Core/Inventory/Part.cs` |
| `InMemoryPartsStores.before.cs.txt` | `src/CampusOps.Infrastructure/Inventory/InMemoryPartsStores.cs` |
| `PartEndpoints.before.cs.txt` | `src/CampusOps.Api/Inventory/PartEndpoints.cs` |
| `Program.before.cs.txt` | `src/CampusOps.Api/Program.cs` |
| `ConcurrentReservationTests.before.cs.txt` | `tests/CampusOps.Infrastructure.Tests/Inventory/ConcurrentReservationTests.cs` |
| `vite.config.before.ts.txt` | `web/CampusOps.Web/vite.config.ts` |

They are `.txt` on purpose: a stray `.cs` file inside a project directory would be compiled.

**Read the before/after of the store first.** The old `InMemoryPartsStores.cs` is two classes — `InMemoryPartStore : IPartStore` over a `ConcurrentDictionary` and a separate `InMemoryPartReservationStore : IPartReservationStore` over a `ConcurrentQueue`. That split is the bug: the two writes could not be made atomic because they lived in two objects. It is also why a blind copy of the snapshot over the live file does not compile — the new tests call `ReserveForWorkOrderAsync`, which the old shape does not have. Exercise 0 tells you which three lines to revert instead, so you get the old *behaviour* with the new *shape* and can watch the tests fail for behavioural reasons rather than compiler reasons.
