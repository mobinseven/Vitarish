# Clinic Web Host Journey Implementation Plan

Status: Accepted  
Owner scope: Clinic owner app / web host  
Source journeys:

- `docs/apps/clinic-owner/journeys/`
- `docs/apps/clinic-owner/journeys/implementation-coverage-matrix.md`
- `docs/apps/clinic-owner/modules/patient/journeys/`
- `docs/apps/clinic-owner/modules/patient/journeys/implementation-matrix.md`

Static implementation evidence:

- `src/Hosts/Main/Common/Routes/ClinicRoutes.cs`
- `src/Hosts/Main/Pages/Clinic/**`
- `src/Apps/Clinic/Interface/Components/**`
- `src/Apps/Clinic/Interface/Models/**`

Applied skills:

- `platform-artifact-placement-steward`
- `platform-feature-ui-integration-steward`

## 1. Objective

Implement the documented clinic app journeys in the web host as components and pages while preserving platform architecture boundaries, module ownership, CQRS separation, frontend/backend isolation, and one source of truth per state model.

This plan consolidates the static journey findings, current Clinic app/page/component scan, implementation gaps, task backlog, route plan, placement plan, and per-artifact architecture expectations into one implementation reference.

## 2. Current State Summary

The Clinic app currently has Razor host pages under `src/Hosts/Main/Pages/Clinic/**` and reusable Razor components under `src/Apps/Clinic/Interface/Components/**`. The journey coverage matrix shows route and component evidence for most clinic journeys, but several journeys are partial, placeholders, or related-only rather than fully implemented.

Current route source of truth is `src/Hosts/Main/Common/Routes/ClinicRoutes.cs`.

Current reusable Clinic interface component areas include:

- `Components/Patients`: patient lookup, advanced search, grid, summary/detail, edit form, find-or-create.
- `Components/CheckIn`: check-in context, shift step, confirmation step.
- `Components/Appointments`: patient appointment panel and booking dialog.
- `Components/Reminders`: patient reminders panel.

Current host pages include:

- Owner dashboard and check-in pages.
- Patient list/new/detail/edit/procedure/event pages.
- Owner appointment reservation/shift/service catalog pages.
- User reservation/shift pages.
- Owner reminders, settings users, and statistics pages.

Important current gaps:

- Clinic statistics page is a placeholder.
- Clinic settings shell route exists, but no complete settings landing page is implemented.
- Appointment settings page currently renders service type/service/provider lists directly and is effectively a placeholder shell.
- Patient profile duplicate prevention and patient active/inactive status are missing.
- Patient overview exists but is not a dedicated safety-first overview source of truth.
- Patient appointment reschedule/follow-up/provider-room workflows are partial or missing.
- Procedure lifecycle, cancellation, provider assignment, and unified notes are partial or missing.
- Patient reminders panel exists but is not currently active in the patient detail page.

## 3. Architecture Boundaries

### 3.1 Dependency Direction

```txt
Web host page
  -> Clinic feature component
    -> Clinic feature hook / use-case adapter
      -> generated contract-safe API client or generated query hook
        -> HTTP API contract
```

Web host pages and components must not import backend `Domain`, `Application`, `Infrastructure`, server `Api` projects, persistence adapters, EF contexts, or server-only modules.

### 3.2 State Ownership

| State or rule | Source of truth |
| --- | --- |
| Clinic web routes | React/web route constants mirroring `ClinicRoutes.cs` paths |
| Patient server state | Clinic/Sea API read models through generated client wrappers |
| Appointment server state | Appointments API/generated client wrappers |
| Procedure/event state | Sea API/generated client wrappers |
| Reminder state | Communications/reminders API/generated client wrappers |
| Check-in wizard state | Check-in feature workflow hook |
| Form validation state | Feature-level form model plus API validation mapping |
| Cache invalidation | Feature mutation hooks only |
| Statistics | Dedicated Clinic statistics read model/API, not UI aggregation |

