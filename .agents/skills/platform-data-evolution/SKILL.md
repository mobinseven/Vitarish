---
name: platform-data-evolution
description: Governs persistence models, database or index schema changes, migrations, backfills, compatibility windows, rollback, and data-loss risk. Use when stored data or persisted formats change. Do not use when a feature explicitly has no persistence impact.
---

# Platform Data Evolution

## Purpose

Evolve stored data safely without creating a second business-rule owner. This skill owns persistence compatibility and migration mechanics, not application behavior.

## Use when

- Database schema, persistence mappings, stored events/messages, cache/index shape, serialized files, or retained configuration formats change.
- Existing data needs migration, backfill, reindexing, or cleanup.

## Do not use

- The change is runtime-only and explicitly persists no new state or format.

## Procedure

1. Identify the authoritative application/domain concept and keep persistence as a derived representation.
2. State schema/format changes, existing-data impact, mixed-version behavior, and whether the change is additive or breaking.
3. Prefer expand -> migrate/backfill -> switch readers/writers -> contract cleanup.
4. Define defaults, nullability, constraints, indexing, concurrency, and failure behavior from actual requirements.
5. Make destructive changes, data loss, irreversible transforms, and rollback limits explicit.
6. Keep migrations mechanical; do not hide business policy in migration code.
7. Define deterministic migration/mapping tests and the required database or index hot scenario.

## Hard stops

- Existing data can be lost or misinterpreted without an explicit decision.
- Old and new application versions cannot coexist during deployment and no deployment strategy is stated.
- Persistence becomes an independent source of business truth.

## Handoff

Route changed behavior to `platform-backend-use-case`, public shape changes to `platform-transport-contract`, and verification to `platform-test-validation`.
