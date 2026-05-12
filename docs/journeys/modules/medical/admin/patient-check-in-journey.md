# Patient Check-In Journey

## 1. Context
- Product / service: Vitarish Medical + Appointments modules
- Journey type: Implemented guided journey with remaining product gaps
- Scenario: Admin / receptionist checks in a patient.
- User goal: Create the patient profile or retrieve the existing profile, then set an appointment for the current session or a future session when the current session is full.
- Business goal: Register arrivals quickly while protecting patient data quality and appointment capacity.
- Scope start: Patient arrives or contacts reception for check-in.
- Scope end: Patient has a verified profile and a confirmed current-session reservation/check-in or future-session reservation. Waitlist remains a future extension.
- Primary persona: Clinic receptionist / admin
- Secondary personas: Patient, scheduler, provider admin
- Evidence sources: Product scenario supplied for this journey; implemented `/Medical/CheckIn` flow and check-in components
- Confidence level: High for current implemented behavior; Medium for future extensions

## 2. Persona Snapshot
**Name / segment:** Front-desk receptionist

**Situation:** Handling patient arrivals while balancing walk-ins, pre-booked appointments, and session capacity.

**Motivation:** Move patients through intake without creating duplicate profiles or overbooking a session.

**Key needs:** Fast patient lookup, safe profile creation, clear session availability, linked user-account readiness for reservations, and an obvious future-session fallback when the current session is full.

**Constraints:** Patient data must be verified before scheduling; the current session can reach capacity; new profiles require enough identity and contact information to support follow-up.

**Success means:** The receptionist finds or creates exactly one patient profile and assigns the patient to the best available appointment option.

## 3. Journey Summary
**Narrative:** The receptionist starts the implemented `/Medical/CheckIn` flow from the medical dashboard or patient list, chooses an intent, searches for the patient, selects an existing profile or creates a validated profile, reviews today's active sessions, books the current session when a bookable occurrence has seats, or moves to a 30-day future-slot picker when today is full or unavailable. The journey ends with a confirmation that identifies the patient, reservation, session time, and whether the outcome is today's check-in or a future booking.

**Emotional arc:** Start: focused → Middle: cautious while matching records and capacity → End: confident once the appointment is confirmed

**Biggest friction point:** Avoiding duplicate patient profiles under time pressure.

**Highest-value opportunity:** The guided flow now combines profile retrieval, duplicate-aware profile creation, and appointment capacity decisions; the next opportunity is to add waitlist/escalation and patient-facing confirmation delivery.

## 4. Journey Stages

### Stage 1: Start check-in
**User goal:** Begin a guided intake flow for an arriving patient.

**User actions:** Open the Check In Patient action from the medical dashboard or patient list; choose walk-in, existing appointment arrival, or scheduled-for-later intent; enter the patient name and optional phone/date-of-birth secondary identifier; continue only after a name is present.

**Touchpoints / channels:** Medical dashboard; patient list; `/Medical/CheckIn`; patient search; appointment/session view.

**User thoughts / questions:** “Is this patient already in the system?”

**Emotions:** Focused, 3, because the flow starts with a clear task.

**Pain points:** Reception may have incomplete or inconsistent patient details.

**Needs:** Minimal required identifiers, fast lookup, and clear intent selection before any state-changing action.

**Opportunities:** Pre-fill search from scanned forms, appointment code, phone number, or date of birth.

**Success metrics:** Time to first search; percentage of check-ins started with sufficient identifiers; abandoned check-in rate.

**Evidence:** Scenario requirement.

### Stage 2: Retrieve or create profile
**User goal:** Resolve the patient to one trusted profile.

**User actions:** Search patient-backed project records by name; use the secondary identifier to highlight likely duplicates by date of birth, phone, or email; select the correct patient from duplicate or other results; if no match is suitable, create a new patient profile; confirm required fields before continuing.

**Touchpoints / channels:** Patient lookup results; patient profile summary; patient creation form.

**User thoughts / questions:** “Am I selecting the right patient, or do I need to create a new profile?”

**Emotions:** Cautious, 4, because creating duplicates or choosing the wrong record can affect care.

**Pain points:** Similar names, missing phone numbers, and outdated profile data slow down the receptionist.

**Needs:** Duplicate warnings, required identity fields, visible candidate identity hints, and a linked user account before reservation booking.

**Opportunities:** Add match confidence, richer duplicate candidate review, and profile freshness indicators.

**Success metrics:** Duplicate profile creation rate; lookup-to-selection time; profile validation error rate.

