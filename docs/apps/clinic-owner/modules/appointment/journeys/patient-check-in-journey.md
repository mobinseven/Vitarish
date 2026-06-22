# Patient Check-In Journey

## 1. Context
- Product / service: Vitarish Clinic app (owner experience) + appointments/reservations services
- Journey type: Implemented guided journey with known extension points
- Scenario: Clinic owner or front-desk staff checks in a patient.
- User goal: Identify an existing patient or create a new patient, then book the best available shift slot and complete handoff.
- Business goal: Move intake quickly while preventing duplicate patients and respecting reservation rules.
- Scope start: Staff opens Clinic Check-In.
- Scope end: Patient is either checked in for today or booked into a future shift, and confirmation is shown.
- Primary persona: Clinic receptionist / clinic owner delegate
- Secondary personas: Patient, scheduler
- Evidence sources: Implemented owner route `/owner/clinic/check-in`, host page orchestration, check-in UI context, shift booking step, confirmation step.
- Confidence level: High for implemented behavior.

## 2. Persona Snapshot
**Name / segment:** Front-desk clinic operator

**Situation:** Handling arrivals and near-term scheduling while minimizing desk-side delays.

**Motivation:** Finish intake in one guided flow without creating patient duplicates or promising unavailable capacity.

**Key needs:** Fast identification lookup, safe create flow with duplicate hints, clear slot choices, and explicit final status.

**Constraints:** Capacity can change between UI display and reservation command; patient identity data may be incomplete at arrival.

**Success means:** One correct patient is selected/created and one reservation outcome is confirmed.

## 3. Journey Summary
**Narrative:** The user enters the owner check-in route, starts in **Identify**, uses `PatientFindOrCreate` to find by identification code or advanced search, and optionally creates a patient with prefilled values plus duplicate-candidate warnings. After patient resolution, the flow moves to **Book**, where bookable slots are shown and a reservation is attempted. If the selected slot is today, the system can also mark arrival (confirm + present). The flow ends on **Confirm**, where the outcome is shown as **Current Shift** or **Future Shift**, including reservation status and direct navigation back to the patient profile.

**Emotional arc:** Start: focused → Middle: cautious during patient resolution and slot choice → End: confident after explicit confirmation.

**Biggest friction point:** Correct patient resolution under incomplete identity data.

**Highest-value opportunity:** Keep the simple 3-step flow while tightening capacity messaging and duplicate safeguards.

## 4. Journey Stages

### Stage 1: Identify patient
**User goal:** Resolve exactly one patient record.

**User actions:**
- Open `/owner/clinic/check-in`.
- Use identification lookup and/or advanced search.
- Select an existing patient, or choose Create New Patient when lookup/search conditions allow it.
- Review duplicate candidates and either select a candidate or proceed with create form.

**Touchpoints / channels:** Owner dashboard entry, `ClinicOwnerPatientCheckInPage`, `PatientFindOrCreate`, `PatientAdvancedSearch`, patient create form.

**User thoughts / questions:** “Is this the correct patient, or do I need to create a new one?”

**Emotions:** Cautious, because duplicate creation has downstream cost.

**Pain points:** Similar names, missing identifiers, and uncertain contact details.

**Needs:** One source of truth for patient selection, duplicate visibility, and safe create prefill.

**Success metrics:** Time-to-selection, duplicate creation rate, create-form cancellation rate.

### Stage 2: Book slot
**User goal:** Reserve an appropriate shift slot.

**User actions:**
- Review available slots from active shifts in a bounded date range (today through 30 days).
- Select a slot.
- Reservation command executes with server-side validation.
- For same-day slots when `MarkAsArrived` is true, system attempts confirm + present after reserve.

**Touchpoints / channels:** `PatientCheckInShiftStep`, slot calendar, booking status message.

**User thoughts / questions:** “Can this patient be scheduled now, and did the booking actually stick?”

**Emotions:** Decisive when availability is clear; frustrated when server-side validation rejects a displayed option.

**Pain points:** UI can show full/near-full conditions that still require command-time validation; failure reasons may be technical.

**Needs:** Clear, receptionist-friendly error feedback and consistent capacity source.

**Success metrics:** Reservation success rate, booking-error recovery time, repeat booking attempts per check-in.

