# Owner Medical Import Operations Journey Map

## 1. Context
- Product / service: Vitarish Medical import pipeline
- Journey type: Target-state owner/importer journey
- Entry point: Medical administration > Imports
- User goal: Run deterministic imports from supported adapters and inspect outcomes.
- Business goal: Reliable standard-data ingestion with full traceability.

## 2. Current Implementation Snapshot
| Area | Planned behavior |
| --- | --- |
| Adapters | Source-specific parsing normalized to canonical import commands. |
| Batch tracking | `ImportBatch` stores version, checksum, status, diagnostics. |
| Idempotency | Separate uniqueness constraints for logical version (source_type/version) and file content (checksum). |

## 3. Happy Path
1. Importer selects source type/version and upload/locator.
2. System validates payload and provenance metadata.
3. System runs dry-run or full import.
4. System writes canonical rows linked to `ImportBatch`.
5. Owner reviews counts/warnings and publishes release if required.

## 4. Alternate and Failure Paths
- Validation fails -> `Failed` batch with structured error report.
- Partial FK issues -> `PartiallyImported` with recovery actions.
- Duplicate checksum/version -> import blocked as idempotent duplicate.

## 5. UX and Guardrail Rules
1. Hide source quirks behind adapters.
2. Never patch imported rows in place; issue new release/reimport flow.
3. Keep status transitions explicit (`Pending -> Validating -> Imported|Failed|PartiallyImported`).

## 6. Acceptance Criteria
- Every imported row has `import_batch_id`.
- Duplicate logical imports are blocked.
- Batch diagnostics are visible to owner/importer roles.
