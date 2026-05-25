# Owner Medical Code System Registry Journey Map

## 1. Context
- Product / service: Vitarish Medical module
- Journey type: Target-state owner journey
- Entry point: Medical administration > Code systems
- User goal: Register code systems, publish releases, and activate the correct release safely.
- Business goal: Keep one trusted source of truth per code system release.

## 2. Current Implementation Snapshot
| Area | Planned behavior |
| --- | --- |
| Registry | Register `CodeSystem` and `CodeSystemRelease` with uniqueness constraints. |
| Activation | Activate/deactivate releases with policy checks. |
| Invariants | Release uniqueness and non-overlapping effective date intervals enforced. |

## 3. Happy Path
1. Owner opens code system registry.
2. Owner creates a code system (`system_key`, category, authority).
3. Owner registers a release with source metadata and effective dates.
4. Owner validates and activates the release.
5. Downstream modules resolve active release for lookups.

## 4. Alternate and Failure Paths
- Duplicate `system_key` or release name is blocked with actionable validation.
- Invalid effective ranges are rejected.
- Unauthorized users cannot activate releases.

## 5. UX and Guardrail Rules
1. Keep one responsibility: registry owns system/release lifecycle only.
2. Separate write commands from lookup queries.
3. Require explicit activation; do not auto-activate on create.
4. Keep imported release rows immutable post-import.

## 6. Acceptance Criteria
- Unique system and release constraints enforced.
- Activation/deactivation is auditable.
- Active release resolution works for query consumers.
