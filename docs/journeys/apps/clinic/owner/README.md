# Clinic Owner Patient Journey Assessment

Date assessed: 2026-05-10

## Scope and method

This report maps the requested clinic-owner patient objectives to the admin-only journeys that currently exist in the codebase and the smallest future journeys needed to close gaps. Evidence comes from static code review of the Clinic app host, Sea, Clinic, and Appointments modules.

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
| Patient Check-In | **Journey described** | Target-state receptionist journey covers profile retrieval/creation, duplicate-safe intake, current-session capacity checks, and future-session fallback when the current session is full. |
| Patients Overview | **Partially implemented** | Patient profile, stats, procedures, appointment list, and appointment booking quick action exist. Upcoming vs history separation, activity feed, flags, and a consolidated overview panel are missing. |

## Evidence highlights

- Clinic owner pages expose patient list, new, detail, edit, procedure, and procedure-event routes under `/owner/clinic/patients`.
- Patient data is represented through clinic patient wrappers around Sea `Project` records, with patient-specific fields stored in metadata and user linkage through `UserIds`.
- Procedures and event timelines are provided by the Sea module and embedded into patient pages.
- Patient appointment booking uses the Appointments reservation/session model and requires a linked user account.
- Reservation creation validates active sessions, booking windows, per-party limits, capacity, and occurrence availability.

## Objective-by-objective assessment

### Patients Profile Management (CRUD)

| Requested objective | Status | Evidence / notes |
| --- | --- | --- |
| Create patient profiles | **Implemented** | `/owner/clinic/patients/new` renders `PatientEditForm`; saving creates a project/patient. |
| View patient details | **Implemented** | Patient detail page renders `PatientDetails`, procedures, appointments, and plugin sections. |
| Edit patient information | **Implemented** | `/owner/clinic/patients/{patientId}/edit` loads the patient and renders `PatientEditForm`. |
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

## Additional clinic owner reservation journeys


- [Reservation Overview Journey](./owner-reservation-overview-journey.md)
- [Reservation List and Calendar Journey](./owner-reservation-list-calendar-journey.md)
- [Reservation Management Journey](./owner-reservation-management-journey.md)
- [Reservation Module Capability Journeys](./owner-reservation-module-capabilities-journey.md)

## Recommended implementation backlog
1. **Patient integrity:** Add patient active/inactive status, duplicate candidate detection, and stronger required-field validation.
2. **Patient filtering:** Add patient filters for status, surgery date, linked user/provider, and key demographic/clinical fields.
3. **Procedure workflow:** Add procedure status, assigned provider(s), procedure notes, and a cancel action with reason.
4. **Appointment scheduling:** Add patient-facing reschedule/edit UI, explicit date/slot selection, provider and room assignment, and follow-up creation.
5. **Scheduling conflict model:** Extend conflict checks to provider and room once those fields exist.
6. **Patient overview:** Build a consolidated overview panel with flags, upcoming appointments, appointment history, procedure status, and activity feed.

## Technical implementation inventory and issue map

### Patient profile CRUD

| Component / unit | Responsibility today | Issues needing attention |
| --- | --- | --- |
| `ClinicOwnerPatientsPage.razor` | Hosts `/owner/clinic/patients`, enables global search, and renders `PatientsGrid`. | Page has no dedicated patient filter model beyond global search. |
| PatientsGrid | Inherits ProjectsGrid, loads Sea projects, maps them to patients, groups by surgery-date presence, and supports a mass-edit mode for bulk patient updates. | Add status, duplicate-risk, linked-user, demographic, and clinical-data filters without hard-coding service internals into the grid. |
| `PatientEditForm` | Creates/updates a patient-backed project; handles avatar, demographics, clinical notes, linked user, and extended clinical data. | Validation only enforces a non-empty composed name; add duplicate checks, stronger required-field validation, and status controls before saving. |
| `PatientDetails` / `ProjectDetails` | Displays patient metadata and delegates edit/delete actions to Sea project behavior. | Clarify archive versus delete; introduce patient-specific status and audit-friendly removal semantics. |
| `PatientReadDto` / `PatientWriteDto` | Medical metadata wrappers over Sea project DTOs. | Add one source-of-truth patient status and duplicate-match fields/normalization strategy. |

