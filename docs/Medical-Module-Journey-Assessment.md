# Medical Module User Journey Assessment

Date assessed: 2026-05-10

## Scope and method

This report maps the requested medical-module objectives to the journeys that currently exist in the codebase and the smallest future journeys needed to close gaps. Evidence comes from static code review of the Medical, Sea, and Appointments modules.

Implementation levels:

- **Implemented:** the user can complete the journey in the current UI and supporting services.
- **Partially implemented:** enough UI/service support exists for part of the journey, but one or more requested objectives are missing, indirect, or not patient-specific.
- **Not implemented:** no clear user-facing journey exists for the requested objective.

## Executive summary

| Objective area | Status | Assessment |
| --- | --- | --- |
| Patients Profile Management (CRUD) | **Partially implemented** | Create, view, edit, delete/archive, and global search exist. Active/inactive status and duplicate prevention are missing. Filtering is minimal and implicit, not a dedicated patient filter journey. |
| Patients Procedures Management | **Partially implemented** | Procedures can be created under a patient, viewed, edited, deleted, and progressed through event/status-like steps with notes/attachments. Provider assignment, explicit procedure status, and procedure cancellation are not modeled. |
| Patients Appointments Management | **Partially implemented** | Appointment booking, status changes, cancellation, capacity protection, session list/search, and status filtering exist. Direct appointment edit/reschedule UI, provider/room assignment, calendar view, follow-up creation, and patient-specific appointment search/filter are missing or indirect. |
| Patients Overview | **Partially implemented** | Patient profile, stats, procedures, appointment list, and appointment booking quick action exist. Upcoming vs history separation, activity feed, flags, and a consolidated overview panel are missing. |

## Evidence highlights

- Medical pages expose patient list, new, detail, edit, procedure, and procedure-event routes under `/Medical/Patients`.
- Patient data is represented as a Medical wrapper around Sea `Project` records, with patient-specific fields stored in metadata and user linkage through `UserIds`.
- Procedures and event timelines are provided by the Sea module and embedded into patient pages.
- Patient appointment booking uses the Appointments reservation/session model and requires a linked user account.
- Reservation creation validates active sessions, booking windows, per-party limits, capacity, and occurrence availability.

## Objective-by-objective assessment

### Patients Profile Management (CRUD)

| Requested objective | Status | Evidence / notes |
| --- | --- | --- |
| Create patient profiles | **Implemented** | `/Medical/Patients/New` renders `PatientEditForm`; saving creates a project/patient. |
| View patient details | **Implemented** | Patient detail page renders `PatientDetails`, procedures, appointments, and plugin sections. |
| Edit patient information | **Implemented** | `/Medical/Patients/{patientId}/Edit` loads the patient and renders `PatientEditForm`. |
| Archive/delete patient profiles | **Implemented** | `PatientDetails` inherits project deletion; delete uses a strict confirmation then removes the record. This appears to be soft-delete through common entity filters. |
| Search and filter patients | **Partially implemented** | Patient list consumes global search text and groups by presence of surgery date; no dedicated patient filters for status, demographics, linked user, etc. |
| Manage patient status (active/inactive) | **Not implemented** | No patient status field, enum, UI control, or filter was found. |
| Prevent duplicate patient records | **Not implemented** | Patient create/update validates only a non-empty composed name; no duplicate check was found for name/date of birth/phone/email/linked user. |

### Patients Procedures Management

| Requested objective | Status | Evidence / notes |
| --- | --- | --- |
| Create procedure records | **Implemented** | Patient detail page renders `ProceduresGrid`; admin users can open `ProcedureCreateDialog`. |
| Assign procedures to patients and providers | **Partially implemented** | Procedures are created under a patient/project via `ProjectId`. Provider assignment is not visible as a procedure field. Event roles can be edited, but this is step-level access/roles rather than provider assignment. |
| Update procedure details | **Implemented** | `ProcedureDetails` opens `ProcedureEditDialog`; name and description can be updated. |
| Track procedure status | **Partially implemented** | Event steps can be committed/uncommitted and procedure stats exist. No explicit procedure-level status such as planned/in progress/completed/cancelled was found. |
| Add procedure notes | **Partially implemented** | Procedure description can be edited; event timeline supports attachment/note input on event details. No procedure-level notes field/journey exists. |
| View procedure history | **Implemented** | Procedure page displays an event chain; event pages display attachment timelines. |
| Cancel procedures | **Not implemented** | Delete exists, but no cancel status/action or cancellation reason workflow was found. |

