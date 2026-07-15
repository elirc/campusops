# Lightweight threat model

Assets include campus topology, maintenance history, staff assignments, vendor quotes, approval decisions, webhook endpoints, and secret references.

| Threat | Teaching control | Required production follow-up |
| --- | --- | --- |
| Forged role/campus headers | Explicit warning and policy tests | Validated institutional tokens and issuer/audience checks |
| Cross-campus access | Campus claim comparison | Enforce scope on every campus-owned query and mutation |
| Duplicate external events | Idempotency keys | Durable unique constraint with retention policy |
| Webhook tampering | Secret reference, HTTPS endpoint | HMAC signing, secret vault, rotation, SSRF egress allow-list |
| Stock over-allocation | Atomic adapter operation | Database conditional update inside transaction |
| Lost concurrent edit | Expected work-order version | Durable optimistic concurrency token |
| Sensitive logs | Sanitized audit detail | Structured redaction, access control, retention policy |
| Dependency compromise | .NET and npm CI audits | Automated updates, SBOM, response ownership |

No real student data, payment data, or public deployment is in scope for the teaching release.