### 3.3 Command / Query Split

- Query hooks load and return data only.
- Command hooks mutate and then invalidate or update query state.
- UI components render state and invoke callbacks only.
- Pages parse route parameters and compose feature surfaces only.

### 3.4 Failure Handling

Every feature hook must expose explicit failure state:

- Loading and empty state.
- Validation errors.
- Not found.
- Forbidden/unauthorized.
- Conflict/duplicate.
- Partial success for multi-step workflows such as check-in booking plus SMS confirmation.
- Retry behavior where safe.

## 4. Proposed Web Host Placement

If a React web host already exists, use its established root. If not, use the platform convention below when the host is introduced:

```txt
src/Hosts/Web/src/
  routes/
    clinicRoutes.ts
  pages/
    clinic/
      owners/
      users/
  features/
    clinic/
      dashboard/
      patients/
      check-in/
      appointments/
      reminders/
      settings/
      statistics/
```

### 4.1 Feature Ownership

| Capability | Owner | Proposed feature folder |
| --- | --- | --- |
| Owner dashboard | Clinic app | `features/clinic/dashboard` |
| Patient list/profile/search | Clinic + Sea-backed patient mapping | `features/clinic/patients` |
| Patient identification lookup | Clinic patient capability | `features/clinic/patients/identification` |
| Patient advanced search | Clinic patient capability | `features/clinic/patients/search` |
| Patient find-or-create | Clinic patient workflow | `features/clinic/patients/find-or-create` |
| Check-in | Clinic workflow over patient and appointments | `features/clinic/check-in` |
| Patient appointments panel | Clinic patient appointment composition | `features/clinic/patients/appointments` |
| Owner appointments | Appointments module hosted under Clinic | `features/clinic/appointments` |
| User appointments | Appointments module hosted under Clinic user routes | `features/clinic/user-appointments` |
| Procedures/events | Sea module hosted under Clinic patient routes | `features/clinic/patients/procedures` |
| Reminders | Communications hosted under Clinic | `features/clinic/reminders`, `features/clinic/patients/reminders` |
| Settings users | Tenant/Sea users surfaced in Clinic settings | `features/clinic/settings/users` |
| Statistics | Clinic aggregate read model | `features/clinic/statistics` |

## 5. Route Inventory

