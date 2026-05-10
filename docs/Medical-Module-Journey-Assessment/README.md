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

## Journey map files

The maps below are intentionally short. Current-state maps document implemented or partial flows; future-state maps document missing journeys needed to satisfy the objectives.

- [Patient Profile CRUD Journey](./patient-profile-crud-journey.md)
- [Patient Procedure Journey](./patient-procedure-journey.md)
- [Patient Appointment Journey](./patient-appointment-journey.md)
- [Patient Overview Journey](./patient-overview-journey.md)

## Recommended implementation backlog
1. **Patient integrity:** Add patient active/inactive status, duplicate candidate detection, and stronger required-field validation.
2. **Patient filtering:** Add patient filters for status, surgery date, linked user/provider, and key demographic/clinical fields.
3. **Procedure workflow:** Add procedure status, assigned provider(s), procedure notes, and a cancel action with reason.
4. **Appointment scheduling:** Add patient-facing reschedule/edit UI, explicit date/slot selection, provider and room assignment, and follow-up creation.
5. **Scheduling conflict model:** Extend conflict checks to provider and room once those fields exist.
6. **Patient overview:** Build a consolidated overview panel with flags, upcoming appointments, appointment history, procedure status, and activity feed.
