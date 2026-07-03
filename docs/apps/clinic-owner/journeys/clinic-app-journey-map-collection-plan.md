# Clinic Owner Journey Map Collection Generation Plan

## 1. Objective

Create a complete current-state journey-map documentation collection for the Clinic Owner app. Complete means every implemented user-visible Clinic feature discovered through static code inspection is represented in journey-map documentation, including routes, host pages, shared Clinic interface components, reused Appointments module components, Sea-backed patient procedure flows, Communications-backed reminders, and settings/statistics placeholders when present.

## 2. Scope Classification

- **App:** `clinic-owner`
- **Primary documentation home:** `docs/apps/clinic-owner/journeys/`
- **Capability:** app-wide journey documentation coverage for implemented Clinic features
- **Product / service:** Vitarish Clinic Owner app
- **Journey type for generated docs:** `Current state with gaps`
- **Evidence source:** static code review / implementation inspection
- **Confidence target:** `High` when route/page/component evidence is direct; `Medium` for inherited or plugin-composed behavior; `Low` for placeholder or indirect behavior
- **Related modules:** Clinic, Appointments, Sea, Communications, Medical/Prescription when linked from clinic-owner workflows

## 3. Source-of-Truth Decision

The requested source-of-truth collection should live at:

```txt
docs/apps/clinic-owner/journeys/
```

Existing module-level journey folders should be treated as evidence or related deep dives unless explicitly migrated:

```txt
docs/apps/clinic-owner/modules/patient/journeys/
docs/apps/clinic-owner/modules/appointment/journeys/
docs/apps/clinic-owner/modules/prescription/journeys/
```

Do not maintain duplicate authoritative journey files for the same workflow. If an existing module-level file is superseded by an app-level file, mark the old file as superseded or link from it to the app-level source of truth.

## 4. Required Collection Artifacts

Create these files under `docs/apps/clinic-owner/journeys/` and maintain the repository-wide React audit at `docs/journeys/react-interface-coverage-audit.md`:

```txt
README.md
0001-owner-dashboard-navigation-journey.md
0002-patient-list-search-and-selection-journey.md
0003-patient-identification-lookup-journey.md
0004-patient-advanced-search-journey.md
0005-patient-create-journey.md
0006-patient-edit-journey.md
0007-patient-detail-overview-journey.md
0008-patient-delete-journey.md
0009-patient-find-or-create-journey.md
0010-patient-check-in-journey.md
0011-patient-check-in-sms-confirmation-journey.md
0012-patient-appointment-panel-booking-journey.md
0013-owner-reservation-management-journey.md
0014-owner-shift-management-journey.md
0015-owner-shift-occurrence-journey.md
0016-user-shift-browsing-and-booking-journey.md
0017-user-reservation-management-journey.md
0018-service-type-management-journey.md
0019-service-management-journey.md
0020-service-provider-management-journey.md
0021-appointment-settings-journey.md
0022-patient-procedure-management-journey.md
0023-patient-procedure-event-journey.md
0024-patient-reminders-journey.md
0025-owner-reminders-journey.md
0026-clinic-settings-users-journey.md
0027-clinic-statistics-journey.md
```

Optional prescription journey files should only be added if the team decides prescriptions are part of this app-level Clinic collection rather than linked module-level journeys:

```txt
0028-prescription-submit-journey.md
0029-prescription-cancel-journey.md
0030-prescription-timeout-recovery-journey.md
0031-prescription-fallback-journey.md
0032-prescription-ineligible-patient-journey.md
0033-prescription-reference-sync-journey.md
0034-prescription-template-management-journey.md
0035-prescription-provider-credential-journey.md
0036-prescription-reconciliation-and-recovery-journey.md
```

## 5. Standard Journey Document Format

Every generated journey map should use this structure:

