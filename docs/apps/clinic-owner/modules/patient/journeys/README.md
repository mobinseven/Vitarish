# Clinic Owner Patient Journey Assessment

**Note: Blazor/Razor component references in this document were retired per ADR-0009.**

Date assessed: 2026-05-10

## Scope and method
This folder documents Clinic Owner patient journeys and the current implementation evidence behind them. The React interface coverage source of truth now lives in the [React interface coverage audit](../../../../../journeys/react-interface-coverage-audit.md), while the detailed journey narratives stay in the focused journey files.

Coverage levels in the React interface coverage audit:

- **implemented:** the user can complete the journey in the current UI and supporting services.
- **partial:** enough UI/service support exists for part of the journey, but one or more requested objectives are missing, indirect, or not patient-specific.
- **missing:** no clear user-facing journey exists for the requested objective.

## How to use this folder
1. Start with the [React interface coverage audit](../../../../../journeys/react-interface-coverage-audit.md) for journey-level React route/component evidence and known gaps.
2. Use the focused journey files for operator flow, decision rules, and domain-specific implementation detail.
3. Use the acceptance criteria section below to turn every partial or missing objective into scoped delivery work.

## Executive summary
- Patients profile management is operational for create/view/edit/delete, but search/filtering, patient lifecycle status, and duplicate prevention remain incomplete. See the [React interface coverage audit](../../../../../journeys/react-interface-coverage-audit.md).
- Patient procedures can be created and updated today, but provider assignment, explicit procedure status, procedure-level notes, and cancellation semantics remain incomplete. See the [React interface coverage audit](../../../../../journeys/react-interface-coverage-audit.md).
- Patient appointments support booking and status transitions, but rescheduling, provider/room assignment, richer filtering/views, and follow-up creation remain incomplete. See the [React interface coverage audit](../../../../../journeys/react-interface-coverage-audit.md).
- Patient check-in is an implemented guided flow with extension work around overflow, richer slot selection, and partial-success handling. See the [React interface coverage audit](../../../../../journeys/react-interface-coverage-audit.md).
- Patient overview is still assembled from multiple panels instead of a single read model, leaving upcoming/history separation, timeline, and patient flags incomplete. See the [React interface coverage audit](../../../../../journeys/react-interface-coverage-audit.md).

## Evidence highlights

- Clinic owner pages expose patient list, new, detail, edit, procedure, and procedure-event routes under `/owner/clinic/patients`.
- Patient data is represented through clinic patient wrappers around Sea `Project` records, with patient-specific fields stored in metadata and user linkage through `UserIds`.
- Procedures and event timelines are provided by the Sea module and embedded into patient pages.
- Patient appointment booking uses the Appointments reservation/session model and requires a linked user account.
- Reservation creation validates active sessions, booking windows, per-party limits, capacity, and occurrence availability.
- Owner check-in is a 3-step guided host flow at `/owner/clinic/check-in`.

## Journey map files

- [Patient Profile CRUD Journey](./patient-profile-crud-journey.md)
- [Patient Check-In Journey](./patient-check-in-journey.md)
- [Patient Procedure Journey](./patient-procedure-journey.md)
- [Patient Appointment Journey](./patient-appointment-journey.md)
- [Patient Overview Journey](./patient-overview-journey.md)

## Acceptance Criteria by Objective

Each objective below uses the same delivery schema: user-visible completion criteria, route/UI entry point, state mutation/query boundary, validation/error behavior, audit/logging expectation, and definition-of-done test coverage locations.

### Patients Profile Management (CRUD)