**Evidence:** Scenario requirement.

### Stage 3: Check current session capacity
**User goal:** Determine whether the patient can be appointed in the current session.

**User actions:** Review today's active sessions and bookable occurrences; inspect start/end time, reservation count, total seats, available seats, and whether the selected patient already has a non-cancelled/non-rejected/non-expired reservation today.

**Touchpoints / channels:** Select Session step; current session card; availability indicator; appointment booking panel; edit-patient fallback when a linked user account is missing.

**User thoughts / questions:** “Can this patient be seen now?”

**Emotions:** Decisive, 3, if availability is visible; frustrated, 4, if capacity is unclear.

**Pain points:** Current session may appear available visually while service rules reject booking because of capacity, timing, linked-account readiness, or duplicate appointment limits.

**Needs:** One source of truth for availability and clear reasons when booking is unavailable.

**Opportunities:** Show a real-time capacity banner with reasons such as full, closed, not started, cut-off passed, patient already booked, or missing linked user account.

**Success metrics:** Booking rejection rate after availability is shown; capacity conflict rate; time spent deciding current vs future session.

**Evidence:** Scenario requirement.

### Stage 4A: Book current session when capacity exists
**User goal:** Assign the patient to the current session.

**User actions:** Select Book Now on an available current-session occurrence; the system reserves the occurrence for the patient's linked user account; for walk-ins, it attempts to confirm the reservation and mark the patient present; then it saves the outcome in the check-in context.

**Touchpoints / channels:** Booking confirmation dialog; reservation details; check-in status indicator.

**User thoughts / questions:** “Is the patient now in today's queue?”

**Emotions:** Confident, 3, because the system confirms capacity and status.

**Pain points:** Reception needs to know whether booking alone is enough or whether an arrival status must also be recorded.

**Needs:** Clear distinction between reserving, confirming, and presenting the patient; visible error handling when reservation, confirmation, or present transitions fail.

**Opportunities:** Keep the walk-in one-click experience while surfacing partial-success states if reserve succeeds but confirm/present fails.

**Success metrics:** Current-session booking completion rate; patient queue accuracy; accidental no-show rate for walk-ins.

**Evidence:** Scenario requirement.

### Stage 4B: Offer future session when current session is full
**User goal:** Give the patient the next acceptable appointment option without overbooking the current session.

**User actions:** Open the future-session path from a full/unavailable current session or scheduled-for-later intent; review available occurrences across active sessions for the next 30 days; select a future slot; reserve it for the patient's linked user account; communicate date/time to the patient.

**Touchpoints / channels:** Future availability list; calendar/session selector; appointment confirmation.

**User thoughts / questions:** “What is the earliest acceptable alternative?”

**Emotions:** Helpful, 3, if alternatives are obvious; concerned, 4, if the patient expected to be seen today.

**Pain points:** A full current session can create pressure to override rules or promise uncertain availability.

**Needs:** Clear full-session/unavailable message, future alternatives, and optional waitlist/escalation if the clinic supports it.

**Opportunities:** Offer recommended next session, waitlist option, filters, and patient notification preference in the same flow.

**Success metrics:** Future booking conversion rate after full-session message; waitlist usage; manual override requests.

**Evidence:** Scenario requirement.

### Stage 5: Confirm and hand off
**User goal:** Complete check-in with an operationally reliable outcome.

**User actions:** Review patient, reservation status, session title, occurrence date/time, and outcome; navigate to the patient profile or start another check-in; hand off to waiting area for current-session outcomes or future reminder workflow for future outcomes.

**Touchpoints / channels:** Confirmation step; patient profile link; reservation status badge; future reminder workflow.

**User thoughts / questions:** “What should the patient do next?”

**Emotions:** Relieved, 2, because the next step is explicit.

**Pain points:** If the confirmation does not show the selected path, staff may misunderstand whether the patient is waiting today or returning later.

**Needs:** Summary of current-session vs future-session outcome, audit trail, and patient-facing instructions.

**Opportunities:** Add a printable check-in slip, SMS/email confirmation, and profile activity record.

**Success metrics:** Confirmation delivery rate; post-check-in correction rate; patient arrival-to-provider handoff time.

**Evidence:** Scenario requirement.

## 5. Decision Rules