| Journey | Route | Current Razor page | Proposed web page |
| --- | --- | --- | --- |
| Owner dashboard navigation | `/owner/clinic` | `ClinicOwnerDashboardPage.razor` | `pages/clinic/owners/ClinicOwnerDashboardPage.tsx` |
| Patient check-in | `/owner/clinic/check-in` | `ClinicOwnerPatientCheckInPage.razor` | `pages/clinic/owners/ClinicPatientCheckInPage.tsx` |
| Patient list/search | `/owner/clinic/patients` | `ClinicOwnerPatientsPage.razor` | `pages/clinic/owners/patients/ClinicPatientsPage.tsx` |
| Patient create | `/owner/clinic/patients/new` | `ClinicOwnerPatientNewPage.razor` | `pages/clinic/owners/patients/ClinicPatientNewPage.tsx` |
| Patient detail/overview | `/owner/clinic/patients/{patientId}` | `ClinicOwnerPatientPage.razor` | `pages/clinic/owners/patients/ClinicPatientPage.tsx` |
| Patient edit | `/owner/clinic/patients/{patientId}/edit` | `ClinicOwnerPatientEditPage.razor` | `pages/clinic/owners/patients/ClinicPatientEditPage.tsx` |
| Patient procedure detail | `/owner/clinic/patients/{patientId}/procedures/{procedureId}` | `ClinicOwnerPatientProcedurePage.razor` | `pages/clinic/owners/patients/ClinicPatientProcedurePage.tsx` |
| Patient procedure event | `/owner/clinic/patients/{patientId}/procedures/{procedureId}/events/{eventId}` | `ClinicOwnerPatientEventPage.razor` | `pages/clinic/owners/patients/ClinicPatientProcedureEventPage.tsx` |
| Settings shell | `/owner/clinic/settings` | route constant only | `pages/clinic/owners/settings/ClinicSettingsPage.tsx` |
| Settings users | `/owner/clinic/settings/users` | `ClinicOwnerSettingsUsersPage.razor` | `pages/clinic/owners/settings/ClinicSettingsUsersPage.tsx` |
| Statistics | `/owner/clinic/settings/statistics` | placeholder page | `pages/clinic/owners/settings/ClinicStatisticsPage.tsx` |
| Owner reminders | `/owner/clinic/reminders` | `ClinicOwnerRemindersPage.razor` | `pages/clinic/owners/reminders/ClinicOwnerRemindersPage.tsx` |
| Appointment settings | `/owner/clinic/appointment-settings` | `ClinicOwnerAppointmentsSettings.razor` | `pages/clinic/owners/appointments/ClinicAppointmentSettingsPage.tsx` |
| Owner reservations | `/owner/clinic/reservations` | `ClinicOwnerReservationsPage.razor` | `pages/clinic/owners/appointments/ClinicOwnerReservationsPage.tsx` |
| Owner reservation detail | `/owner/clinic/reservations/{reservationId}` | `ClinicOwnerReservationDetailPage.razor` | `pages/clinic/owners/appointments/ClinicOwnerReservationDetailPage.tsx` |
| Owner shifts | `/owner/clinic/shifts` | `ClinicOwnerShiftsPage.razor` | `pages/clinic/owners/appointments/ClinicOwnerShiftsPage.tsx` |
| Owner shift create | `/owner/clinic/shifts/new`, `/owner/clinic/shifts/new/{dayOfWeek}` | `ClinicOwnerShiftCreatePage.razor` | `pages/clinic/owners/appointments/ClinicOwnerShiftCreatePage.tsx` |
| Owner shift detail | `/owner/clinic/shifts/{shiftId}` | `ClinicOwnerShiftDetailPage.razor` | `pages/clinic/owners/appointments/ClinicOwnerShiftDetailPage.tsx` |
| Owner shift edit | `/owner/clinic/shifts/{shiftId}/edit` | `ClinicOwnerShiftEditPage.razor` | `pages/clinic/owners/appointments/ClinicOwnerShiftEditPage.tsx` |
| Owner shift occurrence | `/owner/clinic/shifts/{shiftId}/occurrence/{occurrenceDate}` | `ClinicOwnerShiftOccurrencePage.razor` | `pages/clinic/owners/appointments/ClinicOwnerShiftOccurrencePage.tsx` |
| Service types | `/owner/clinic/service-types`, `/owner/clinic/service-types/{serviceTypeId}` | service type pages | `pages/clinic/owners/appointments/service-types/*` |
| Services | `/owner/clinic/services`, `/owner/clinic/services/{serviceId}` | service pages | `pages/clinic/owners/appointments/services/*` |
| Service providers | `/owner/clinic/service-providers`, `/owner/clinic/service-providers/{serviceProviderId}` | provider pages | `pages/clinic/owners/appointments/service-providers/*` |
| User reservations | `/user/clinic/reservations` | `ClinicUserReservationsPage.razor` | `pages/clinic/users/ClinicUserReservationsPage.tsx` |
| User reservation detail | `/user/clinic/reservations/{reservationId}` | `ClinicUserReservationDetailPage.razor` | `pages/clinic/users/ClinicUserReservationDetailPage.tsx` |
| User shifts | `/user/clinic/shifts` | `ClinicUserShiftsPage.razor` | `pages/clinic/users/ClinicUserShiftsPage.tsx` |
| User shift detail | `/user/clinic/shifts/{shiftId}` | `ClinicUserShiftDetailPage.razor` | `pages/clinic/users/ClinicUserShiftDetailPage.tsx` |

## 6. Implementation Backlog

### 6.1 Foundation Tasks

#### Task: Extend centralized web clinic route constants

