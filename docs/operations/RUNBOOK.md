# CampusOps runbook

## Verify a deployment

1. Record release SHA and UTC time.
2. Check `/health/live` for process liveness.
3. Check `/health/ready`; confirm persistence and worker mode match the environment.
4. Send `X-Correlation-ID` and confirm it is echoed.
5. Verify authenticated location read, work-order list, and operations summary.
6. Verify the worker emits a healthy startup log.

## First response

| Symptom | Check | Safe action |
| --- | --- | --- |
| Data disappears | readiness persistence mode | Stop real use; select durable adapters and restore backup |
| Duplicate webhook | event ID, idempotency key, delivery records | Disable subscription if ongoing; preserve evidence |
| Work edit conflict | expected and current version | Reload; do not overwrite blindly |
| Stock unavailable | on-hand/reserved and owning work orders | Release only an identified reservation |
| 401/403 | token/header, role, campus scope | Correct identity; never weaken policy during incident |
| Preventive jobs absent | worker uptime, UTC clock, plan due date | Run use case once after preserving logs |

Escalate with UTC timestamps, correlation IDs, release SHA, sanitized logs, persistence mode, and actions already attempted.