1. **Profile is required before appointment creation.** The receptionist must resolve the patient to an existing profile or create a validated profile before booking.
2. **Lookup is read-only until the user chooses an action.** Search and duplicate checks should not mutate patient records.
3. **Create profile only after duplicate review.** If likely matches exist, the receptionist must select an existing profile or explicitly confirm that a new profile is needed.
4. **Current session booking depends on service-backed capacity.** The UI can suggest availability, but the appointment command must revalidate capacity at save time.
5. **A linked user account is required for reservations.** If the selected patient has no linked user ID, the flow must stop booking and route the receptionist to edit the patient profile.
6. **Future session is the default fallback when current session is full or unavailable.** The implemented flow searches active sessions for available slots in the next 30 days and does not overbook unless a separately authorized override workflow exists.
7. **Confirmation must identify the outcome.** The final state must say whether the patient is checked into the current session or scheduled for a future session; waitlist is not currently implemented.

## 6. Required Data and Validation

### Patient lookup inputs
- Full or partial name
- Phone number or email
- Date of birth or another clinic-approved secondary identifier
- Existing appointment reference, if available

### New profile minimum data
- Last name; first name is supported and should be captured when available
- At least one reliable contact method
- Date of birth or configured identity verifier
- Linked user account before appointment reservation
- Consent/communication preference where required by clinic policy

### Appointment inputs
- Patient profile identifier
- Session identifier
- Appointment date/time or selected session occurrence
- Appointment/reservation status, such as booked/reserved, confirmed, or present for current-session walk-ins
- Linked reserving party user identifier
- Optional reminder preference

### Validation and safe failure handling
- Block profile creation when required identity fields are missing.
- Warn when candidate duplicate profiles match the submitted identifiers.
- Block current-session booking when no linked user account exists, capacity is full, the booking window is closed, or the patient already has a conflicting appointment.
- Show a clear recovery action for every failure, such as edit profile, choose existing patient, pick future session, or retry after resolving the booking error. Waitlist recovery is a future extension.

## 7. Cross-Journey Insights

### Key pain points
1. Duplicate profile risk — occurs during patient retrieval or creation.
2. Linked-account readiness — occurs when a patient profile exists but cannot be used as a reservation party.
3. Capacity uncertainty — occurs when selecting the current session.
4. Ambiguous partial success — can occur when walk-in reserve succeeds but confirm/present status changes do not.

### Moments that matter
- **Moment of truth:** The reservation action that commits the selected profile to a current or future session without creating duplicates or exceeding capacity.
- **Moment of friction:** The current session is full and the patient expected same-day service.
- **Moment of delight:** The receptionist can offer the next best appointment without leaving the check-in flow.

### Design principles implied by the journey
- Keep patient identity as the source of truth before scheduling.
- Separate read-only search/availability queries from state-changing create/book/check-in commands.
- Validate capacity in the service layer and explain failures in receptionist-friendly language.
- Make the fallback path as easy as the preferred current-session path.
- Preserve a simple guided flow while allowing future extension for waitlists, provider assignment, reminders, filters, and override approvals.

## 8. Technical Detail Addendum

### Implemented touchpoints
- Admin entry points now route to `/Medical/CheckIn` from the medical dashboard and patient list.
- `PatientCheckInPage` orchestrates the Intent → Lookup → Create → Session → FutureSession → Confirm steps while keeping selected patient, duplicate candidates, sessions, reservations, and final outcome in one `PatientCheckInContext`.
- The lookup step queries patient/project records and classifies duplicate candidates when name plus secondary identifier matches date of birth, phone, or email.
- The create step pre-fills names from the search query and validates last name, date of birth, and at least one contact method before creating the patient record.
- The session step uses active sessions, bookable date queries, and reservation services to display today's capacity, block already-booked patients, reserve available current-session occurrences, and attempt confirm/present transitions for walk-ins.
- The future step lists available active-session occurrences for the next 30 days and reserves the selected future slot.
- The confirmation step distinguishes current-session check-in from future booking, displays the reservation status, and offers navigation to the patient profile or a reset for another check-in.

### Remaining issues and extensions
- Define and display partial-success handling when a walk-in reservation succeeds but confirm or present transitions fail.
- Route the scheduled-for-later intent directly to future-slot selection instead of requiring the receptionist to encounter a full/unavailable current session first.
- Define whether full current sessions support waitlist, authorized overbooking, or only future-session booking; waitlist is not implemented in the current flow.
- Add richer future-session filtering by provider, service type, location, or patient preference once those fields are available.
- Define duplicate matching rules beyond the current in-flow candidate hints and add a workflow for merging profiles created by mistake.
- Define patient-facing confirmation channels and reminder behavior for current vs future appointments.
- Decide whether newly created patients should be required to link a user account during intake or be routed through profile editing before booking.