1. Extend the existing centralized routes file at src/Hosts/Web/src/app/routes.ts.
2. Mirror all paths from src/Hosts/Main/Common/Routes/ClinicRoutes.cs within the Clinic route object.
3. Export builders/functions for parameterized routes: patient, patient edit, procedure, event, reservation, shift, shift edit, shift occurrence, service type, service, service provider, user reservation, and user shift.
4. Keep route casing and kebab-case identical to server route constants.
5. Ensure src/Hosts/Web/src/app/routes.ts remains the single route source of truth used by web clinic pages/components.

#### Task: Add owner and user route guards

1. Add tenant-owner guard/layout for `/owner/clinic/**` routes.
2. Add tenant-user guard/layout for `/user/clinic/**` routes.
3. Match the Razor authorization intent from current Clinic `_Imports.razor` files.
4. Keep authorization enforcement at route/layout and API levels; do not rely on UI hiding alone.

#### Task: Create feature hook wrappers over generated clients

1. Create hooks under `features/clinic/<capability>/hooks`.
2. Allow generated client imports only inside feature hooks or feature API adapters.
3. Keep pages and presentational components dependent on feature contracts only.
4. Centralize query invalidation in mutation hooks.
5. Centralize ProblemDetails and validation error mapping in feature hooks.

#### Task: Create feature view model mappers

1. Add capability-owned mappers under `features/clinic/<capability>/api` or `features/clinic/<capability>/models`.
2. Map generated DTOs to UI-ready view models.
3. Keep read mapping and write mapping explicit.
4. Do not create generic shared mapper utilities until two real consumers require promotion.

### 6.2 Owner Dashboard and Settings

#### Task: Implement clinic owner dashboard navigation

1. Create `pages/clinic/owners/ClinicOwnerDashboardPage.tsx`.
2. Create `features/clinic/dashboard/components/ClinicOwnerDashboard.tsx`.
3. Render grouped navigation sections: Patients, Check-in, Appointments, Reminders, Settings, Service catalog, and Statistics.
4. Keep dashboard as navigation composition only unless a current journey requires data.

#### Task: Implement clinic settings landing page

1. Create `pages/clinic/owners/settings/ClinicSettingsPage.tsx`.
2. Create `features/clinic/settings/components/ClinicSettingsDashboard.tsx`.
3. Link to Users, Appointment Settings, Statistics, reminders configuration if supported, and service catalog settings.
4. Do not load heavy grids on the settings shell.

#### Task: Implement appointment settings landing page

1. Create `pages/clinic/owners/appointments/ClinicAppointmentSettingsPage.tsx`.
2. Create `features/clinic/appointments/settings/components/AppointmentSettingsDashboard.tsx`.
3. Render cards/links for Service Types, Services, Service Providers, and Shift Policies if supported.
4. Avoid loading all service catalog lists on entry unless lightweight counts already exist.

#### Task: Implement clinic statistics from a dedicated read model

1. Identify or create a Clinic statistics read contract/API endpoint before UI implementation.
2. Create `pages/clinic/owners/settings/ClinicStatisticsPage.tsx`.
3. Create `features/clinic/statistics/components/ClinicStatisticsDashboard.tsx`.
4. Create `features/clinic/statistics/hooks/useClinicStatistics.ts`.
5. Display only contract-backed metrics.
6. If no endpoint exists, render a clear unavailable state rather than aggregating from multiple list endpoints in UI.

### 6.3 Patient Foundation

#### Task: Implement patient list and selection surface

1. Create pages/clinic/owners/patients/ClinicPatientsPage.tsx.
2. Create features/clinic/patients/components/PatientListSurface.tsx.
3. Create features/clinic/patients/components/PatientList.tsx.
4. Migrate and refactor the existing src/Hosts/Web/src/features/clinic/hooks/usePatientList.ts and src/Hosts/Web/src/features/clinic/api/patientApi.ts into the new capability folder structure under features/clinic/patients/.
5. Support list, search text, add patient, and click patient parity first.
6. Expose UI-ready patient summaries from the hook.
7. Keep generated DTO details out of components.

