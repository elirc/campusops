# CampusOps curriculum specification

CampusOps manages university locations, assets, maintenance requests, work orders, technicians, parts, preventive maintenance, vendors, procurement, notifications, and reporting.

| Sprint | Outcome | Primary skill |
| --- | --- | --- |
| 01 | Locations and API foundation | entities, contracts, HTTP |
| 02 | Asset lifecycle | invariants and state |
| 03 | Maintenance intake and triage | validation and prioritization |
| 04 | Work-order workflow | state machines |
| 05 | Technician skills and assignment | policies and relationships |
| 06 | Comments and audit history | append-only facts |
| 07 | SQLite persistence | EF Core and migrations |
| 08 | Parts and stock reservations | transactions and quantities |
| 09 | Preventive maintenance | clocks and background work |
| 10 | SLA clocks and escalation | configurable policy |
| 11 | Vendors and purchase requests | approval thresholds |
| 12 | Roles and campus permissions | authentication and authorization |
| 13 | React operations dashboard | TypeScript contracts and UI state |
| 14 | Webhooks and bug harvest | idempotency and concurrency |
| 15 | Reporting and operations | observability and release judgment |

Constraints: one backend deployment, explicit modules, stable v1 contracts, UTC timestamps, no real student data, and no claim of production readiness.