### Patient procedures

| Component / unit | Responsibility today | Issues needing attention |
| --- | --- | --- |
| `ProceduresGrid` | Lists procedures for `ProjectId`, sorts unread/new procedures first, and opens create dialog. | Cannot display provider, explicit status, cancellation state, or next clinical step because these fields are not modeled. |
| `ProcedureCreateDialog` | Creates a procedure under the patient project and optionally copies template events, roles, and attachments. | Template copy is useful, but provider assignment and initial workflow status are absent from creation. |
| `ProcedureDetails` / `ProcedureEditDialog` | Shows name/description and supports edit/delete. | Edit fields are too generic for medical procedures; delete should be complemented by cancel/close with reason. |
| `EventsGrid`, `EventDetails`, `EventItemsTimeline` | Provide step/event history, commit state, role editing, and attachment timeline. | Event commits are not a substitute for procedure-level status; role editing is not clinical provider assignment. |

### Patient appointments

| Component / unit | Responsibility today | Issues needing attention |
| --- | --- | --- |
| `PatientAppointmentsPanel` | Requires a linked user, queries reservations by `ReservingPartyId`, lists reservation status badges, and opens booking/detail dialogs. | Split upcoming/history, add patient-specific filters, and replace implicit `UserIds.FirstOrDefault()` scheduling identity with an explicit model. |
| `PatientAppointmentBookingDialog` | Lists active sessions and reserves the earliest future non-cancelled bookable occurrence. | Add slot/date selection, provider/room context, and clearer availability presentation before reservation creation. |
| `ReservationDetail` / `ReservationDetailDialog` | Performs reservation status transitions and terminal-state display. | Add reschedule UI, cancellation/rejection reasons, and follow-up creation actions while keeping read detail separate from state changes. |
| `ReservationFilters` / `ReservationsList` | Provides global reservation status filtering. | Extend filters for date range, patient, session, provider/room, and upcoming/history use cases. |

### Patient overview

| Component / unit | Responsibility today | Issues needing attention |
| --- | --- | --- |
| `ClinicOwnerPatientPage.razor` | Stacks details, procedures, appointments, and plugin sections as the current patient page. | Introduce a single overview read model rather than forcing the page to compose module-specific context. |
| `PatientOverviewPanel` | Displays compact profile facts, stats, and linked user information. | Not currently the primary overview; expand it into a read-only summary for flags, next appointment, procedure statuses, and recent activity. |
| `PatientSummaryCard` | Provides a small navigable patient card with surgery date and stats. | Add status/flag badges once patient status and risk flags exist. |
| `IMedicalPatientSectionPlugin` / `IMedicalProcedureSectionPlugin` | Allows medical sections to extend patient/procedure pages. | Define stable ordered slots so extensions do not compete with safety-critical overview content. |

## Cross-cutting technical recommendations

1. Create patient-specific DTOs/view models for status, duplicate candidates, overview facts, and appointment scheduling identity instead of leaking Sea `Project` and Appointments `Reservation` details into every component.
2. Keep read-only overview queries separate from state-changing commands: overview components should aggregate, while `PatientEditForm`, `ProcedureDetails`, and `ReservationDetail` should own mutations.
3. Add validation and audit-friendly failure handling at the component boundary before service calls, then keep deeper service validation as the source of truth for concurrency/capacity rules.
4. Prefer extension points around medical abstractions (`PatientOverviewSection`, `MedicalProcedureAssignment`, `PatientAppointmentSlot`) so future clinical fields do not require rewriting stable Sea/Appointments components.