#### [Search and filter patients](./patient-profile-crud-journey.md)
- **User-visible completion criteria:** Operators can filter the patient list by status, linked user, key demographics, date-of-birth range, and missing-clinical-data flags without losing the existing global text search.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patients` (`/owner/clinic/patients`) via `ClinicOwnerPatientsPage.razor` and `PatientsGrid` filter controls.
- **Required state mutation/query boundary:** Extend the patient list read boundary behind `IAccessControlledProjectsService.GetListAsync` so filters are expressed as patient-specific query criteria rather than ad hoc UI-only filtering.
- **Validation/error behavior:** Invalid filter combinations are blocked inline; the UI explicitly shows when no patients match the active filters; query failures preserve the current filter state and show an operator-friendly retry/error message.
- **Audit/logging expectation:** No audit event is required for read-only filtering, but failed queries should emit standard telemetry/error logs.
- **Definition of done test coverage locations:** Unit tests in a mirrored clinic interface/component test area for `PatientsGrid`; integration coverage in `tests/Hosts/Main.Tests/Common/AppRoutesTests.cs` plus a future mirrored clinic owner query test area; UI coverage in a clinic owner patient-list flow suite for filter persistence and empty/error states.

#### [Manage patient status (active/inactive)](./patient-profile-crud-journey.md)
- **User-visible completion criteria:** Operators can see a patient lifecycle badge everywhere patient identity is shown and can change status explicitly between supported states without deleting the record.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` and `AppRoutes.Clinic.PatientEdit(patientId)` through `PatientDetails` and/or `PatientEditForm`.
- **Required state mutation/query boundary:** Add a dedicated patient status field to the patient read/write contract so status reads, updates, and list filters use one source of truth.
- **Validation/error behavior:** Unsupported transitions are blocked with explicit reasons; concurrent updates return a non-destructive retry message; status changes do not silently remove patients from visible lists.
- **Audit/logging expectation:** Record an audit event for status changes because they alter patient lifecycle visibility and downstream workflows.
- **Definition of done test coverage locations:** Unit tests in a mirrored patient form/details test area; integration coverage in a mirrored clinic owner patient command test area plus route coverage in `tests/Hosts/Main.Tests/Common/AppRoutesTests.cs`; UI coverage in a patient-details lifecycle-status flow suite.

#### [Prevent duplicate patient records](./patient-profile-crud-journey.md)
- **User-visible completion criteria:** Before create or update commits, operators see duplicate candidates based on normalized identity inputs and must explicitly confirm creating a new record when a likely match exists.
- **Required route/UI action entry point:** `AppRoutes.Clinic.PatientNew` and the create path launched from `AppRoutes.Clinic.Patients`, centered in `PatientEditForm`.
- **Required state mutation/query boundary:** Add a dedicated duplicate-candidate query ahead of create/update so duplicate review stays read-only until the operator confirms the mutation.
- **Validation/error behavior:** The form blocks save until duplicate review is resolved; failed duplicate queries show a recoverable error; confirmed overrides are explicit and never implicit.
- **Audit/logging expectation:** Log duplicate warnings and explicit operator overrides for traceability when a likely match is bypassed.
- **Definition of done test coverage locations:** Unit tests in a mirrored patient form test area for duplicate decision rules; integration coverage in a mirrored clinic owner patient create/update command test area; UI coverage in a patient-create duplicate-review flow suite.

### Patients Procedures Management