```md
# <Journey Name>

## 1. Context

- App:
- Module:
- Capability:
- Related apps:
- Related modules:
- Product / service:
- Journey type:
- Scenario:
- User goal:
- Business goal:
- Scope start:
- Scope end:
- Primary persona:
- Evidence sources:
- Confidence level:

## 2. Persona Snapshot

**Name / segment:**

**Situation:**

**Motivation:**

**Key needs:**

**Constraints:**

**Success means:**

## 3. Journey Summary

**Narrative:**

**Emotional arc:** Start: -> Middle: -> End:

**Biggest friction point:**

**Highest-value opportunity:**

## 4. Journey Stages

### Stage 1: <stage>

**User goal:**

**User actions:**

**Touchpoints / channels:**

**User thoughts / questions:**

**Emotions:** <label>, <1-5>, because <reason>.

**Pain points:**

**Needs:**

**Opportunities:**

**Success metrics:**

**Evidence:**

## 5. Current Implementation Evidence

- Routes:
- Pages/components:
- Application/API/contract evidence:
- State source of truth:
- Command/query split:
- Known validation/failure behavior:

## 6. Gaps and Risks

- Implemented:
- Partial:
- Missing:
- Risks:

## 7. Acceptance Criteria for Documentation Completeness

- All route entry points documented.
- All visible user actions documented.
- All user-visible success, empty, loading, and failure states documented where evident.
- All cross-module dependencies documented.
- All assumptions marked explicitly.

## 8. Related Journeys

-
```

## 6. README Requirements

`README.md` should contain:

1. Purpose of the clinic-owner journey collection.
2. Date and method of static assessment.
3. App-level scope statement.
4. Feature coverage summary.
5. Links to every journey file.
6. Links to module-level related journeys.
7. Evidence roots.
8. Known assumptions.
9. Maintenance rule that every Clinic route and user-visible feature must be mapped in the coverage matrix.

Recommended sections:

```md
# Clinic Owner Journey Map Collection

## Scope
## Static Evidence Sources
## Coverage Summary
## Journey Maps
## Cross-module Dependencies
## Existing Module-level Journey References
## Assumptions and Confidence
## Maintenance Rules
```

## 7. React Interface Coverage Audit Requirements

`docs/journeys/react-interface-coverage-audit.md` should map every implemented feature to one journey file.

Required columns:

```md
| Journey file | Owner | Expected user workflow | Current React route(s) | Current React evidence | Status | Confidence | Known gap |
```

Status values:

- `Implemented`: directly visible and usable in current code.
- `Partial`: implemented route/component exists, but journey has visible gaps or indirect/inherited behavior.
- `Placeholder`: route/page exists but no substantial visible workflow is evident from static inspection.
- `Related`: implemented outside `src/Hosts/Main/Pages/Clinic` but part of clinic-owner operational context.

## 8. Static Evidence Roots

Inspect and cite these code roots while generating final journeys:

```txt
src/Hosts/Main/Common/Routes/ClinicRoutes.cs
src/Hosts/Main/Pages/Clinic/
src/Apps/Clinic/App/
src/Apps/Clinic/Api/
src/Apps/Clinic/Application/
src/Apps/Clinic/Contracts/
src/Apps/Clinic/Domain/
src/Apps/Clinic/Infrastructure/
src/Apps/Clinic/Interface/
src/Apps/Clinic/Client/
src/Modules/Appointments/Interface/Components/
src/Modules/Appointments/Application/Commands/
src/Modules/Appointments/Contracts/
src/Modules/Sea/
src/Modules/Communications/
src/Modules/Medical/
docs/apps/clinic-owner/modules/
docs/platform/modules/appointments/
docs/platform/modules/medical/
```

## 9. Route Coverage Checklist

Every route below must appear in `docs/journeys/react-interface-coverage-audit.md` and be traceable to at least one journey file:

