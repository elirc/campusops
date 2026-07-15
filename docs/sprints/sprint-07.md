# Sprint 7: SQLite persistence

EF Core types remain in Infrastructure. Row models express the schema while domain objects keep lifecycle rules. The integration test uses a real in-memory SQLite connection and a fresh context, proving more than a mocked database call can.

The SQL migration is explicit for teaching. Production deployment would generate, review, apply, and roll back versioned EF migrations through a controlled release process.
