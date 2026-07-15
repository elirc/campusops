# Sprint 12: roles and campus permissions

Authentication establishes identity; authorization decides permission. Tests distinguish 401 from 403, and location creation checks campus scope in addition to role.

`X-Demo-Role` and `X-Demo-Campus` are deliberately forgeable teaching headers. Replace the handler with validated institutional tokens before deployment; keep policies and scope checks.
