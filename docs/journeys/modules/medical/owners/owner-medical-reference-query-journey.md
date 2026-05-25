# Owner Medical Reference Query Enablement Journey Map

## 1. Context
- Product / service: Vitarish Medical read/search APIs
- Journey type: Target-state owner journey
- Entry point: Medical administration > Search/read services
- User goal: Ensure consumer modules can search and resolve references fast and safely.
- Business goal: Consistent read behavior across platform modules.

## 2. Current Implementation Snapshot
| Area | Planned behavior |
| --- | --- |
| Query APIs | Drug, service, terminology, mapping, and import-status lookups. |
| Search model | Denormalized projections rebuildable from canonical data. |
| Query filters | As-of date, active-only, release-specific, exact + typeahead. |

## 3. Happy Path
1. Owner enables read APIs for approved consumer roles/modules.
2. Owner validates core queries (`ResolveMedicalCode`, `SearchDrugs`, `ResolveServiceCode`).
3. Owner schedules/rebuilds search index when needed.
4. Consumer modules use read APIs without write coupling.

## 4. Alternate and Failure Paths
- Stale projection detected -> run `RebuildReferenceSearchIndex`.
- Invalid release filters -> return validation error with supported release options.
- Unauthorized access -> read denied by role policies.

## 5. UX and Guardrail Rules
1. Keep query services read-only.
2. Maintain separation from import/write handlers.
3. Ensure projections are disposable and rebuildable.

## 6. Acceptance Criteria
- Core lookup and typeahead queries return release-aware results.
- Search rebuild can recover query freshness.
- Query surface stays independent from write-side workflows.
