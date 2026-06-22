# Owner Medical Tenant Preferences Journey Map

## 1. Context
- Product / service: Vitarish Medical tenant-level reference preferences
- Journey type: Target-state owner journey
- Entry point: Tenant settings > Medical references
- User goal: Set tenant release preferences and visibility flags without duplicating canonical data.
- Business goal: Provide tenant configurability while preserving global reference integrity.

## 2. Current Implementation Snapshot
| Area | Planned behavior |
| --- | --- |
| Tenant preferences | Optional pointers to preferred release per code system. |
| Visibility | Optional enable/disable flags for local overlays. |
| Data boundary | Tenant tables store configuration only; no imported-row duplication. |

## 3. Happy Path
1. Owner opens tenant medical reference settings.
2. Owner chooses preferred release per relevant code system.
3. Owner configures local visibility flags.
4. System saves preferences and applies them in read resolution.

## 4. Alternate and Failure Paths
- Preferred release inactive/unavailable -> validation blocks save.
- Owner lacks permission -> settings read-only/blocked.
- Conflicting preferences -> system falls back to global active release with warning.

## 5. UX and Guardrail Rules
1. Keep one source of truth in canonical tables.
2. Tenant settings may reference, never rewrite, canonical entities.
3. Show effective configuration provenance (global default vs tenant override).

## 6. Acceptance Criteria
- Tenant preference writes are isolated to tenant config tables.
- Read resolution honors tenant preferences when valid.
- Canonical imported rows remain unchanged.
