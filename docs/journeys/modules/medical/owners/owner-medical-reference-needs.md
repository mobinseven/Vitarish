# Owner Medical Reference Needs Map

## 1. Context

- Product / service: Vitarish Medical module owner experience
- Journey type: Implied-needs map derived from Medical initial implementation plan
- Entry point: Owner or platform reference admin opens Medical module setup and operations surfaces
- Scenario: An owner/admin must publish trustworthy medical reference data so downstream products can search and resolve codes safely.
- User goal: Keep medical references accurate, versioned, searchable, and operationally maintainable without mixing patient workflows.
- Business goal: Establish a stable source of truth that can be reused by clinical, reservation, billing, and insurance modules.
- Scope start: Owner decides to onboard and operate Medical reference data.
- Scope end: Owner can import, activate, query, curate, and audit reference data with safe guardrails.
- Primary persona: Platform owner / reference admin
- Secondary personas: Reference importer, reference curator, read-only consumer module owners
- Evidence sources: `docs/plans/Medical-Implementation-Plan.md`
- Confidence level: Medium-high for feature boundaries and lifecycle; medium for final UX sequencing.

## 2. Implied User Needs

| Need ID | Implied user need | Plan evidence | Owner outcome |
| --- | --- | --- | --- |
| N1 | Register and manage code systems and immutable releases. | Code System Registry + Phase 1 deliverables. | Reliable terminology baseline per release. |
| N2 | Import external standards with traceability, idempotency, and failure visibility. | Import Pipeline + ImportBatch constraints + adapter contract. | Repeatable imports and easier incident recovery. |
| N3 | Resolve drugs/services/codes quickly through read-only APIs. | Query model + Search API + Phase 6. | Downstream modules can trust lookups. |
| N4 | Maintain local overlays without mutating imported records. | Data separation model + local curation commands. | Safe local flexibility without corrupting standards. |
| N5 | Manage effective dating, lifecycle states, and active releases. | Status/lifecycle + invariants + activation commands. | Historical accuracy and controlled go-live behavior. |
| N6 | Understand valuation references without treating RVU as prices. | Service valuation scope + validation rule #8 + Phase 4 criteria. | Prevent billing misuse and semantic drift. |
| N7 | Enforce role-based governance and auditability. | Roles section + command model + audit guardrails. | Compliant and attributable changes. |
| N8 | Configure minimal tenant preferences without duplicating canonical data. | Optional tenant configuration model. | Tenant control with one shared source of truth. |

## 3. Journey Portfolio Derived From Needs

1. Owner establishes code system registry and release lifecycle (N1, N5).
2. Owner/importer executes import operations and handles failures (N2, N5, N7).
3. Owner enables reference search/read services for consuming modules (N3).
4. Owner curates local records and mappings with governance (N4, N7).
5. Owner activates service valuation references and communicates semantics (N6).
6. Owner applies tenant-level reference preferences safely (N8).

## 4. Success Criteria Across Medical Owner Journeys

- Every imported record is traceable to an `ImportBatch`.
- Imported records are immutable after success.
- Read/query paths remain separated from write/import operations.
- Release activation/deactivation is explicit and auditable.
- Local curation never overwrites imported rows.
- Search/rebuild operations are operationally visible and repeatable.
- Tenant-level preferences do not duplicate canonical tables.

## 5. Related Journey Maps

- `owner-medical-code-system-registry-journey.md`
- `owner-medical-import-operations-journey.md`
- `owner-medical-reference-query-journey.md`
- `owner-medical-local-curation-journey.md`
- `owner-medical-service-valuation-journey.md`
- `owner-medical-tenant-preferences-journey.md`