### Patients Appointments Management

| Requested objective | Status | Evidence / notes |
| --- | --- | --- |
| Create appointments | **Implemented** | Patient appointment panel opens a booking dialog; the dialog reserves the first bookable occurrence for an active session. |
| Edit/reschedule appointments | **Partially implemented** | Back-end `MoveReservationAsync` exists, but the reviewed UI only opens reservation details/status actions and does not expose a reschedule flow. |
| Cancel appointments | **Implemented** | Reservation details expose cancel actions for pending/confirmed/waitlisted reservations. |
| Manage appointment statuses | **Implemented** | Reservation details transition pending, confirmed, present, completed, rejected, and cancelled states. |
| Assign providers and rooms | **Not implemented** | Session form captures title, day/time, capacity, status, visibility, approval, and rescheduling; no provider or room fields were found. |
| Prevent double-bookings | **Partially implemented** | Reservation creation uses booking windows, per-party limits, capacity checks, and an occurrence lock. This protects seats and repeat party limits, but not provider/room double-booking because provider/room assignment is absent. |
| View appointments in calendar/list view | **Partially implemented** | Lists exist for sessions and reservations; a session occurrence calendar component exists, but patient-specific appointment UI is a list only. |
| Search and filter appointments | **Partially implemented** | Session/reservation lists support search/status filters; patient appointment panel has no local search/filter. |
| Create follow-up appointments | **Not implemented** | No follow-up-specific action or prefilled journey was found. A user can book another appointment manually. |

### Patients Overview

| Requested objective | Status | Evidence / notes |
| --- | --- | --- |
| View patient summary/profile | **Implemented** | `PatientDetails` shows demographic, clinical, linked-user, and stats information. |
| View upcoming appointments | **Partially implemented** | Patient appointment panel lists reservations ordered by date but does not separate upcoming appointments from historical appointments. |
| View appointment history | **Partially implemented** | Same reservation list can include historical data but has no history section/filter in patient context. |
| View procedures history/status | **Partially implemented** | Procedures list and event history exist; explicit procedure-level status is absent. |
| View activity feed/timeline | **Partially implemented** | Event timeline exists per procedure event; no consolidated patient timeline across profile/procedures/appointments exists. |
| Access quick actions (edit, book, add procedure) | **Implemented** | Patient page exposes edit, add procedure, and add appointment actions for admins. |
| View important patient flags/notes | **Partially implemented** | Clinical notes and allergies/medications fields exist, but there is no prominent flag model or dedicated critical-alert UI. |

## Required user journey maps

The maps below are intentionally short. Current-state maps document implemented or partial flows; future-state maps document missing journeys needed to satisfy the objectives.

---

# Patient Profile CRUD Journey

## 1. Context
- Product / service: Vitarish Medical module
- Journey type: Current state
- Scenario: Admin creates, finds, views, edits, or deletes a patient.
- User goal: Keep a patient profile accurate.
- Business goal: Maintain reliable patient records.
- Scope start: Admin opens Patients.
- Scope end: Patient record is saved, opened, or removed.
- Primary persona: Clinic admin / provider admin
- Evidence sources: Static code review assumptions
- Confidence level: High

## 2. Persona Snapshot
**Name / segment:** Clinic admin  
**Situation:** Managing patient records during daily operations.  
**Motivation:** Find and update patient information quickly.  
**Key needs:** Simple create, search, edit, delete.  
**Constraints:** Duplicate and active/inactive controls are absent.  
**Success means:** The correct patient record is current and reachable.  

