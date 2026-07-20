---
name: platform-data-evolution
description: Owns persisted models/formats, schema changes, migrations, backfills, compatibility windows, rollback, and data-loss risk. Skip changes with no persistence impact.
---

# Platform Data Evolution

## Rules

1. Keep persistence derived from the authoritative Domain/Application concept.
2. State schema/format changes, existing-data impact, mixed-version behavior, and additive/breaking status.
3. Prefer expand → migrate/backfill → switch readers/writers → contract cleanup.
4. Define required defaults, nullability, constraints, indexes, concurrency, and failure behavior.
5. Make destructive changes, irreversible transforms, data loss, and rollback limits explicit.
6. Keep migrations mechanical; never embed business policy.
7. Define deterministic mapping/migration tests and required database/index hot scenarios.

## Stop

- Existing data may be lost or misread without approval.
- Mixed-version deployment is unsafe and no strategy exists.
- Persistence becomes an independent business-rule owner.