#### Task: Implement patient identification lookup workflow

1. Create `features/clinic/patients/identification/components/PatientIdentificationLookup.tsx`.
2. Create child components for input, found, not-found, and failure states.
3. Create `features/clinic/patients/identification/hooks/usePatientIdentificationLookup.ts`.
4. Model states: idle, loading, found, not found, failure.
5. Expose lookup, retry, and select callbacks through feature-level contracts.
6. Reuse this capability from check-in and find-or-create.

#### Task: Implement patient advanced search

1. Create `features/clinic/patients/search/components/PatientAdvancedSearch.tsx`.
2. Create form, results, and panel components.
3. Create `features/clinic/patients/search/hooks/usePatientAdvancedSearch.ts`.
4. Support query/name, identification code, date of birth, phone, and email.
5. Normalize search criteria in the feature hook or mapper.
6. Do not import generated clients from form or results components.

#### Task: Implement patient create page and form

1. Create `pages/clinic/owners/patients/ClinicPatientNewPage.tsx`.
2. Create `features/clinic/patients/profile/components/PatientProfileForm.tsx`.
3. Create `features/clinic/patients/profile/hooks/useCreatePatientProfile.ts`.
4. Use a UI form model distinct from generated write DTOs.
5. Map form model to API request in the hook/adapter.
6. On success, navigate to the patient detail route.
7. On cancel, navigate back to the patient list route.
8. Handle validation and backend errors explicitly.

#### Task: Implement patient edit page

1. Create `pages/clinic/owners/patients/ClinicPatientEditPage.tsx`.
2. Reuse `PatientProfileForm.tsx`.
3. Create `features/clinic/patients/profile/hooks/usePatientProfileEditor.ts`.
4. Load patient by `patientId` in the hook.
5. Expose patient, form, save, cancel, loading, saving, and errors.
6. On save success, invalidate patient detail/list queries and navigate to patient detail.

#### Task: Implement explicit patient delete or archive action boundary

1. Inspect generated/API contract availability for patient delete, project delete, archive, or status commands.
2. If only delete exists, create `features/clinic/patients/profile/components/PatientDeleteAction.tsx` and `hooks/useDeletePatient.ts`.
3. Require strict confirmation and irreversible-action messaging.
4. On success, invalidate patient queries and navigate to patient list.
5. If archive/status is required, add or consume a backend contract first.
6. Do not simulate archive state in frontend-only state.

#### Task: Implement patient find-or-create workflow

1. Create `features/clinic/patients/find-or-create/components/PatientFindOrCreate.tsx`.
2. Create `features/clinic/patients/find-or-create/hooks/usePatientFindOrCreate.ts`.
3. Compose identification lookup, advanced search, and profile form.
4. Model workflow states: lookup, advanced search, duplicate review, create, selected.
5. Accept `onPatientSelected` and `onPatientCreated` callbacks.
6. Keep create mutation in the patient profile hook; the workflow hook owns orchestration state only.

### 6.4 Patient Overview

#### Task: Implement patient detail overview page

1. Create `pages/clinic/owners/patients/ClinicPatientPage.tsx`.
2. Create `features/clinic/patients/overview/components/PatientOverviewPageSurface.tsx`.
3. Create `PatientSummaryCard.tsx`, `PatientCriticalSummary.tsx`, `PatientQuickActions.tsx`, and `PatientOverviewSections.tsx`.
4. Compose profile summary, procedure list, appointment panel, and reminders panel if enabled.
5. Create `features/clinic/patients/overview/hooks/usePatientOverview.ts`.
6. Keep appointments, procedures, and reminders data in their own hooks; do not duplicate server state in overview state.
7. Add tabs or sections only when each has a current data source.

#### Task: Implement patient reminders panel