- [ ] `/owner/clinic`
- [ ] `/owner/clinic/check-in`
- [ ] `/owner/clinic/patients`
- [ ] `/owner/clinic/patients/new`
- [ ] `/owner/clinic/patients/{patientId}`
- [ ] `/owner/clinic/patients/{patientId}/edit`
- [ ] `/owner/clinic/patients/{patientId}/procedures/{procedureId}`
- [ ] `/owner/clinic/patients/{patientId}/procedures/{procedureId}/events/{eventId}`
- [ ] `/owner/clinic/settings/users`
- [ ] `/owner/clinic/settings/statistics`
- [ ] `/owner/clinic/reminders`
- [ ] `/owner/clinic/appointment-settings`
- [ ] `/owner/clinic/reservations`
- [ ] `/owner/clinic/reservations/{reservationId}`
- [ ] `/owner/clinic/shifts`
- [ ] `/owner/clinic/shifts/new`
- [ ] `/owner/clinic/shifts/new/{dayOfWeek}`
- [ ] `/owner/clinic/shifts/{shiftId}`
- [ ] `/owner/clinic/shifts/{shiftId}/edit`
- [ ] `/owner/clinic/shifts/{shiftId}/occurrence/{occurrenceDate}`
- [ ] `/user/clinic/reservations`
- [ ] `/user/clinic/reservations/{reservationId}`
- [ ] `/user/clinic/shifts`
- [ ] `/user/clinic/shifts/{shiftId}`
- [ ] `/owner/clinic/service-types`
- [ ] `/owner/clinic/service-types/{serviceTypeId}`
- [ ] `/owner/clinic/services`
- [ ] `/owner/clinic/services/{serviceId}`
- [ ] `/owner/clinic/service-providers`
- [ ] `/owner/clinic/service-providers/{serviceProviderId}`

## 10. Component Coverage Checklist

Every component below must be represented in at least one journey or marked as related/supporting evidence:

- [ ] `ClinicOwnerDashboardPage`
- [ ] `ClinicOwnerPatientCheckInPage`
- [ ] `PatientFindOrCreate`
- [ ] `PatientIdentificationCodeLookup`
- [ ] `PatientIdentificationCodeInput`
- [ ] `PatientIdentificationCodeFound`
- [ ] `PatientIdentificationCodeNotFound`
- [ ] `PatientIdentificationCodeLookupFailure`
- [ ] `PatientAdvancedSearch`
- [ ] `PatientAdvancedSearchPanel`
- [ ] `PatientAdvancedSearchForm`
- [ ] `PatientAdvancedSearchResults`
- [ ] `PatientEditForm`
- [ ] `PatientsGrid`
- [ ] `PatientDetails`
- [ ] `PatientOverviewPanel`
- [ ] `PatientAppointmentsPanel`
- [ ] `PatientAppointmentBookingDialog`
- [ ] `PatientCheckInShiftStep`
- [ ] `PatientCheckInConfirmStep`
- [ ] `PatientRemindersPanel`
- [ ] `ReservationsList`
- [ ] `ReservationDetail`
- [ ] `ReservationDetailDialog`
- [ ] `ReservationFilters`
- [ ] `UserReservationDetail`
- [ ] `ShiftsList`
- [ ] `ShiftForm`
- [ ] `ShiftDetail`
- [ ] `UserShiftDetail`
- [ ] `BookableShiftsCalendar`
- [ ] `ShiftReservationsCalendar`
- [ ] `ShiftReservationList`
- [ ] `ReservationForShiftDialog`
- [ ] `ShiftBookingPoliciesList`
- [ ] `ShiftBookingPolicyForm`
- [ ] `ShiftReminderPoliciesList`
- [ ] `ShiftReminderPolicyForm`
- [ ] `ServiceTypesList`
- [ ] `ServiceTypeDetail`
- [ ] `ServiceTypeForm`
- [ ] `ServicesList`
- [ ] `ServiceDetail`
- [ ] `ServiceForm`
- [ ] `ServiceProvidersList`
- [ ] `ServiceProviderDetail`
- [ ] `ServiceProviderForm`

## 11. Journey-by-Journey Generation Plan