#### [Assign procedures to patients and providers](./patient-procedure-journey.md)
- **User-visible completion criteria:** Each procedure shows its owning patient and assigned provider(s), and operators can set or change provider assignment during create and maintenance flows.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` and `AppRoutes.Clinic.PatientProcedure(patientId, procedureId)` via `ProceduresGrid`, `ProcedureCreateDialog`, and `ProcedureDetails`.
- **Required state mutation/query boundary:** Keep patient assignment on the procedure record while adding a medical-specific provider assignment read/write boundary instead of reusing generic Sea event-role editing.
- **Validation/error behavior:** Missing or invalid provider assignments are surfaced inline; save failures preserve unsaved form values; unauthorized assignment attempts show an explicit permission error.
- **Audit/logging expectation:** Provider assignment changes should create auditable change events because they alter clinical responsibility.
- **Definition of done test coverage locations:** Unit tests in mirrored procedure dialog/details component tests; integration coverage in a mirrored clinic owner procedure command test area; UI coverage in patient-procedure assignment flows.

#### [Track procedure status](./patient-procedure-journey.md)
- **User-visible completion criteria:** Each procedure exposes a clear lifecycle status (for example planned, active, completed, cancelled) that is visible in the patient list and procedure detail pages without forcing users to infer status from event commits.
- **Required route/UI action entry point:** `AppRoutes.Clinic.PatientProcedure(patientId, procedureId)` and `AppRoutes.Clinic.PatientEvent(patientId, procedureId, eventId)` via `ProcedureDetails`, `ProceduresGrid`, and `EventsGrid`.
- **Required state mutation/query boundary:** Add a procedure lifecycle status read/write boundary that is separate from step/event history, while event commits remain step-level facts.
- **Validation/error behavior:** Invalid status transitions are rejected with human-readable guidance; status changes cannot orphan in-progress events; read failures fall back to a clearly unavailable status display.
- **Audit/logging expectation:** Procedure lifecycle changes should be audited because they change the medical work state.
- **Definition of done test coverage locations:** Unit tests in mirrored procedure workflow component tests; integration coverage in a mirrored procedure command/query test area; UI coverage in procedure-status transition flows from patient and procedure pages.

#### [Add procedure notes](./patient-procedure-journey.md)
- **User-visible completion criteria:** Operators can add and review procedure-level notes without mixing them with generic description text or forcing all notes into event timelines.
- **Required route/UI action entry point:** `AppRoutes.Clinic.PatientProcedure(patientId, procedureId)` with note entry from `ProcedureDetails` and contextual linkage to `EventItemsTimeline` when step-level notes are still needed.
- **Required state mutation/query boundary:** Introduce a dedicated procedure-notes read/write boundary or wrapper so procedure notes remain distinct from event attachments and generic procedure description fields.
- **Validation/error behavior:** Empty notes are blocked; save failures preserve draft text; read failures clearly distinguish missing notes from unavailable note history.
- **Audit/logging expectation:** Author and timestamp should be retained for procedure-note writes, even if a formal audit event is not emitted for every edit.
- **Definition of done test coverage locations:** Unit tests in mirrored procedure-details note components; integration coverage in a mirrored procedure note command/query area; UI coverage in a patient-procedure notes flow suite.

#### [Cancel procedures](./patient-procedure-journey.md)
- **User-visible completion criteria:** Operators can cancel or close a procedure with a reason, timestamp, and visible terminal status instead of deleting the record from the clinical history.
- **Required route/UI action entry point:** `AppRoutes.Clinic.PatientProcedure(patientId, procedureId)` through a dedicated cancel action in `ProcedureDetails`.
- **Required state mutation/query boundary:** Add an explicit cancel/close command and read model fields for terminal status, cancellation reason, and timestamp.
- **Validation/error behavior:** Cancellation requires a reason; already terminal procedures cannot be cancelled again; failures leave the procedure unchanged and surface a retryable message.
- **Audit/logging expectation:** Procedure cancellation must be audited because it ends clinical work and affects downstream care review.
- **Definition of done test coverage locations:** Unit tests in mirrored procedure-detail action tests; integration coverage in a mirrored procedure cancellation command test area; UI coverage in cancel-procedure confirmation and post-cancel state flows.

### Patients Appointments Management

#### [Edit/reschedule appointments](./patient-appointment-journey.md)
- **User-visible completion criteria:** Schedulers can open an existing reservation, choose a new slot intentionally, confirm the move, and see the updated appointment reflected immediately in patient and reservation views.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` and `AppRoutes.Clinic.OwnerReservation(reservationId)` via `PatientAppointmentsPanel` and `ReservationDetailDialog` / `ReservationDetail`.
- **Required state mutation/query boundary:** Expose the existing move/reschedule command boundary behind a dedicated reschedule workflow that separates read-only reservation detail from the state-changing move action.
- **Validation/error behavior:** The reschedule UI must block invalid slot selections, surface capacity/concurrency failures in operator language, and preserve the original reservation until the move succeeds.
- **Audit/logging expectation:** Reservation reschedules should be logged with before/after slot details because they change booked patient care.
- **Definition of done test coverage locations:** Unit tests in mirrored reservation-detail/reschedule component tests; integration coverage in a mirrored appointments command test area; UI coverage in patient-reservation reschedule flows.

