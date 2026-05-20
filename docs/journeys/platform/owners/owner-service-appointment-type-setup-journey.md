# Service / Appointment Type Setup Journey Map

## 1. Context

- Product / service: Vitarish platform owner configuration for services and appointment types
- Journey type: Target-state foundational setup flow
- Entry point: Authenticated owner opens **Service Setup** from onboarding or settings
- Scenario: The owner defines bookable services (appointment types) so reservations can reference a valid service entity.
- User goal: Create and publish clear appointment types quickly, with safe validation and predictable booking behavior.
- Business goal: Introduce a canonical Service entity to reduce booking errors, improve reporting consistency, and support future pricing/rules expansion.
- Scope start: Owner enters Service Setup with tenant context resolved.
- Scope end: At least one active, valid service is published and available to reservation flows.
- Primary persona: Tenant owner / primary administrator
- Friction principle: Collect only the minimum service fields needed to make a service bookable.

## 2. Service Entity Definition (New Platform Capability)

The platform currently lacks a first-class Service entity. This journey defines it as the single source of truth for what can be booked.

| Field group | Required | Purpose | Minimum rule |
| --- | --- | --- | --- |
| Service identity (name, internal key) | Yes | Uniquely identifies appointment type | Name is non-empty; key is auto-generated and unique within tenant. |
| Duration and timing (duration, buffer) | Yes | Determines slot calculation and schedule fit | Duration is positive; buffer is zero or positive. |
| Booking visibility (active/inactive, display name) | Yes | Controls whether customers can see/book service | Service can only be published when active + valid. |
| Assignment defaults (provider/team/resource requirement) | Yes | Ensures reservations can be routed | At least one valid assignment path is configured. |
| Pricing baseline (price amount/currency or "contact us") | Yes | Sets customer expectation and downstream billing hook | One pricing mode is selected and valid. |
| Description and preparation notes | No | Improves customer understanding | Optional; if present, must respect length/content limits. |
| Constraints (lead time, max advance window, cancellation policy override) | No (recommended) | Allows service-specific booking behavior | If set, constraints must pass policy bounds validation. |

## 3. Journey Summary

**Narrative:** The owner creates one or more services through a guided setup form. Each service is validated and saved as a canonical entity. Only valid active services appear in reservation selection.

**Emotional arc:** Ambiguous -> structured -> confident -> publish-ready

**Biggest friction point addressed:** Owners currently cannot define appointment types as a dedicated entity; this journey introduces a simple, explicit service model with clear publish rules.

**State model rule:** Reservation service selection must read from one canonical Service collection per tenant, not ad-hoc labels embedded in reservation forms.

## 4. Journey Stages

| Stage | Owner action | System response | Exit criteria |
| --- | --- | --- | --- |
| Access setup | Open Service Setup | Load existing services and setup guidance | Service management view is loaded |
| Create draft service | Click **Create Service** and enter required fields | Initialize draft, validate required fields inline | Draft passes minimum validation |
| Configure booking behavior | Set duration, buffer, assignment defaults, pricing mode | Validate numeric/routing/pricing constraints | Booking behavior is valid and saved |
| Save service | Click **Save Draft** or **Save and Publish** | Persist canonical Service entity with audit metadata | Service record exists with version |
| Publish service | Toggle active/published status | Revalidate required fields from persisted state before publish | Service status becomes active/published |
| Verify in reservations | Open reservation creation flow | Show only active valid services in selection list | Service selectable for booking |

## 5. Happy Path

1. Owner opens Service Setup.
2. System shows existing services and a primary action to create a new service.
3. Owner enters service name and required booking duration.
4. System validates required identity and duration rules.
5. Owner configures buffer, assignment defaults, and pricing mode.
6. System validates numeric bounds and assignment completeness.
7. Owner saves service as draft.
8. System persists the service and records audit metadata.
9. Owner reviews the summary and clicks **Publish Service**.
10. System revalidates persisted service state and marks it active.
11. Reservation flow reads active services and displays the newly published service.

## 6. Validation and Guardrails

| Guardrail | Rule |
| --- | --- |
| Tenant uniqueness | Service internal key must be unique per tenant. |
| Required core fields | Name, duration, assignment path, and pricing mode are mandatory before publish. |
| Safe timing bounds | Duration must be > 0; buffer must be >= 0 and within configured max. |
| Assignment integrity | Publish blocked if no valid provider/team/resource routing is configured. |
| Pricing integrity | Exactly one baseline pricing mode must be valid at a time. |
| Status gate | Inactive or invalid services cannot appear in reservation service selectors. |
| Final publish recheck | Publish action recalculates validity from persisted data, not unsaved UI state. |

## 7. Failure and Recovery Rules

| Failure point | Current expected behavior | Recovery path |
| --- | --- | --- |
| Duplicate service key | Save blocked with explicit uniqueness error | Owner edits key and retries save |
| Invalid duration/buffer | Field-level validation with actionable message | Owner corrects value and resubmits |
| Missing assignment defaults | Publish blocked with missing routing details | Owner configures assignment and retries |
| Pricing mode misconfiguration | Save/publish blocked with pricing rule errors | Owner selects valid mode and retries |
| Persistence conflict | Version conflict warning on stale edits | Owner reloads latest version, reapplies intended changes |
| Downstream reservation cache stale | Newly published service not visible immediately | System invalidates selector cache to ensure immediate visibility |

## 8. UX Simplicity Rules

1. **One service editor, one source of truth:** All appointment-type setup writes to the same canonical Service model.
2. **Clear draft vs publish states:** Draft is editable; publish is explicit and validated.
3. **Required fields first:** Show mandatory fields before optional advanced constraints.
4. **Actionable errors only:** Every validation error states exactly what to fix.
5. **Reservation-safe defaults:** Unpublished/invalid services are automatically excluded from booking selection.

## 9. Acceptance Criteria

- Platform provides a first-class Service entity usable as appointment type.
- Owner can create, edit, save draft, and publish/unpublish services.
- Service publish requires valid identity, timing, assignment, and pricing baseline.
- Reservation flows list only active valid services from canonical service data.
- Validation, save, and publish failures are recoverable without corrupting existing services.
- Service lifecycle changes are auditable (created/updated/published/unpublished metadata).

## 10. Future Iteration Opportunities

- Add service templates (e.g., consultation, follow-up, group session).
- Support service variants (same service, multiple durations).
- Add channel-specific visibility (web/app/partner).
- Add localized service names/descriptions.
- Add dependency hints between service constraints and reservation policy defaults.