### 0001 Owner Dashboard Navigation Journey

- **Primary route:** `/owner/clinic`
- **Primary page:** `src/Hosts/Main/Pages/Clinic/Owners/ClinicOwnerDashboardPage.razor`
- **Persona:** clinic owner / clinic admin
- **Document:** dashboard entry, navigation to patients, check-in, shifts, reservations, reminders, users, appointment settings, and statistics.
- **State source of truth:** none; navigation hub only.
- **Command/query split:** N/A.
- **Gaps to check:** missing/disabled links, placeholder destinations.

### 0002 Patient List, Search, and Selection Journey

- **Primary route:** `/owner/clinic/patients`
- **Primary page:** `src/Hosts/Main/Pages/Clinic/Owners/Sea/ClinicOwnerPatientsPage.razor`
- **Primary component:** `src/Apps/Clinic/Interface/Components/Patients/PatientsGrid.razor`
- **Persona:** receptionist / clinic admin
- **Document:** patient list loading, text search, sort, pagination, selection, add patient, empty/error states.
- **State source of truth:** Clinic patient view over project-backed patient data.
- **Queries:** list patients, count patients.
- **Commands:** none in the list flow.

### 0003 Patient Identification Lookup Journey

- **Primary components:** `PatientIdentificationCodeLookup`, `PatientIdentificationCodeInput`, `PatientIdentificationCodeFound`, `PatientIdentificationCodeNotFound`, `PatientIdentificationCodeLookupFailure`
- **Persona:** receptionist / clinic admin
- **Document:** identification code input, debounce, exact lookup, found/not-found/failed/cancelled states, retry, select patient.
- **State source of truth:** patient search context during UI flow; Clinic patient query for persisted data.
- **Queries:** lookup patient by identification code.
- **Commands:** none.
- **Failure states:** lookup failure, cancellation by newer input.

### 0004 Patient Advanced Search Journey

- **Primary components:** `PatientAdvancedSearch`, `PatientAdvancedSearchPanel`, `PatientAdvancedSearchForm`, `PatientAdvancedSearchResults`
- **Persona:** receptionist / clinic admin
- **Document:** search by name, identification code, date of birth, phone, and email; result selection; no-result state; in-progress state; failure assumptions.
- **State source of truth:** patient advanced search context.
- **Queries:** patient search request through Clinic client.
- **Commands:** none.

### 0005 Patient Create Journey

- **Primary route:** `/owner/clinic/patients/new`
- **Primary page:** `ClinicOwnerPatientNewPage.razor`
- **Primary component:** `PatientEditForm`
- **Persona:** receptionist / clinic admin
- **Document:** opening create form, entering demographics/contact/identity fields, saving, navigating to created patient, cancelling.
- **State source of truth:** project-backed patient record adapted through Clinic patient mappings.
- **Commands:** create patient/project.
- **Queries:** optional supporting lookups from find-or-create flow.

### 0006 Patient Edit Journey

- **Primary route:** `/owner/clinic/patients/{patientId}/edit`
- **Primary page:** `ClinicOwnerPatientEditPage.razor`
- **Primary component:** `PatientEditForm`
- **Persona:** clinic admin / clinician
- **Document:** loading current patient, editing fields, saving, returning to detail, cancelling.
- **State source of truth:** project-backed patient record.
- **Queries:** load patient by ID.
- **Commands:** update patient/project.

### 0007 Patient Detail and Overview Journey

- **Primary route:** `/owner/clinic/patients/{patientId}`
- **Primary page:** `ClinicOwnerPatientPage.razor`
- **Primary components:** `PatientDetails`, `PatientOverviewPanel`, `PatientAppointmentsPanel`
- **Persona:** clinician / clinic admin
- **Document:** reviewing patient demographics, linked user, stats, procedures, appointments, quick actions, edit/delete actions.
- **State source of truth:** Clinic patient view plus Appointments reservations.
- **Queries:** patient by ID, appointments by patient/user party.
- **Commands:** none unless user triggers edit/delete/booking.