#### [Assign providers and rooms](./patient-appointment-journey.md)
- **User-visible completion criteria:** Booking and reservation detail surfaces show assigned provider and room, and schedulers can set or update both during booking and subsequent maintenance.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` for patient booking plus owner shift/reservation surfaces under `AppRoutes.Clinic.OwnerShift(shiftId)` and `AppRoutes.Clinic.OwnerReservation(reservationId)`.
- **Required state mutation/query boundary:** Extend the scheduling model so shifts, occurrences, or reservations expose provider/room read and write boundaries instead of relying on UI-only labels.
- **Validation/error behavior:** Required provider/room rules are enforced before save; unavailable provider/room combinations are blocked with explicit conflict messaging; stale assignments recover cleanly.
- **Audit/logging expectation:** Provider/room assignment changes should be auditable because they change operational responsibility and capacity planning.
- **Definition of done test coverage locations:** Unit tests in mirrored booking and shift-editor component tests; integration coverage in mirrored appointment scheduling command/query tests; UI coverage in booking flows with provider/room selection and edit scenarios.

#### [Prevent double-bookings](./patient-appointment-journey.md)
- **User-visible completion criteria:** The scheduler sees provider, room, and patient conflict warnings before booking, and the final reservation command still authoritatively blocks conflicting bookings.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` and `AppRoutes.Clinic.CheckIn` via `PatientAppointmentBookingDialog` and `PatientCheckInShiftStep`.
- **Required state mutation/query boundary:** Keep reservation conflict checks in the reserve command boundary while extending the underlying validation model to include provider and room conflicts once those fields exist.
- **Validation/error behavior:** Conflict warnings are explicit before submit; command-time conflicts never create partial bookings; users receive actionable retry guidance when the last available slot is lost.
- **Audit/logging expectation:** Conflict rejections should be logged for operational diagnostics, even when they do not require a formal audit event.
- **Definition of done test coverage locations:** Unit tests in mirrored booking/check-in component tests; integration coverage in mirrored reservation command conflict tests; UI coverage in appointment-booking and check-in conflict scenarios.

#### [View appointments in calendar/list view](./patient-appointment-journey.md)
- **User-visible completion criteria:** Patient-specific appointment management supports both a chronological list and a calendar/timeline-style view without leaving patient context.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` with optional drill-through to `AppRoutes.Clinic.OwnerReservations`.
- **Required state mutation/query boundary:** Reuse reservation read models behind a patient-context query that can drive both list and calendar presentations from the same source data.
- **Validation/error behavior:** Unsupported date ranges and empty periods show explicit empty states; switching views never mutates reservation state; load failures are localized to the appointments surface.
- **Audit/logging expectation:** No audit event is required for view switching; standard telemetry should capture repeated load failures.
- **Definition of done test coverage locations:** Unit tests in mirrored patient appointments panel/view-toggle tests; integration coverage in mirrored reservation query tests; UI coverage in patient appointment calendar/list toggling flows.

#### [Search and filter appointments](./patient-appointment-journey.md)
- **User-visible completion criteria:** Schedulers can filter patient appointments by status, date range, upcoming/history, shift, provider, and room without leaving patient context.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` with parity where appropriate on `AppRoutes.Clinic.OwnerReservations`.
- **Required state mutation/query boundary:** Extend the reservation query/filter boundary so patient-context filters are backed by explicit query parameters rather than client-side list trimming.
- **Validation/error behavior:** Invalid filter ranges are blocked inline; no-results states remain explicit; query failures preserve entered filters and offer retry.
- **Audit/logging expectation:** No audit event is required for read-only filters; failed reservation queries should emit normal telemetry/error logs.
- **Definition of done test coverage locations:** Unit tests in mirrored patient appointments filter component tests; integration coverage in mirrored reservation query tests; UI coverage in patient appointment filter persistence and empty-state flows.

#### [Create follow-up appointments](./patient-appointment-journey.md)
- **User-visible completion criteria:** From a completed or cancelled appointment, the scheduler can launch a follow-up flow that keeps patient context, prefills a recommended date range, and creates the new reservation without restarting from scratch.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` and/or `AppRoutes.Clinic.OwnerReservation(reservationId)` from `ReservationDetail`.
- **Required state mutation/query boundary:** Add a follow-up creation boundary that reuses booking primitives while carrying forward patient identity and follow-up context as explicit inputs.
- **Validation/error behavior:** Follow-up suggestions remain editable; invalid suggested ranges are blocked; reservation failures do not alter the source appointment.
- **Audit/logging expectation:** Follow-up creation should be logged as a distinct booking action linked back to the source reservation when applicable.
- **Definition of done test coverage locations:** Unit tests in mirrored reservation-detail/follow-up component tests; integration coverage in mirrored follow-up booking command tests; UI coverage in completed-appointment to follow-up creation flows.

### Patients Overview

#### [View upcoming appointments](./patient-overview-journey.md)
- **User-visible completion criteria:** The patient overview clearly surfaces the next appointment and an upcoming-appointments section separate from historical activity.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` through `ClinicOwnerPatientPage.razor` and a promoted `PatientOverviewPanel` summary surface.
- **Required state mutation/query boundary:** Add a read-only patient overview query that derives `NextAppointment` and `UpcomingAppointments` instead of making the page infer them from a flat reservation list.
- **Validation/error behavior:** Missing upcoming appointments render an explicit empty state; query failures do not hide the rest of the patient page; stale data is clearly marked unavailable rather than blank.
- **Audit/logging expectation:** No audit event is required for this read-only summary; overview query failures should emit standard telemetry.
- **Definition of done test coverage locations:** Unit tests in mirrored patient overview component tests; integration coverage in a mirrored patient overview query test area; UI coverage in patient-overview upcoming-appointment states.

