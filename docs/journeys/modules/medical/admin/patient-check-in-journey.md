# Patient Check-In Journey

## 1. Context
- Product / service: Vitarish Medical + Appointments modules
- Journey type: Target state journey description
- Scenario: Admin / receptionist checks in a patient.
- User goal: Create the patient profile or retrieve the existing profile, then set an appointment for the current session or a future session when the current session is full.
- Business goal: Register arrivals quickly while protecting patient data quality and appointment capacity.
- Scope start: Patient arrives or contacts reception for check-in.
- Scope end: Patient has a verified profile and a confirmed appointment, waitlist placement, or future appointment.
- Primary persona: Clinic receptionist / admin
- Secondary personas: Patient, scheduler, provider admin
- Evidence sources: Product scenario supplied for this journey
- Confidence level: Medium

## 2. Persona Snapshot
**Name / segment:** Front-desk receptionist

**Situation:** Handling patient arrivals while balancing walk-ins, pre-booked appointments, and session capacity.

**Motivation:** Move patients through intake without creating duplicate profiles or overbooking a session.

**Key needs:** Fast patient lookup, safe profile creation, clear session availability, and an obvious fallback when the current session is full.

**Constraints:** Patient data must be verified before scheduling; the current session can reach capacity; new profiles require enough identity and contact information to support follow-up.

**Success means:** The receptionist finds or creates exactly one patient profile and assigns the patient to the best available appointment option.

## 3. Journey Summary
**Narrative:** The receptionist starts check-in, searches for the patient, verifies an existing profile or creates a new one, reviews current session availability, books the patient into the current session when capacity exists, or offers the next suitable future session when the current session is full. The journey ends with a confirmation that the patient can understand and the clinic can operationally trust.

**Emotional arc:** Start: focused → Middle: cautious while matching records and capacity → End: confident once the appointment is confirmed

**Biggest friction point:** Avoiding duplicate patient profiles under time pressure.

**Highest-value opportunity:** Provide one guided check-in flow that combines profile retrieval, duplicate-safe profile creation, and appointment capacity decisions.

## 4. Journey Stages

### Stage 1: Start check-in
**User goal:** Begin a guided intake flow for an arriving patient.

**User actions:** Open the admin check-in entry point; choose walk-in, existing appointment arrival, or scheduled-for-later intent; collect the patient's name and at least one secondary identifier.

**Touchpoints / channels:** Reception dashboard; patient search; appointment/session view.

**User thoughts / questions:** “Is this patient already in the system?”

**Emotions:** Focused, 3, because the flow starts with a clear task.

**Pain points:** Reception may have incomplete or inconsistent patient details.

**Needs:** Minimal required identifiers, fast lookup, and visible privacy safeguards.

**Opportunities:** Pre-fill search from scanned forms, appointment code, phone number, or date of birth.

**Success metrics:** Time to first search; percentage of check-ins started with sufficient identifiers; abandoned check-in rate.

**Evidence:** Scenario requirement.

### Stage 2: Retrieve or create profile
**User goal:** Resolve the patient to one trusted profile.

**User actions:** Search by name, phone, date of birth, email, or linked account; review candidate matches; select the correct patient; if no match exists, create a new patient profile; confirm required fields before continuing.

**Touchpoints / channels:** Patient lookup results; patient profile summary; patient creation form.

**User thoughts / questions:** “Am I selecting the right patient, or do I need to create a new profile?”

**Emotions:** Cautious, 4, because creating duplicates or choosing the wrong record can affect care.

**Pain points:** Similar names, missing phone numbers, and outdated profile data slow down the receptionist.

**Needs:** Duplicate warnings, required identity fields, and a read-only profile preview before selection.

**Opportunities:** Add match confidence, duplicate candidate review, and profile freshness indicators.

**Success metrics:** Duplicate profile creation rate; lookup-to-selection time; profile validation error rate.

**Evidence:** Scenario requirement.

### Stage 3: Check current session capacity
**User goal:** Determine whether the patient can be appointed in the current session.

**User actions:** Review today's active/current session; inspect remaining capacity, booked count, waitlist count, appointment cut-off rules, and any patient-specific eligibility constraints.

**Touchpoints / channels:** Current session card; availability indicator; appointment booking panel.

**User thoughts / questions:** “Can this patient be seen now?”

**Emotions:** Decisive, 3, if availability is visible; frustrated, 4, if capacity is unclear.

**Pain points:** Current session may appear available visually while service rules reject booking because of capacity, timing, or duplicate appointment limits.

**Needs:** One source of truth for availability and clear reasons when booking is unavailable.

**Opportunities:** Show a real-time capacity banner with reasons such as full, closed, not started, cut-off passed, or patient already booked.

**Success metrics:** Booking rejection rate after availability is shown; capacity conflict rate; time spent deciding current vs future session.

**Evidence:** Scenario requirement.

### Stage 4A: Book current session when capacity exists
**User goal:** Assign the patient to the current session.

**User actions:** Select current session; confirm appointment details; optionally mark the patient as arrived/checked in; provide queue number or expected wait time; save.