### 0008 Patient Delete Journey

- **Primary route:** `/owner/clinic/patients/{patientId}`
- **Primary component:** `PatientDetails`
- **Persona:** clinic admin
- **Document:** delete action, confirmation if present, successful return to patient list, inherited/plugin behavior, deletion uncertainty if indirect.
- **State source of truth:** project-backed patient record.
- **Commands:** delete patient/project.
- **Risk:** determine whether delete is soft delete, hard delete, or project archive.

### 0009 Patient Find-or-Create Journey

- **Primary component:** `PatientFindOrCreate`
- **Primary usage:** check-in flow
- **Persona:** receptionist
- **Document:** lookup first, advanced search fallback, duplicate candidates, create-new option, draft prefill from search criteria, selecting or creating patient.
- **State source of truth:** selected patient in check-in context; persisted patient in Clinic/Sea project model.
- **Queries:** lookup/search/get patient.
- **Commands:** create patient when needed.

### 0010 Patient Check-In Journey

- **Primary route:** `/owner/clinic/check-in`
- **Primary page:** `ClinicOwnerPatientCheckInPage.razor`
- **Primary components:** `PatientCheckInContext`, `PatientCheckInShiftStep`, `PatientCheckInConfirmStep`
- **Persona:** receptionist / front desk operator
- **Document:** identify patient, book slot, capacity override option, waitlist option if implemented, mark as arrived, confirm/present same-day reservation, operational warning, start another, go to patient.
- **State source of truth:** Appointments reservation plus temporary check-in context.
- **Queries:** bookable slots for active shifts.
- **Commands:** reserve, reserve with override, confirm reservation, present reservation.
- **Failures:** capacity/full shift, duplicate booking, confirm/present partial failure.

### 0011 Patient Check-In SMS Confirmation Journey

- **Primary component:** `PatientCheckInConfirmStep`
- **Persona:** receptionist
- **Document:** optional SMS confirmation after booking, recipient resolution through linked user phone, sent/failed states, missing recipient failure, exception failure.
- **State source of truth:** Communications message outcome plus transient check-in notification status.
- **Commands:** send SMS.
- **Queries:** linked user/phone lookup from tenant users.

### 0012 Patient Appointment Panel Booking Journey

- **Primary components:** `PatientAppointmentsPanel`, `PatientAppointmentBookingDialog`
- **Persona:** scheduler / clinic admin
- **Document:** load patient appointments, open booking dialog, load shifts, select active shift, reserve first bookable occurrence, optional reminder after booking, refresh panel.
- **State source of truth:** Appointments reservation.
- **Queries:** reservations, active shifts, bookable dates.
- **Commands:** reserve appointment, optional reminder creation.

### 0013 Owner Reservation Management Journey

- **Primary routes:** `/owner/clinic/reservations`, `/owner/clinic/reservations/{reservationId}`
- **Primary pages:** `ClinicOwnerReservationsPage`, `ClinicOwnerReservationDetailPage`
- **Persona:** scheduler / clinic owner
- **Document:** reservation list, filters if present, opening detail, status changes, delete/cancel if available.
- **State source of truth:** Appointments reservation.
- **Queries:** reservation list/detail.
- **Commands:** update status, delete/cancel/move if visible.

### 0014 Owner Shift Management Journey

- **Primary routes:** `/owner/clinic/shifts`, `/owner/clinic/shifts/new`, `/owner/clinic/shifts/new/{dayOfWeek}`, `/owner/clinic/shifts/{shiftId}`, `/owner/clinic/shifts/{shiftId}/edit`
- **Primary pages:** `ClinicOwnerShiftsPage`, `ClinicOwnerShiftCreatePage`, `ClinicOwnerShiftDetailPage`, `ClinicOwnerShiftEditPage`
- **Persona:** clinic owner / scheduler
- **Document:** list shifts, create, prefill day, view detail, edit, delete, update status, booking/reminder policies if visible.
- **State source of truth:** Appointments shift.
- **Queries:** shifts and shift detail.
- **Commands:** create/update/delete shift, update shift status.

