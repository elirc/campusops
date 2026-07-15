# API integration guide

Base path: `/api/v1`. JSON is default; business failures use Problem Details with a stable `code` extension.

- Ignore additive response fields and handle unknown future enum values.
- Send a unique `X-Correlation-ID` of at most 100 characters and record the echoed value.
- Work-order commands require the version last read; reload after `work_order.version_conflict`.
- Webhook event IDs are idempotency keys. Reusing one intentionally produces no new delivery.
- Webhook endpoints must be HTTPS. Secrets are referenced by name and resolved by deployment infrastructure.
- Do not blindly retry other mutations after an ambiguous timeout; general idempotency keys are not implemented.

Development OpenAPI is available at `/openapi/v1.json`. Demo identity headers provide no security assurance.