1. Create `features/clinic/patients/reminders/components/PatientRemindersPanel.tsx`.
2. Create `features/clinic/patients/reminders/hooks/usePatientReminders.ts`.
3. Wrap generated reminders query/mutation hooks by patient source ID.
4. Render active/upcoming reminders and a create action equivalent to the current reminder action.
5. Add the panel to patient overview only when API data is available or a feature flag enables it.

### 6.5 Check-In

#### Task: Implement patient check-in wizard

1. Create `pages/clinic/owners/ClinicPatientCheckInPage.tsx`.
2. Create `features/clinic/check-in/components/PatientCheckInWizard.tsx`.
3. Create step components for find, shift selection, and confirmation.
4. Create `features/clinic/check-in/hooks/usePatientCheckInWorkflow.ts`.
5. Keep selected patient, selected shift/occurrence, booking outcome, notification outcome, and operational warning in wizard state.
6. Reuse `PatientFindOrCreate` for the first step.
7. Reuse appointment/shift hooks for slot loading and booking.
8. Represent partial success explicitly.

#### Task: Implement check-in SMS confirmation action

1. Locate or generate the Communications/notification client hook for sending check-in confirmation SMS.
2. Create `features/clinic/check-in/hooks/useSendCheckInConfirmation.ts`.
3. Expose send, sending, sent, and error states.
4. Render SMS action only when patient/reservation/contact prerequisites are met.
5. Treat SMS failure as recoverable if booking already succeeded.

### 6.6 Patient Appointments

#### Task: Implement patient appointment panel and booking dialog

1. Create `features/clinic/patients/appointments/components/PatientAppointmentsPanel.tsx`.
2. Create `PatientAppointmentBookingDialog.tsx`, `PatientAppointmentList.tsx`, and `PatientAppointmentDetailDialog.tsx`.
3. Create `features/clinic/patients/appointments/hooks/usePatientAppointments.ts`.
4. Create `features/clinic/patients/appointments/hooks/useBookPatientAppointment.ts`.
5. Load reservations by patient/party id through feature hooks.
6. Separate upcoming and historical appointments where data supports it.
7. Let users choose a visible slot/occurrence when slot data is available.
8. Invalidate patient appointment and relevant owner appointment queries centrally.

### 6.7 Owner Appointment Management

#### Task: Implement owner reservations pages

1. Create `pages/clinic/owners/appointments/ClinicOwnerReservationsPage.tsx`.
2. Create `pages/clinic/owners/appointments/ClinicOwnerReservationDetailPage.tsx`.
3. Create `features/clinic/appointments/reservations/components/OwnerReservationsList.tsx`.
4. Create `features/clinic/appointments/reservations/hooks/useOwnerReservations.ts`.
5. Create `features/clinic/appointments/reservations/hooks/useOwnerReservationDetail.ts`.
6. Keep status/cancel/delete mutations in feature hooks.

#### Task: Implement owner shift management pages

1. Create owner shift list, create, detail, edit, and occurrence pages under `pages/clinic/owners/appointments/`.
2. Create shift components under `features/clinic/appointments/shifts/components/`.
3. Create hooks for list, detail, save, and occurrence reservation views.
4. Support optional `dayOfWeek` route parameter for create.
5. Parse `occurrenceDate` once in the page and pass typed values down.
6. Invalidate shift and reservation queries centrally.

#### Task: Implement service type management pages

1. Create service type list/detail pages under `pages/clinic/owners/appointments/service-types/`.
2. Create service type list/detail/form components.
3. Create hooks for list, detail, and save.
4. Keep Appointments service-catalog contracts behind feature hooks.

#### Task: Implement service management pages

1. Create service list/detail pages under `pages/clinic/owners/appointments/services/`.
2. Create service list/detail/form components.
3. Create hooks for list, detail, and save.
4. Load service type choices through service-catalog hooks rather than duplicating fetch logic.

#### Task: Implement service provider management pages