### 0015 Owner Shift Occurrence Journey

- **Primary route:** `/owner/clinic/shifts/{shiftId}/occurrence/{occurrenceDate}`
- **Primary page:** `ClinicOwnerShiftOccurrencePage.razor`
- **Persona:** scheduler / clinic owner
- **Document:** date-specific occurrence view, occurrence reservations/capacity, reservation inspection, same-day operations if present.
- **State source of truth:** Appointments shift occurrence/reservation projection.
- **Queries:** occurrence reservations.
- **Commands:** create/manage reservation only if exposed by component.

### 0016 User Shift Browsing and Booking Journey

- **Primary routes:** `/user/clinic/shifts`, `/user/clinic/shifts/{shiftId}`
- **Primary pages:** `ClinicUserShiftsPage`, `ClinicUserShiftDetailPage`
- **Persona:** patient / clinic user
- **Document:** browse shifts, open shift, reserve occurrence, cancel if available, refresh after reservation changes.
- **State source of truth:** Appointments shift and reservation.
- **Queries:** user-visible shifts/bookable dates.
- **Commands:** reserve/cancel reservation.

### 0017 User Reservation Management Journey

- **Primary routes:** `/user/clinic/reservations`, `/user/clinic/reservations/{reservationId}`
- **Primary pages:** `ClinicUserReservationsPage`, `ClinicUserReservationDetailPage`
- **Persona:** patient / clinic user
- **Document:** view own reservations, navigate to shifts, open reservation detail, cancel reservation if allowed.
- **State source of truth:** Appointments reservation.
- **Queries:** user reservations and detail.
- **Commands:** cancel reservation.

### 0018 Service Type Management Journey

- **Primary routes:** `/owner/clinic/service-types`, `/owner/clinic/service-types/{serviceTypeId}`
- **Primary pages:** `ClinicOwnerServiceTypesPage`, `ClinicOwnerServiceTypeDetailPage`
- **Persona:** clinic owner / scheduler admin
- **Document:** list, open detail, create, edit, delete service types.
- **State source of truth:** Appointments service type.
- **Queries:** service types.
- **Commands:** create/update/delete service type.

### 0019 Service Management Journey

- **Primary routes:** `/owner/clinic/services`, `/owner/clinic/services/{serviceId}`
- **Primary pages:** `ClinicOwnerServicesPage`, `ClinicOwnerServiceDetailPage`
- **Persona:** clinic owner / scheduler admin
- **Document:** list, open detail, create, edit, delete services.
- **State source of truth:** Appointments service.
- **Queries:** services.
- **Commands:** create/update/delete service.

### 0020 Service Provider Management Journey

- **Primary routes:** `/owner/clinic/service-providers`, `/owner/clinic/service-providers/{serviceProviderId}`
- **Primary pages:** `ClinicOwnerServiceProvidersPage`, `ClinicOwnerServiceProviderDetailPage`
- **Persona:** clinic owner / scheduler admin
- **Document:** list, open detail, create, edit, delete providers, link/unlink users if exposed.
- **State source of truth:** Appointments service provider.
- **Queries:** service providers.
- **Commands:** create/update/delete provider, link/unlink provider user if visible.

### 0021 Appointment Settings Journey

- **Primary route:** `/owner/clinic/appointment-settings`
- **Primary page:** `ClinicOwnerAppointmentsSettings.razor`
- **Persona:** clinic owner / admin
- **Document:** all visible appointment settings panels; if placeholder, document current route-only placeholder and gaps.
- **State source of truth:** Appointments configuration artifacts exposed by page.
- **Queries/commands:** derive from visible settings components.

### 0022 Patient Procedure Management Journey

