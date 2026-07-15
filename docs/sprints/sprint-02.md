# Sprint 2: asset lifecycle

Assets reference locations and own guarded lifecycle transitions. Compare explicit methods with a public status setter: methods make valid changes discoverable and give invalid changes stable error codes.

The in-memory adapter intentionally stores object references; durable persistence and concurrency arrive after the workflow is understood.