### Stage 3: Confirm outcome
**User goal:** Verify and communicate next steps.

**User actions:**
- Review outcome badge: current shift check-in vs future shift booking.
- Review shift/date/time and reservation status.
- Go to patient profile or start another check-in (context reset).

**Touchpoints / channels:** `PatientCheckInConfirmStep`, patient navigation, restart action.

**User thoughts / questions:** “Is the patient waiting today or coming back later?”

**Emotions:** Relieved when outcome is explicit.

**Pain points:** Ambiguity if reserve succeeds but arrival transitions partially fail.

**Needs:** Clear outcome classification and auditable reservation status.

**Success metrics:** Post-check-in correction rate, restart flow success rate, patient handoff clarity.

## 5. Decision Rules

1. **Patient resolution precedes booking.** Booking step starts only after a selected/created patient exists in context.
2. **Read-only discovery before mutation.** Lookup/search and duplicate review are read-only; create/reserve are explicit state-changing actions.
3. **Create is gated by search state.** Create action appears only when identification lookup is done with no exact match, or advanced search is done with zero results.
4. **Reservation is command-validated.** Slot display is advisory; the reserve command is authoritative.
5. **Same-day arrival marking is conditional.** Confirm/present transitions are attempted only for same-day bookings when `MarkAsArrived` is enabled.
6. **Outcome is singular and explicit.** Final outcome is one of `CurrentShift` or `FutureShift` and is shown in confirmation.
7. **Restart must fully reset journey state.** Check-in context and search/create contexts are reset before a new intake cycle.

## 6. Required Data and Validation

### Patient identification inputs
- Identification code (primary quick lookup path)
- Optional advanced search fields: name query, identification code, date of birth, phone, email

### New patient creation support
- Prefill from search context when available (identification code, name, date of birth, phone, email)
- Duplicate candidates shown before create commit

### Booking inputs
- Selected patient (reserving-party identifier derived from linked user id when present, otherwise patient id)
- Shift id
- Occurrence date

### Validation and failure handling
- Block booking on reservation command failure and show booking error message.
- Prevent silent state drift by storing selected shift, occurrence, reservation, and outcome in one check-in context.
- Keep a deterministic back path from Book → Identify.

## 7. Cross-Journey Insights

### Key pain points
1. Identity ambiguity during patient resolution.
2. Capacity/availability perception vs command-time validation.
3. Partial success risk in same-day confirm/present follow-up.

### Moments that matter
- **Moment of truth:** Successful reserve command for the selected slot.
- **Moment of friction:** Displayed slot is selected but reserve returns failure.
- **Moment of delight:** One-click restart for the next patient after successful confirmation.

### Design principles implied by the journey
- Keep a single active check-in context (`PatientCheckInContext`) as source of truth for journey state.
- Keep step responsibilities narrow: Identify, Book, Confirm.
- Separate query flows (lookup/search/slot listing) from mutation commands (create/reserve/confirm/present).
- Preserve simple operator UX while allowing extension points (waitlist, richer filters, notification workflows).

## 8. Technical Detail Addendum

### Implemented touchpoints (current)
- Route is owner-facing: `AppRoutes.Clinic.CheckIn` (`/owner/clinic/check-in`).
- Host page `ClinicOwnerPatientCheckInPage` orchestrates exactly three steps: `Identify` → `Book` → `Confirm`.
- Identify step uses `PatientFindOrCreate` with separate `PatientAdvancedSearchContext` and `PatientCreateContext`.
- Create flow supports duplicate-candidate surfacing and draft prefill from lookup/search inputs.
- Book step loads bookable slots for active shifts in a 30-day range, attempts reserve, and conditionally performs same-day confirm/present.
- Confirmation step displays patient, outcome type, reservation status, and provides go-to-patient / start-another actions.

### Gaps and extension opportunities
- Improve full/closed slot UX so warning states do not look bookable unless an authorized override exists.
- Surface explicit partial-success messaging when reserve succeeds but present transition does not.
- Add policy-driven waitlist/overflow path when same-day capacity is exhausted.
- Add richer slot filtering (provider/service/location) if those dimensions are required by clinic operations.
- Define patient-facing confirmation channels (print/SMS/email) for future-shift bookings.