- **Primary routes:** `/owner/clinic/patients/{patientId}`, `/owner/clinic/patients/{patientId}/procedures/{procedureId}`
- **Primary pages:** `ClinicOwnerPatientPage`, `ClinicOwnerPatientProcedurePage`
- **Persona:** clinician
- **Document:** view patient procedures, create procedure, open procedure, edit/delete where exposed, plugin sections.
- **State source of truth:** Sea project/procedure data scoped to patient.
- **Queries:** procedure list/detail.
- **Commands:** create/update/delete procedure where exposed.

### 0023 Patient Procedure Event Journey

- **Primary route:** `/owner/clinic/patients/{patientId}/procedures/{procedureId}/events/{eventId}`
- **Primary page:** `ClinicOwnerPatientEventPage`
- **Persona:** clinician
- **Document:** open event, view/edit/delete event if exposed, return to procedure after delete, page title/event load.
- **State source of truth:** Sea event data.
- **Queries:** get event.
- **Commands:** edit/delete event if visible.

### 0024 Patient Reminders Journey

- **Primary component:** `PatientRemindersPanel`
- **Persona:** clinician / clinic admin
- **Document:** load reminders by patient source ID, create reminder, open reminder detail dialog, empty/failure assumptions.
- **State source of truth:** Communications reminder.
- **Queries:** reminders by source.
- **Commands:** create/update reminder via Communications UI components.

### 0025 Owner Reminders Journey

- **Primary route:** `/owner/clinic/reminders`
- **Primary page:** `ClinicOwnerRemindersPage.razor`
- **Persona:** clinic owner / admin
- **Document:** visible owner-level reminders workflow; if placeholder, document as implemented route with current-state gap.
- **State source of truth:** Communications reminders if present.
- **Queries/commands:** derive from page content.

### 0026 Clinic Settings Users Journey

- **Primary route:** `/owner/clinic/settings/users`
- **Primary page:** `ClinicOwnerSettingsUsersPage.razor`
- **Persona:** clinic owner / admin
- **Document:** clinic users settings, tenant members/users UI if reused from Sea, patient linked-user relationship where relevant.
- **State source of truth:** tenant users/members plus patient linked user IDs.
- **Queries/commands:** derive from visible user settings components.

### 0027 Clinic Statistics Journey

- **Primary route:** `/owner/clinic/settings/statistics`
- **Primary page:** `ClinicOwnerSettingsStatisticsPage.razor`
- **Persona:** clinic owner / admin
- **Document:** visible statistics, patient/procedure counts if exposed, placeholder state if no substantial content exists.
- **State source of truth:** Clinic patient view stats and any page-specific analytics source.
- **Queries:** statistics/read models only.
- **Commands:** none.

## 12. Cross-Module State Ownership

### Patient state

- **Owner:** Clinic app over Sea project-backed patient data.
- **Evidence:** Clinic patient DTOs/models, patient views, patient query handlers, patient form mappings.
- **Documentation rule:** journey docs must not imply Appointments or Communications owns patient identity.

### Appointment state

- **Owner:** Appointments module.
- **Evidence:** Appointments reservation, shift, service, service type, service provider contracts and components.
- **Documentation rule:** Clinic journeys consume Appointments capabilities; they do not own appointment lifecycle rules.

### Procedure/event state

- **Owner:** Sea module, hosted through Clinic patient pages.
- **Evidence:** Clinic `Owners/Sea` pages and procedure/event route wrappers.
- **Documentation rule:** document explicit Clinic route ownership and Sea data/workflow ownership.

### Reminder state

- **Owner:** Communications module.
- **Evidence:** `PatientRemindersPanel`, reminder actions/dialogs, Communications client calls.
- **Documentation rule:** patient reminders are source-linked by patient ID but persisted by Communications.

### Prescription state

- **Owner:** Medical/Prescription module.
- **Evidence:** existing prescription journey docs and Medical module command/query/provider artifacts.
- **Documentation rule:** include as related clinic-owner operational journey only if the app-level collection intentionally covers prescription workflows.