## 3. Journey Summary
**Narrative:** The admin opens Patients, searches or adds a patient, completes the patient form, saves, views the profile, and later edits or deletes it when needed.

**Emotional arc:** Start: focused → Middle: confident → End: satisfied

**Biggest friction point:** No duplicate warning before save.

**Highest-value opportunity:** Add patient status and duplicate detection to the save flow.

## 4. Journey Stages

### Stage 1: Find or start
**User goal:** Locate an existing patient or start a new one.  
**User actions:** Open Patients; search; select Add Patient.  
**Touchpoints / channels:** Medical Patients page.  
**User thoughts / questions:** “Is this patient already here?”  
**Emotions:** Confident, 3, because search exists.  
**Pain points:** Limited filters; no duplicate hints.  
**Needs:** Search and clear empty states.  
**Opportunities:** Add filters and duplicate candidates.  
**Success metrics:** Search success rate; duplicate creation rate.  
**Evidence:** Static code review assumption.

### Stage 2: Save and maintain
**User goal:** Create or update the patient safely.  
**User actions:** Enter demographics/clinical fields; save; edit; delete if needed.  
**Touchpoints / channels:** Patient edit form and details page.  
**User thoughts / questions:** “Did I enter enough information?”  
**Emotions:** Cautious, 3, because medical data must be accurate.  
**Pain points:** Required validation is minimal.  
**Needs:** Validation, status, audit-friendly deletion.  
**Opportunities:** Add active/inactive status and stronger validation.  
**Success metrics:** Save failures; missing required data; restoration requests.  
**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points
1. Duplicate uncertainty — occurs at creation.
2. No status control — occurs during maintenance.
3. Limited filtering — occurs during lookup.

### Moments that matter
- **Moment of truth:** Saving a new patient.
- **Moment of friction:** Unsure whether the patient is a duplicate.
- **Moment of delight:** Search quickly finds the right profile.

### Design principles implied by the journey
- Prevent bad records before they are saved.
- Keep the form short but extensible.
- Make patient state visible everywhere.

---

# Patient Procedure Journey

## 1. Context
- Product / service: Vitarish Medical + Sea modules
- Journey type: Current state with gaps
- Scenario: Admin adds a procedure and records step-level history.
- User goal: Track care/procedure work for a patient.
- Business goal: Preserve clinical procedure history.
- Scope start: Admin opens a patient profile.
- Scope end: Procedure details, events, and notes are recorded.
- Primary persona: Provider admin / clinician
- Evidence sources: Static code review assumptions
- Confidence level: Medium

## 2. Persona Snapshot
**Name / segment:** Clinician  
**Situation:** Documenting patient procedure progress.  
**Motivation:** Keep procedure history understandable.  
**Key needs:** Add procedure, update details, record events/notes.  
**Constraints:** No procedure provider assignment or cancel status.  
**Success means:** Team can see what happened and what is next.  

## 3. Journey Summary
**Narrative:** The clinician opens a patient, adds a procedure, edits its name/description, opens the procedure, adds events, commits steps, and records timeline notes/attachments.

**Emotional arc:** Start: purposeful → Middle: attentive → End: informed

**Biggest friction point:** Status is inferred from events instead of shown as a procedure status.

**Highest-value opportunity:** Add procedure status, provider assignment, and cancel action.

## 4. Journey Stages

### Stage 1: Add or open procedure
**User goal:** Select the procedure to work on.  
**User actions:** Open patient; add procedure or select existing procedure.  
**Touchpoints / channels:** Patient details; Procedures grid.  
**User thoughts / questions:** “Which procedure does this note belong to?”  
**Emotions:** Focused, 3, because the list is simple.  
**Pain points:** No provider/status at a glance.  
**Needs:** Clear ownership and status.  
**Opportunities:** Show procedure status and assigned provider in the list.  
**Success metrics:** Procedure selection errors; time to locate procedure.  
**Evidence:** Static code review assumption.