1. Create provider list/detail pages under `pages/clinic/owners/appointments/service-providers/`.
2. Create provider list/detail/form components.
3. Create hooks for list, detail, and save.
4. Keep provider selection isolated from future provider/room assignment until shift/reservation contracts support those assignments.

### 6.8 User Appointment Management

#### Task: Implement user shift browsing and booking

1. Create `pages/clinic/users/ClinicUserShiftsPage.tsx`.
2. Create `pages/clinic/users/ClinicUserShiftDetailPage.tsx`.
3. Create `features/clinic/user-appointments/shifts/components/UserShiftsList.tsx` and `UserShiftDetail.tsx`.
4. Create `useUserClinicShifts` and `useUserClinicShiftBooking` hooks.
5. Invalidate user reservations and user shifts after reserve/cancel mutations.
6. Keep user booking UI distinct from owner shift management UI.

#### Task: Implement user reservation management

1. Create `pages/clinic/users/ClinicUserReservationsPage.tsx`.
2. Create `pages/clinic/users/ClinicUserReservationDetailPage.tsx`.
3. Create `features/clinic/user-appointments/reservations/components/UserReservationsList.tsx` and `UserReservationDetail.tsx`.
4. Create `useUserClinicReservations` and `useUserClinicReservationDetail` hooks.
5. Provide navigation to user shifts for new booking.
6. Keep cancellation mutation and error handling in feature hooks.

### 6.9 Procedures and Events

#### Task: Implement patient procedure management

1. Create `pages/clinic/owners/patients/ClinicPatientProcedurePage.tsx`.
2. Create `features/clinic/patients/procedures/components/PatientProceduresPanel.tsx`, `PatientProcedureDetail.tsx`, and `PatientProcedureEventsList.tsx`.
3. Create `usePatientProcedures`, `usePatientProcedureDetail`, and `useSavePatientProcedure` hooks.
4. Use generated Sea/procedure client wrappers through feature hooks.
5. Support current parity: create procedure, open procedure, edit generic details, delete if supported, list events.
6. Do not add lifecycle states, cancellation reason, or provider assignment until backend contracts expose them.

#### Task: Implement patient procedure event page

1. Create `pages/clinic/owners/patients/ClinicPatientProcedureEventPage.tsx`.
2. Create `features/clinic/patients/procedures/events/components/PatientProcedureEventDetail.tsx`.
3. Create `features/clinic/patients/procedures/events/components/PatientProcedureEventTimeline.tsx`.
4. Create `usePatientProcedureEventDetail` and `usePatientProcedureEventTimeline` hooks.
5. Route delete/back navigation to the parent patient procedure route.
6. Keep timeline item mutations in event-specific hooks.

### 6.10 Reminders

#### Task: Implement owner reminders page

1. Create `pages/clinic/owners/reminders/ClinicOwnerRemindersPage.tsx`.
2. Create `features/clinic/reminders/components/ClinicOwnerRemindersGrid.tsx`.
3. Create `features/clinic/reminders/hooks/useClinicOwnerReminders.ts`.
4. Support listing, filtering, opening details, and mutations only where generated contracts exist.
5. Do not mix patient reminder panel state with owner reminders grid state except through shared client wrappers.

### 6.11 Settings Users

#### Task: Implement clinic settings users page

1. Create `pages/clinic/owners/settings/ClinicSettingsUsersPage.tsx`.
2. Create `features/clinic/settings/users/components/ClinicSettingsUsersSurface.tsx`.
3. Create `features/clinic/settings/users/hooks/useClinicSettingsUsers.ts`.
4. Wrap tenant/Sea user generated queries through a clinic settings feature hook only if route-specific mapping is needed.
5. Keep user account/business rules outside the clinic feature.

## 7. Implementation Sequence

