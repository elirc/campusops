# Production-readiness assessment

CampusOps v1.0 is curriculum-complete, not ready for real facilities operations.

It has explicit business rules, contracts, regression tests, SQLite adapter examples, authorization policies, atomic/idempotent seams, health metadata, correlation IDs, dual-stack CI, and operational documentation.

Before production:

- Replace demo authentication and enforce campus scope consistently.
- Select durable adapters for every module; create reviewed migrations and transactional multi-record workflows.
- Implement a durable outbox, webhook signing, SSRF protection, secret vault integration, retry ownership, and idempotency retention.
- Add backups with restore drills, privacy retention, audit access controls, rate/request limits, TLS/security headers, and deployment secret management.
- Define SLOs, metrics, alerts, dashboards, trace export, log redaction, on-call ownership, capacity tests, and disaster recovery.
- Run environment-specific threat modeling, penetration, load, migration, accessibility, and recovery tests.

The readiness endpoint deliberately reports the non-durable default so a demo cannot masquerade as a production configuration.