### Stage 2: Record history
**User goal:** Document progress.  
**User actions:** Add/edit events; commit a step; add notes/attachments.  
**Touchpoints / channels:** Procedure page; event page; timeline.  
**User thoughts / questions:** “Is this step complete?”  
**Emotions:** Careful, 4, because clinical history matters.  
**Pain points:** Procedure-level cancellation is missing.  
**Needs:** Status, cancel reason, provider responsibility.  
**Opportunities:** Add a procedure workflow state machine.  
**Success metrics:** Completed procedure documentation; missing-note rate.  
**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points
1. Missing provider assignment — occurs at procedure setup.
2. Missing explicit status — occurs during tracking.
3. Missing cancel workflow — occurs when work stops.

### Moments that matter
- **Moment of truth:** Committing an event step.
- **Moment of friction:** Explaining whether a procedure is active, done, or cancelled.
- **Moment of delight:** Timeline notes preserve procedure context.

### Design principles implied by the journey
- Separate procedure status from event history.
- Keep notes close to the clinical step they describe.
- Make assignment explicit, not inferred.

---

# Patient Appointment Journey

## 1. Context
- Product / service: Vitarish Medical + Appointments modules
- Journey type: Current state with gaps
- Scenario: Admin books and manages a patient reservation.
- User goal: Schedule and update patient appointments.
- Business goal: Use appointment capacity reliably.
- Scope start: Admin opens patient profile or appointment lists.
- Scope end: Appointment is booked, statused, cancelled, or viewed.
- Primary persona: Clinic scheduler
- Evidence sources: Static code review assumptions
- Confidence level: Medium

## 2. Persona Snapshot
**Name / segment:** Scheduler  
**Situation:** Booking patients into available sessions.  
**Motivation:** Fill schedules without conflicts.  
**Key needs:** Book, cancel, status, search, reschedule.  
**Constraints:** Patient must be linked to a user; provider/room data is absent.  
**Success means:** The right patient has the right appointment at the right time.  

## 3. Journey Summary
**Narrative:** The scheduler opens a patient, confirms the patient is linked to a user, opens booking, selects an active session, books the next available occurrence, then manages status or cancellation from reservation details.

**Emotional arc:** Start: efficient → Middle: blocked or confident → End: relieved

**Biggest friction point:** Booking cannot proceed until the patient has a linked user account.

**Highest-value opportunity:** Add rescheduling, provider/room assignment, and follow-up booking.

## 4. Journey Stages

### Stage 1: Book
**User goal:** Create an appointment for a patient.  
**User actions:** Open patient; click add appointment; select active session.  
**Touchpoints / channels:** Patient appointments panel; booking dialog.  
**User thoughts / questions:** “Which appointment slot is available?”  
**Emotions:** Confident, 3, if sessions exist; frustrated, 4, if user linkage is missing.  
**Pain points:** No explicit date picker; first bookable occurrence is chosen.  
**Needs:** Visible slot/date choice and clear capacity.  
**Opportunities:** Add date selection and room/provider context.  
**Success metrics:** Booking completion; booking errors; linked-user blockers.  
**Evidence:** Static code review assumption.

### Stage 2: Manage
**User goal:** Update appointment state.  
**User actions:** Open appointment; confirm, mark present/completed, cancel/reject.  
**Touchpoints / channels:** Reservation detail dialog.  
**User thoughts / questions:** “Has the patient arrived?”  
**Emotions:** In control, 3, because status actions are clear.  
**Pain points:** Reschedule is not exposed in UI.  
**Needs:** Reschedule, follow-up, calendar/list toggle.  
**Opportunities:** Add edit/reschedule and follow-up action from details.  
**Success metrics:** Reschedule completion; cancellation rate; no-show workflow completion.  
**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points
1. Linked-user dependency — occurs before booking.
2. Missing reschedule UI — occurs after booking.
3. Missing provider/room assignment — occurs during schedule design.

### Moments that matter
- **Moment of truth:** Reservation save after capacity validation.
- **Moment of friction:** Patient lacks a linked user account.
- **Moment of delight:** Status actions match appointment operations.

