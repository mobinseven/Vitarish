# Owner Medical Local Curation Journey Map

## 1. Context
- Product / service: Vitarish Medical administrative curation APIs
- Journey type: Target-state owner/curator journey
- Entry point: Medical administration > Local curation
- User goal: Add or retire local references without altering imported standards.
- Business goal: Support operational flexibility with governance.

## 2. Current Implementation Snapshot
| Area | Planned behavior |
| --- | --- |
| Local origin | Local records marked `record_origin = Local`. |
| Mutation rules | Imported rows are immutable; local updates only on local records. |
| Lifecycle | Local states follow `Draft -> Active -> Retired`. |

## 3. Happy Path
1. Curator creates local medical code, map, drug, or service.
2. System validates uniqueness/effective dating.
3. Curator activates record after review.
4. Read APIs include local overlays where configured.
5. Curator retires obsolete local records.

## 4. Alternate and Failure Paths
- Attempt to edit imported row -> blocked with immutable-data error.
- Overlapping effective dates -> blocked with interval validation error.
- Invalid status transition -> rejected.

## 5. UX and Guardrail Rules
1. Local overlays coexist; they never overwrite imports (uniqueness constraints include record_origin to prevent conflicts).
2. Curation commands are auditable and role-protected.
3. Keep curation UI focused on local origin decisions and lifecycle.

## 6. Acceptance Criteria
- Local create/update/retire commands work for curator role.
- Imported rows remain immutable.
- Audit history captures all state changes.
