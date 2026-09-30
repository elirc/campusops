# Hints for stock and ownership

For C01.1, calculate Available from the other two fields after every operation instead of updating it independently in your notes. Consumption subtracts the same amount from OnHand and Reserved. Receipt changes only OnHand. Then ask which method argument identifies the work order whose allocation was consumed; the object-level methods do not contain that information.

For C01.2, begin with Reserved plus quantity less than or equal to OnHand. The availability guard gives you that inequality directly. For Receive, the important test observes state after the rejection, because a guard after mutation can throw the right code while leaving the wrong quantity.

For C01.3, a retrieved Part is detached and the interface offers no general SaveAsync. An authoritative consume command needs to locate the stored aggregate and the owned reservation, validate both, and publish their changes together. Merely mutating the snapshot cannot establish that relationship.

For C02.1, give every object a separate label even when all have the same business ID. MemberwiseClone copies the object but does not recursively clone a future mutable collection. Distinguish the safe current field set from a proposed field that changes the copying obligation.

For C02.2, inspect the factory registrations in Program. Both interfaces resolve the already registered concrete singleton. The test must exercise that composition path or an equivalent configured provider; manually using one new store instance assumes away the defect.

For C02.3, keep both old snapshots after the first successful command. They are observations of earlier state and should remain unchanged. The second command asks the store to evaluate current stock, so it can reject even while the caller's snapshot still displays five available.
