# Owner Medical Service Valuation Journey Map

## 1. Context
- Product / service: Vitarish Medical service valuation catalog
- Journey type: Target-state owner/importer journey
- Entry point: Medical administration > Service valuations
- User goal: Publish RVU/RBRVS valuation references per year/locality/facility context.
- Business goal: Provide consistent valuation references without implying final prices.

## 2. Current Implementation Snapshot
| Area | Planned behavior |
| --- | --- |
| Data model | `ServiceValuation` keyed by service code + year + locality + facility + effective date. |
| Import | Valuation imports tracked by `ImportBatch` and release/source metadata. |
| Semantics | Values documented as reference valuations, not pricing outputs. |

## 3. Happy Path
1. Importer loads valuation source release.
2. System validates uniqueness dimensions and FK integrity.
3. Owner approves publication.
4. Consumers query valuations by context and date.

## 4. Alternate and Failure Paths
- Duplicate valuation dimension -> blocked by uniqueness constraints.
- Missing service code references -> batch validation error.
- Consumer confuses valuation for price -> UI/API labeling and docs clarify semantics.

## 5. UX and Guardrail Rules
1. Preserve strict terminology: valuation reference only.
2. Keep valuation lifecycle versioned by source release.
3. Separate valuation updates from billing logic.

## 6. Acceptance Criteria
- Context-specific valuation resolution is reliable.
- Uniqueness and FK invariants hold.
- Documentation and API contracts avoid price-language ambiguity.