#### [View appointment history](./patient-overview-journey.md)
- **User-visible completion criteria:** Historical appointments are grouped into a dedicated history section that is distinct from upcoming work and supports quick review of prior visit outcomes.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` through the patient overview surface.
- **Required state mutation/query boundary:** Extend the patient overview read model with an `AppointmentHistory` boundary that reuses reservation reads without mixing upcoming and historical items in one list.
- **Validation/error behavior:** Empty history states are explicit; partial load failures isolate to the history surface; date ordering stays deterministic.
- **Audit/logging expectation:** No audit event is required for read-only history; repeated history-query failures should be logged for diagnostics.
- **Definition of done test coverage locations:** Unit tests in mirrored patient overview/history component tests; integration coverage in a mirrored overview query test area; UI coverage in patient-overview history rendering flows.

#### [View procedures history/status](./patient-overview-journey.md)
- **User-visible completion criteria:** The overview shows current procedure statuses and a concise recent-procedure history without forcing the user to drill into the full procedures grid first.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` with drill-through to `AppRoutes.Clinic.PatientProcedure(patientId, procedureId)`.
- **Required state mutation/query boundary:** Read procedure summaries through the patient overview query boundary, including explicit status rollups once procedure lifecycle state exists.
- **Validation/error behavior:** Missing procedure data renders a clear unavailable state; stale status cannot be shown as current when the underlying procedure query fails.
- **Audit/logging expectation:** No audit event is required for the read-only overview, but source procedure status mutations should already be audited by their owning flow.
- **Definition of done test coverage locations:** Unit tests in mirrored patient overview/procedure summary tests; integration coverage in a mirrored patient overview query test area; UI coverage in overview-to-procedure drill-through flows.

#### [View activity feed/timeline](./patient-overview-journey.md)
- **User-visible completion criteria:** The patient overview includes a single chronological feed that combines relevant profile, procedure, and appointment activity in one place.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` with drill-through to procedure-event detail via `AppRoutes.Clinic.PatientEvent(patientId, procedureId, eventId)` when needed.
- **Required state mutation/query boundary:** Build a read-only patient activity query that aggregates cross-module events without pushing mutation logic into the overview page.
- **Validation/error behavior:** Timeline gaps are labeled honestly; partial-source failures identify which feed segments are unavailable; read-only timeline interactions never mutate clinical state.
- **Audit/logging expectation:** The overview feed consumes existing auditable events; no extra audit event is needed for viewing, but aggregation failures should be logged.
- **Definition of done test coverage locations:** Unit tests in mirrored patient overview timeline component tests; integration coverage in a mirrored patient activity query area; UI coverage in timeline rendering and drill-through flows.

#### [View important patient flags/notes](./patient-overview-journey.md)
- **User-visible completion criteria:** Safety-critical flags and important notes appear in a dedicated top-of-page summary region before the user scans detailed sections.
- **Required route/UI action entry point:** `AppRoutes.Clinic.Patient(patientId)` through `PatientOverviewPanel` or an equivalent safety-first summary region in `ClinicOwnerPatientPage.razor`.
- **Required state mutation/query boundary:** Extend the patient overview read model so risk flags, allergies, medications, and important notes are surfaced as prioritized summary data rather than buried in generic profile details.
- **Validation/error behavior:** Missing flag data is explicitly marked unavailable; conflicting flag inputs are not silently merged; read failures never suppress previously loaded high-priority alerts without notice.
- **Audit/logging expectation:** Viewing flags does not need auditing, but any future flag mutations should emit auditable change records because they affect clinical risk awareness.
- **Definition of done test coverage locations:** Unit tests in mirrored patient overview flag-summary components; integration coverage in a mirrored overview query test area; UI coverage in patient flag visibility and alert-priority states.