1. Foundation route constants, route registration, authorization layouts, and generated-client feature hook conventions.
2. Owner dashboard and settings shells.
3. Patient list, detail loading, create/edit form, identification lookup, advanced search, and find-or-create.
4. Check-in wizard and SMS confirmation.
5. Patient overview panels: appointments, procedures, reminders.
6. Owner appointments: reservations, shifts, service catalog.
7. User appointments: shift browsing/booking and reservation management.
8. Procedure/event detail and timeline parity.
9. Statistics after a dedicated read model/API is confirmed.

## 8. Deferred or Contract-Dependent Work

Do not implement these as frontend-only behavior:

- Patient active/inactive status.
- Patient duplicate prevention beyond current lookup/search unless a duplicate candidate or guard contract exists.
- Provider and room assignment for appointments.
- Provider conflict and room conflict double-booking prevention.
- Procedure lifecycle states such as planned, in progress, completed, cancelled.
- Procedure cancellation reason and audit trail.
- Consolidated patient-level activity feed across profile, appointments, procedures, and reminders.
- Statistics metrics without a dedicated read model.

## 9. Validation Plan

Run these after implementation:

```bash
python tools/architecture/validate_interface_boundaries.py
python tools/architecture/validate_module_api_boundaries.py
```

Also run the React host's package-specific commands once the actual React host is identified:

```bash
npm run typecheck
npm run lint
npm test
```

Add or run focused tests for:

- Route builders.
- Feature hook error mapping.
- Mutation invalidation behavior.
- Check-in partial-success states.
- Patient form validation mapping.
- Appointment booking/reservation mutation success and failure states.

## 10. Per-Artifact Rule Audit Template

Use this audit for every implementation artifact:

```txt
Artifact: <path/name>
Type: <page/component/hook/mapper/route contract>
Responsibility: <one sentence>

SRP:                    Pass / Fail — reason
Single state model:     Pass / Fail — reason
Low coupling:           Pass / Fail — reason
Abstraction:            Pass / Fail — reason
Command-query:          Pass / Fail / N/A — reason
Failure safety:         Pass / Fail — reason
Open/Closed:            Pass / Fail — reason
Core protection:        Pass / Fail / N/A — reason
KISS/YAGNI:             Pass / Fail — reason
Single source of truth: Pass / Fail — reason
```

## 11. Trade-Offs

- The plan favors feature-owned hooks over page-level API calls to preserve frontend/backend isolation and React Query invalidation ownership.
- The plan mirrors existing Razor route paths to avoid route churn and maintain user/bookmark continuity.
- The plan keeps contract-dependent gaps deferred instead of inventing frontend-only state models.
- The plan uses capability folders rather than generic shared folders to avoid cross-feature coupling.
- The plan keeps statistics behind a dedicated read model because UI-side aggregation would create an unreliable parallel source of truth.

## 12. Rejected Alternatives

| Alternative | Rejection reason |
| --- | --- |
| Put all Clinic pages and components in one flat `clinic` folder | Mixes unrelated capabilities and makes module ownership unclear. |
| Let pages import generated API hooks directly | Violates feature UI integration boundary and scatters error/invalidation behavior. |
| Share Appointments internals directly with Clinic patient panels | Couples sibling feature internals instead of using named feature adapters/contracts. |
| Implement missing patient status/archive/provider/room/procedure lifecycle in local React state | Creates parallel truth sources without backend command/query ownership. |
| Build a generic CRUD framework for all clinic entities | Adds speculative abstraction and violates YAGNI. |
| Compute statistics by calling multiple list endpoints from the dashboard | Creates inconsistent business metrics and duplicates read-model responsibility. |

## 13. Documentation Maintenance

When implementation begins, keep these sources synchronized:

- Update `docs/apps/clinic-owner/journeys/implementation-coverage-matrix.md` as journey status changes.
- Update `docs/apps/clinic-owner/modules/patient/journeys/implementation-matrix.md` for patient-specific objectives.
- Add ADRs only if introducing a new React host, new generated-client boundary, new route ownership convention, or a new cross-module data ownership rule.
- Keep this plan as a planning artifact; do not treat it as the source of truth for implemented status after code ships unless it is deliberately refreshed.
