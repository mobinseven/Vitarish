# Provider Availability Management Journey Map

## 1. Context

- Product / service: Vitarish platform owner scheduling configuration
- Journey type: Target-state prerequisite setup flow
- Entry point: Authenticated owner opens **Provider Availability** setup from owner scheduling/reservations settings
- Scenario: The owner configures the minimum reservation settings that must exist before booking can be enabled.
- User goal: Define provider-level availability readiness in a guided way without enabling booking too early.
- Business goal: Ensure booking enablement is blocked until provider availability prerequisites are valid, persisted, and auditable.
- Scope start: Owner enters provider availability setup with tenant context resolved.
- Scope end: Booking eligibility is set to enabled only when provider availability readiness is complete.
- Primary persona: Tenant owner / scheduling administrator
- Friction principle: Require only the minimum provider and availability inputs needed for safe booking enablement.

## 2. Domain Definitions and Boundaries

| Term | Definition | Design boundary |
| --- | --- | --- |
| Provider | A schedulable service subject (person or equivalent role) that can receive reservations. | New domain entity owned by reservation scheduling context. |
| Availability | Existing shift definitions that represent bookable time windows. | Existing shift model remains source of truth for time windows. |
| Provider availability readiness | Computed state showing whether minimum provider and shift constraints are complete for enablement. | One canonical read model; no duplicated UI-specific readiness flags. |

### Provider entity (new)

Minimum provider fields required by this journey:

- Provider identifier (stable ID)
- Display name
- Active/inactive status
- Booking participation flag (eligible for assignment)
- Optional default capacity override (if provider-specific capacity is enabled)

Design rules:

- Provider lifecycle is managed separately from reservation creation actions.
- Provider writes (create/update/activate/deactivate) are state-changing commands.
- Provider reads (list/search/detail/readiness contribution) are read-only queries.

## 3. Minimum Required Configuration Before Enablement

Booking enablement depends on one source of truth: a single provider availability readiness model.

| Requirement | Why required | Minimum completion rule |
| --- | --- | --- |
| Provider roster | Reservations need at least one assignable provider. | At least one active provider with booking participation enabled exists. |
| Provider-shift mapping | Availability must be attributable to one or more providers. | At least one active future shift is linked to at least one active provider. |
| Reservation policy baseline | Provider availability must follow booking constraints. | Policy required fields (lead time/booking window/cancellation) are valid and persisted. |
| Capacity baseline | Provider-linked shifts need safe capacity bounds. | Capacity rules are valid with positive limits for all provider-bookable shifts. |
| Readiness confirmation | Enablement must be explicit and safe. | Final enable action passes full readiness recomputation from persisted state. |

## 4. Journey Summary

**Narrative:** The owner creates or activates providers, links them to existing shifts, confirms booking policy and capacity prerequisites, then explicitly enables booking. Each step saves independently and contributes to one shared readiness state.

**Emotional arc:** Ambiguous model -> structured setup -> validated control -> confident enablement

**Biggest friction point addressed:** The platform currently has shifts but lacks a provider entity; this journey introduces the minimum provider model needed to make shift-based booking assignment safe and understandable.

**State model rule:** Provider readiness and booking enablement are computed from one canonical availability readiness model, not from ad hoc component states.

## 5. Journey Stages

| Stage | Owner action | System response | Exit criteria |
| --- | --- | --- | --- |
| Access setup | Open provider availability management | Load readiness snapshot and missing requirements | Setup view loaded with checklist state |
| Define providers | Create provider records and set active/booking participation | Validate required fields and persist provider entities | Provider roster requirement complete |
| Link providers to shifts | Associate active providers to future shifts | Validate assignment integrity and persisted links | Provider-shift mapping requirement complete |
| Validate policy baseline | Review and save reservation policy prerequisites | Validate numeric and rule bounds | Policy baseline requirement complete |
| Validate capacity baseline | Review and save capacity prerequisites on bookable shifts | Validate positive limits and safe upper bounds | Capacity baseline requirement complete |
| Run readiness check | Trigger full readiness recompute | Rebuild checklist from persisted source-of-truth state | All mandatory requirements complete |
| Enable booking | Click **Enable Booking** | Revalidate and write auditable booking-enabled state | Booking enablement active |

## 6. Happy Path

1. Owner opens Provider Availability Management.
2. System loads checklist status from the canonical readiness query.
3. Owner creates at least one provider and marks it active and bookable.
4. System validates provider data and marks provider roster complete.
5. Owner links provider(s) to one or more active future shifts.
6. System validates links and marks provider-shift mapping complete.
7. Owner confirms reservation policy baseline is valid and saved.
8. System marks policy baseline complete.
9. Owner confirms capacity baseline is valid and saved.
10. System marks capacity baseline complete.
11. Owner selects **Run Readiness Check**.
12. System recomputes readiness from persisted state only.
13. Owner selects **Enable Booking**.
14. System revalidates, enables booking, and records an auditable state transition.

## 7. Validation and Guardrails

| Guardrail | Rule |
| --- | --- |
| Provider presence | Booking cannot be enabled with zero active bookable providers. |
| Shift linkage integrity | Bookable shifts must resolve to at least one active provider. |
| Status safety | Inactive providers cannot satisfy readiness or receive new bookings. |
| Policy/capacity safety | Lead time, booking window, and capacity bounds must stay valid at enablement time. |
| Final revalidation | Enable action always recomputes readiness from persisted source of truth. |
| Command/query separation | Readiness query cannot mutate provider, shift, or policy state. |

## 8. Failure and Recovery Rules

| Failure point | Current expected behavior | Recovery path |
| --- | --- | --- |
| Invalid provider input | Field-level and section-level validation shown | Owner corrects provider data and resubmits |
| Provider-shift link conflict | Link action blocked with conflict explanation | Owner updates shift/provider status or selects valid mapping |
| Policy/capacity invalidation | Readiness check returns explicit failed requirements | Owner navigates to failing section and fixes prerequisites |
| Save failure | Previously persisted state remains unchanged | Owner retries failed command; completed sections remain complete |
| Concurrent update/version conflict | Conflict detected against canonical model version | Owner refreshes and reapplies intended updates |

## 9. UX Simplicity Rules

1. **One checklist, one readiness state:** A single readiness checklist is the only enablement truth.
2. **Simple provider model first:** Start with minimal provider fields; defer advanced profile data.
3. **Explicit writes:** Provider create/update/link/unlink are explicit save actions.
4. **Read clarity:** Readiness and diagnostics are query-only, never side-effecting.
5. **No hidden enablement:** Booking enablement is always deliberate and auditable.

## 10. Acceptance Criteria

- A provider entity exists with minimum required fields for booking participation.
- Owners can create/activate providers and link them to active future shifts.
- Provider availability readiness shows explicit status for provider roster, provider-shift mapping, policy baseline, and capacity baseline.
- Booking remains disabled until all mandatory readiness requirements are complete in persisted state.
- Enable action recomputes readiness from one canonical model before writing enabled status.
- Validation, save, and conflict failures are recoverable without corrupting completed sections.
- Successful booking enablement writes an auditable state change event.

## 11. Extensibility Notes

- Add provider specialization/skills without rewriting readiness core by extending provider descriptors.
- Add room/resource co-assignment later as an additive readiness requirement flag.
- Add provider time-off/blackout overlays as a read-model extension over shift availability.
- Preserve one canonical readiness aggregator so new requirements are additive, not duplicative.
