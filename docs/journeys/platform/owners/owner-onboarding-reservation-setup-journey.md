# Owner Onboarding and Reservation Setup Journey Map

## 1. Context

- Product / service: Vitarish platform owner onboarding for reservations
- Journey type: Target-state prerequisite setup flow
- Entry point: Authenticated owner opens **Reservation Setup** from onboarding checklist
- Scenario: A new owner configures the minimum reservation settings required before booking can be enabled for the tenant.
- User goal: Complete required reservation configuration once, with clear progress and safe validation.
- Business goal: Prevent invalid or partial booking configurations while reducing support hand-holding.
- Scope start: Owner enters Reservation Setup with tenant context resolved.
- Scope end: System marks reservation booking as enabled only after all required setup steps are valid and saved.
- Primary persona: Tenant owner / primary administrator
- Friction principle: Collect only minimum booking prerequisites before enabling reservations.

## 2. Minimum Required Configuration

Booking enablement depends on one source of truth: a single reservation readiness model.

| Requirement | Why required | Minimum completion rule |
| --- | --- | --- |
| Reservation policy | Defines lead time, booking window, and cancellation behavior | All required policy fields pass validation and are saved. |
| Operating schedule | Defines when reservations can occur | At least one active schedule block exists and is valid. |
| Capacity definition | Prevents overbooking | At least one capacity rule exists with positive limits. |
| Assignment defaults | Ensures each reservation can be routed | Default assignment path (provider/room/team) is configured. |
| Confirmation behavior | Sets communication/confirmation baseline | At least one confirmation mode is selected and testable. |

## 3. Journey Summary

**Narrative:** The owner completes a guided reservation setup checklist. Each step validates required fields, persists independently, and updates a shared readiness status. Booking stays disabled until all required prerequisites are complete.

**Emotional arc:** Uncertain -> guided -> confident -> ready to accept bookings

**Biggest friction point addressed:** Owners often do not know which reservation settings are truly mandatory; this journey turns that into a concrete, sequential checklist with live readiness feedback.

**State model rule:** Reservation readiness is computed from a single canonical setup model, not from duplicated UI-specific flags.

## 4. Journey Stages

| Stage | Owner action | System response | Exit criteria |
| --- | --- | --- | --- |
| Access setup | Open reservation setup from onboarding | Load current readiness snapshot and incomplete requirements | Setup page is loaded with current status |
| Configure policy | Enter booking window, lead time, cancellation constraints | Validate values and save draft/persisted policy | Policy requirement marked complete |
| Configure schedule | Add at least one active availability block | Validate conflicts/invalid ranges and save | Schedule requirement marked complete |
| Configure capacity | Define reservation capacity limits | Validate numeric bounds and save | Capacity requirement marked complete |
| Configure assignments | Select default assignment routing | Validate required target values and save | Assignment requirement marked complete |
| Configure confirmation | Choose confirmation mode(s) and test path | Validate at least one active mode and save | Confirmation requirement marked complete |
| Enable booking | Click **Enable Booking** | Recompute readiness from source of truth; enable only when complete | Booking enabled status becomes active |

## 5. Happy Path

1. Owner opens Reservation Setup.
2. System loads a readiness checklist with current status for each requirement.
3. Owner configures reservation policy and saves.
4. System validates and marks policy complete.
5. Owner configures operating schedule and saves.
6. System validates and marks schedule complete.
7. Owner configures capacity and saves.
8. System validates and marks capacity complete.
9. Owner configures assignment defaults and saves.
10. System validates and marks assignment complete.
11. Owner configures confirmation behavior and saves.
12. System validates and marks confirmation complete.
13. Owner selects **Enable Booking**.
14. System recomputes readiness from canonical setup state.
15. System enables booking and records an auditable status change.

## 6. Validation and Guardrails

| Guardrail | Rule |
| --- | --- |
| Required-first flow | Booking cannot be enabled while any mandatory setup section is incomplete. |
| Safe numeric bounds | Lead time, booking window, and capacity values must be positive and within allowed bounds. |
| Time consistency | Schedule blocks must have valid ranges and no invalid overlaps in the same context. |
| Assignment safety | At least one default assignment target must exist before enablement. |
| Confirmation safety | At least one confirmation path must be active before enablement. |
| Revalidation on enable | Final enable action always recalculates readiness from persisted state. |

## 7. Failure and Recovery Rules

| Failure point | Current expected behavior | Recovery path |
| --- | --- | --- |
| Invalid input on a section | Show section-level and field-level validation errors | Owner corrects fields and resubmits section |
| Save failure for a section | Keep previous persisted state; show non-destructive error message | Owner retries save without losing unchanged completed sections |
| Readiness mismatch at enable time | Enable request is blocked with missing requirement list | Owner navigates directly to incomplete requirement(s) |
| Concurrent configuration change | Source-of-truth version conflict is detected | Owner reloads latest state and reapplies intended changes |

## 8. UX Simplicity Rules

1. **One checklist, one readiness state:** Use one visible checklist tied to one canonical readiness model.
2. **Separate write from read:** Section edits are explicit save actions; readiness display is read-only status.
3. **Progressive disclosure:** Show advanced options only after minimum required fields are complete.
4. **Direct recovery:** Every failure points to the exact field or section that must be fixed.
5. **No silent enablement:** Booking enablement is always a deliberate explicit action.

## 9. Acceptance Criteria

- Reservation setup exposes a required checklist for policy, schedule, capacity, assignments, and confirmation.
- Each required section validates input before it can be marked complete.
- Booking remains disabled until all required sections are complete in persisted state.
- Enablement action recomputes readiness from one canonical data model.
- Validation and save failures are recoverable without data loss in other completed sections.
- Successful enablement records an auditable state change.

## 10. Future Iteration Opportunities

- Add guided defaults by tenant type to prefill minimum reservation settings.
- Add inline simulation to preview the first available booking slot before enablement.
- Add policy templates for common operating models.
- Add dependency hints when one setting choice affects another requirement.
- Add onboarding telemetry for section completion times and drop-off points.