**Touchpoints / channels:** Booking confirmation dialog; reservation details; check-in status indicator.

**User thoughts / questions:** “Is the patient now in today's queue?”

**Emotions:** Confident, 3, because the system confirms capacity and status.

**Pain points:** Reception needs to know whether booking alone is enough or whether an arrival status must also be recorded.

**Needs:** Clear distinction between creating an appointment and marking arrival/check-in.

**Opportunities:** Combine booking and arrival status in a single confirm action for walk-ins while keeping the underlying commands separate.

**Success metrics:** Current-session booking completion rate; patient queue accuracy; accidental no-show rate for walk-ins.

**Evidence:** Scenario requirement.

### Stage 4B: Offer future session when current session is full
**User goal:** Give the patient the next acceptable appointment option without overbooking the current session.

**User actions:** Review next available sessions; filter by date, provider, service type, or location if applicable; select a future session; confirm appointment details; communicate date/time to the patient.

**Touchpoints / channels:** Future availability list; calendar/session selector; appointment confirmation.

**User thoughts / questions:** “What is the earliest acceptable alternative?”

**Emotions:** Helpful, 3, if alternatives are obvious; concerned, 4, if the patient expected to be seen today.

**Pain points:** A full current session can create pressure to override rules or promise uncertain availability.

**Needs:** Clear full-session message, future alternatives, and optional waitlist/escalation if the clinic supports it.

**Opportunities:** Offer recommended next session, waitlist option, and patient notification preference in the same flow.

**Success metrics:** Future booking conversion rate after full-session message; waitlist usage; manual override requests.

**Evidence:** Scenario requirement.

### Stage 5: Confirm and hand off
**User goal:** Complete check-in with an operationally reliable outcome.

**User actions:** Review patient, appointment, session, status, and contact details; save or print/send confirmation; hand off to waiting area or future reminder workflow.

**Touchpoints / channels:** Confirmation page; appointment receipt; reminders; patient profile timeline.

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
5. **Future session is the default fallback when current session is full.** The system should not overbook unless a separately authorized override workflow exists.
6. **Confirmation must identify the outcome.** The final state must say whether the patient is checked into the current session, scheduled for a future session, or placed on a waitlist.

## 6. Required Data and Validation

### Patient lookup inputs
- Full or partial name
- Phone number or email
- Date of birth or another clinic-approved secondary identifier
- Existing appointment reference, if available

### New profile minimum data
- First name and last name
- At least one reliable contact method
- Date of birth or configured identity verifier
- Consent/communication preference where required by clinic policy

### Appointment inputs
- Patient profile identifier
- Session identifier
- Appointment date/time or selected session occurrence
- Appointment status, such as booked, checked in, or waitlisted
- Optional reminder preference

### Validation and safe failure handling
- Block profile creation when required identity fields are missing.
- Warn when candidate duplicate profiles match the submitted identifiers.
- Block current-session booking when capacity is full, the booking window is closed, or the patient already has a conflicting appointment.
- Show a clear recovery action for every failure, such as edit profile, choose existing patient, pick future session, or join waitlist.

## 7. Cross-Journey Insights

### Key pain points
1. Duplicate profile risk — occurs during patient retrieval or creation.
2. Capacity uncertainty — occurs when selecting the current session.
3. Ambiguous final status — occurs when appointment booking and arrival check-in are not clearly separated.

### Moments that matter
- **Moment of truth:** The save action that commits a profile and appointment without creating duplicates or exceeding capacity.
- **Moment of friction:** The current session is full and the patient expected same-day service.
- **Moment of delight:** The receptionist can offer the next best appointment without leaving the check-in flow.

### Design principles implied by the journey
- Keep patient identity as the source of truth before scheduling.
- Separate read-only search/availability queries from state-changing create/book/check-in commands.
- Validate capacity in the service layer and explain failures in receptionist-friendly language.
- Make the fallback path as easy as the preferred current-session path.
- Preserve a simple guided flow while allowing future extension for waitlists, provider assignment, reminders, and override approvals.

## 8. Technical Detail Addendum

### Proposed implementation touchpoints
- Add an admin check-in entry point that orchestrates patient lookup, patient creation, current-session availability, and appointment booking without embedding business rules directly in the UI.
- Reuse the patient profile form for profile creation, but add a duplicate-candidate query before creating a new patient.
- Reuse appointment session and reservation services for capacity-backed booking, with a dedicated availability query for read-only current/future session options.
- Model the check-in flow state in one view model so the selected patient, candidate profile matches, selected session, and final appointment outcome have a single source of truth.
- Keep profile creation, reservation booking, and arrival/check-in status transitions as separate commands so failures can be retried safely.

### Issues to address before implementation
- Define whether “checked in” is a reservation status, an appointment event, or a separate attendance marker.
- Define whether full current sessions support waitlist, authorized overbooking, or only future-session booking.
- Define the required minimum profile fields for reception-created patients.
- Define duplicate matching rules and the workflow for merging profiles created by mistake.
- Define patient-facing confirmation channels and reminder behavior for current vs future appointments.
