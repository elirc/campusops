# Curriculum retrospective

CampusOps begins with one vertical slice and introduces complexity only after a visible need: SQLite for persistence, a worker for scheduled work, `IClock` for deterministic time, policies for permissions, React for an actual consumer, and idempotency/concurrency controls for reproduced integration failures.

A learner finishing the course should be able to trace requests across layers, model state and policy, choose an appropriate test boundary, publish compatible contracts, diagnose CI, review security and operational gaps, and argue against speculative architecture with concrete evidence.

Suggested capstone: choose one production-readiness gap—durable outbox, token validation, or transactional work-order persistence—write an ADR, implement one tested vertical slice, and update the runbook.