## 13. Generation Sequence

1. Create `docs/apps/clinic-owner/journeys/README.md` with scope, evidence roots, journey index, and source-of-truth rules.
2. Create `docs/journeys/react-interface-coverage-audit.md` and populate every Clinic route and implemented feature.
3. Generate journeys `0001` through `0027` using the standard format.
4. Link existing module-level patient, appointment, and prescription journey docs from README and relevant journey files.
5. Re-check route coverage against `src/Hosts/Web/src/app/routes.ts` and route files under `src/Hosts/Web/src/app/**`.
6. Re-check page coverage against React route pages under `src/Hosts/Web/src/app/owner/clinic/**` and `src/Hosts/Web/src/app/user/clinic/**`.
7. Re-check component coverage against React feature components under `src/Hosts/Web/src/features/**`.
8. Mark every assumption explicitly.
9. Ensure each journey has state source of truth and command/query split.
10. Run documentation lint/search checks listed below.

## 14. Documentation Completeness Acceptance Criteria

The work is complete when:

1. Every current React Clinic route appears in `docs/journeys/react-interface-coverage-audit.md` when it is journey evidence.
2. Every current React Clinic route page under `src/Hosts/Web/src/app/**` appears in `docs/journeys/react-interface-coverage-audit.md` when it is journey evidence.
3. Every major Clinic interface component is referenced by at least one journey.
4. Every Appointments component used by Clinic pages is referenced by at least one journey.
5. Every journey declares app, module/capability, persona, scope start/end, evidence sources, and confidence level.
6. Every journey has current-state stages and current implementation evidence.
7. Every journey states command/query boundaries where applicable.
8. Every journey states state ownership/source of truth.
9. Placeholder routes are documented as current-state placeholders instead of omitted.
10. Existing module-level journeys are linked, migrated, or explicitly marked historical/superseded.
11. No journey duplicates ownership of a state model already owned by another module.
12. Every documented gap is traceable to code evidence or marked as an assumption.

## 15. Required Checks After Generation

Run these after creating the collection:

```bash
git diff --check
rg -n "TO[D]O|TB[D]|FIX[M]E" docs/apps/clinic-owner/journeys
rg -n "AppRoutes\.Clinic|public const string" src/Hosts/Main/Common/Routes/ClinicRoutes.cs
find docs/apps/clinic-owner/journeys -maxdepth 1 -type f | sort
```

Optional deeper checks:

```bash
rg -n "src/Hosts/Main/Pages/Clinic|src/Apps/Clinic|src/Modules/Appointments" docs/apps/clinic-owner/journeys
rg -n "State source of truth|Command/query" docs/apps/clinic-owner/journeys
```

## 16. Per-Artifact Rule Audit for This Plan

Artifact: `docs/apps/clinic-owner/journeys/clinic-app-journey-map-collection-plan.md`
Type: documentation plan
Responsibility: Define the exhaustive, static-inspection-driven plan for generating Clinic Owner journey-map documentation.

- **SRP:** Pass — the file only plans the journey-map documentation collection.
- **Single state model:** Pass — it names state owners rather than creating new state models.
- **Low coupling:** Pass — it references evidence roots and ownership boundaries without coupling implementation modules.
- **Abstraction:** Pass — it documents app/module boundaries rather than depending on implementation internals for behavior.
- **Command-query:** N/A — documentation artifact only; it requires future journeys to document command/query boundaries.
- **Failure safety:** Pass — it requires placeholder/uncertain behavior to be marked explicitly.
- **Open/Closed:** Pass — additional journeys can be added by creating new files without changing existing generated journey documents unnecessarily.
- **Core protection:** N/A — documentation only; no core logic touched.
- **KISS/YAGNI:** Pass — the plan contains only artifacts required to document current implemented Clinic features.
- **Single source of truth:** Pass — it declares `docs/apps/clinic-owner/journeys/` as the app-level journey collection and warns against duplicate authoritative sources.