### Design principles implied by the journey
- Show availability before committing.
- Prevent conflicts in services, not only the UI.
- Keep follow-up close to completed/cancelled appointments.

---

# Patient Overview Journey

## 1. Context
- Product / service: Vitarish Medical module
- Journey type: Future state, based on partial current state
- Scenario: Clinician opens a patient and needs a concise care snapshot.
- User goal: Understand the patient quickly and act.
- Business goal: Reduce navigation and missed context.
- Scope start: User opens a patient profile.
- Scope end: User understands status and chooses an action.
- Primary persona: Clinician / clinic admin
- Evidence sources: Static code review assumptions
- Confidence level: Medium

## 2. Persona Snapshot
**Name / segment:** Clinician  
**Situation:** Preparing for or reviewing a patient interaction.  
**Motivation:** See key risks, appointments, procedures, and next actions.  
**Key needs:** Summary, upcoming/history, flags, timeline, quick actions.  
**Constraints:** Current data is split across profile, procedures, and appointment panels.  
**Success means:** Clinician can act without hunting for context.  

## 3. Journey Summary
**Narrative:** The clinician opens the patient and sees a compact summary, critical notes/flags, upcoming appointments, recent history, procedure status, timeline, and quick actions to edit, book, or add a procedure.

**Emotional arc:** Start: curious → Middle: oriented → End: confident

**Biggest friction point:** No consolidated timeline or flag area.

**Highest-value opportunity:** Create one patient overview source of truth that aggregates profile, appointments, procedures, and flags.

## 4. Journey Stages

### Stage 1: Orient
**User goal:** Understand the patient at a glance.  
**User actions:** Open patient; scan summary, flags, upcoming items.  
**Touchpoints / channels:** Patient overview panel.  
**User thoughts / questions:** “What matters before I act?”  
**Emotions:** Alert, 3, because medical context can be critical.  
**Pain points:** Important data is not prioritized.  
**Needs:** Flags, status, recent changes.  
**Opportunities:** Add a top-of-page overview card.  
**Success metrics:** Time to first action; missed-flag incidents; page bounce/navigation count.  
**Evidence:** Static code review assumption.

### Stage 2: Act
**User goal:** Take the next clinical/admin action.  
**User actions:** Edit patient; book/follow-up; add procedure; open history.  
**Touchpoints / channels:** Quick actions and timeline.  
**User thoughts / questions:** “What is the next best action?”  
**Emotions:** Confident, 4, if next steps are obvious.  
**Pain points:** Upcoming/history are not separated.  
**Needs:** Clear quick actions and chronological activity.  
**Opportunities:** Add unified timeline and action menu.  
**Success metrics:** Action completion; extra clicks; support requests.  
**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points
1. Data fragmentation — occurs on profile review.
2. Missing flags — occurs during clinical risk scanning.
3. History/upcoming not separated — occurs during planning.

### Moments that matter
- **Moment of truth:** Clinician decides what to do next.
- **Moment of friction:** User switches between panels to reconstruct history.
- **Moment of delight:** One screen answers “what matters now?”

### Design principles implied by the journey
- Prioritize safety-critical information.
- Separate upcoming, historical, and current-state data.
- Put quick actions next to the context that triggers them.

## Recommended implementation backlog

1. **Patient integrity:** Add patient active/inactive status, duplicate candidate detection, and stronger required-field validation.
2. **Patient filtering:** Add patient filters for status, surgery date, linked user/provider, and key demographic/clinical fields.
3. **Procedure workflow:** Add procedure status, assigned provider(s), procedure notes, and a cancel action with reason.
4. **Appointment scheduling:** Add patient-facing reschedule/edit UI, explicit date/slot selection, provider and room assignment, and follow-up creation.
5. **Scheduling conflict model:** Extend conflict checks to provider and room once those fields exist.
6. **Patient overview:** Build a consolidated overview panel with flags, upcoming appointments, appointment history, procedure status, and activity feed.
