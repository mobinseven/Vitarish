# Clinic Owner Journey Map Collection

## Scope

This collection is the app-level source of truth for current-state journey maps covering implemented user-visible Clinic Owner features. It covers Clinic routes, host pages, Clinic interface components, Appointments components hosted under Clinic, Sea-backed patient/procedure flows, Communications-backed reminders, and current settings/statistics placeholders.

## Static Evidence Sources

Assessment date: 2026-06-30.

Assessment method: static code inspection of Clinic Owner routes, pages, Clinic interface components, and hosted Appointments/Sea/Communications components.

Evidence roots:

- `src/Hosts/Main/Common/Routes/ClinicRoutes.cs`
- `src/Hosts/Main/Pages/Clinic/`
- `src/Apps/Clinic/App/`
- `src/Apps/Clinic/Api/`
- `src/Apps/Clinic/Application/`
- `src/Apps/Clinic/Contracts/`
- `src/Apps/Clinic/Domain/`
- `src/Apps/Clinic/Infrastructure/`
- `src/Apps/Clinic/Interface/`
- `src/Apps/Clinic/Client/`
- `src/Modules/Appointments/Interface/Components/`
- `src/Modules/Appointments/Application/Commands/`
- `src/Modules/Appointments/Contracts/`
- `src/Modules/Sea/`
- `src/Modules/Communications/`
- `src/Modules/Medical/`

## Coverage Summary

- Route coverage: every route constant in `AppRoutes.Clinic` is mapped in `implementation-coverage-matrix.md`.
- Page coverage: every Clinic page under `src/Hosts/Main/Pages/Clinic` is mapped as a journey entry, supporting page base, or supporting evidence.
- Component coverage: every required Clinic and Appointments component from the collection plan is represented in one or more journey files or the coverage matrix.
- Placeholder coverage: appointment settings and statistics are documented as current-state low-confidence/placeholder journeys where static evidence shows limited workflow.
- State ownership: patient/procedure state stays with Clinic-over-Sea/Sea, appointment state stays with Appointments, reminders stay with Communications, and prescription journeys remain related module-level references unless intentionally promoted later.

## Journey Maps

- [Owner Dashboard Navigation](0001-owner-dashboard-navigation-journey.md)
- [Patient List, Search, and Selection](0002-patient-list-search-and-selection-journey.md)
- [Patient Identification Lookup](0003-patient-identification-lookup-journey.md)
- [Patient Advanced Search](0004-patient-advanced-search-journey.md)
- [Patient Create](0005-patient-create-journey.md)
- [Patient Edit](0006-patient-edit-journey.md)
- [Patient Detail and Overview](0007-patient-detail-overview-journey.md)
- [Patient Delete](0008-patient-delete-journey.md)
- [Patient Find or Create](0009-patient-find-or-create-journey.md)
- [Patient Check-In](0010-patient-check-in-journey.md)
- [Patient Check-In SMS Confirmation](0011-patient-check-in-sms-confirmation-journey.md)
- [Patient Appointment Panel Booking](0012-patient-appointment-panel-booking-journey.md)
- [Owner Reservation Management](0013-owner-reservation-management-journey.md)
- [Owner Shift Management](0014-owner-shift-management-journey.md)
- [Owner Shift Occurrence](0015-owner-shift-occurrence-journey.md)
- [User Shift Browsing and Booking](0016-user-shift-browsing-and-booking-journey.md)
- [User Reservation Management](0017-user-reservation-management-journey.md)
- [Service Type Management](0018-service-type-management-journey.md)
- [Service Management](0019-service-management-journey.md)
- [Service Provider Management](0020-service-provider-management-journey.md)
- [Appointment Settings](0021-appointment-settings-journey.md)
- [Patient Procedure Management](0022-patient-procedure-management-journey.md)
- [Patient Procedure Event](0023-patient-procedure-event-journey.md)
- [Patient Reminders](0024-patient-reminders-journey.md)
- [Owner Reminders](0025-owner-reminders-journey.md)
- [Clinic Settings Users](0026-clinic-settings-users-journey.md)
- [Clinic Statistics](0027-clinic-statistics-journey.md)

## Cross-module Dependencies

- Clinic composes Sea-backed patient, procedure, and event workflows.
- Clinic composes Appointments-backed reservations, shifts, service types, services, providers, booking policies, and reminder policies.
- Clinic composes Communications-backed reminder grids and patient reminder panels.
- Medical/Prescription remains a related operational area through existing module-level journeys; this collection does not duplicate prescription ownership.

## Existing Module-level Journey References

- `docs/apps/clinic-owner/modules/patient/journeys/`
- `docs/apps/clinic-owner/modules/appointment/journeys/`
- `docs/apps/clinic-owner/modules/prescription/journeys/`
- `docs/platform/modules/appointments/`
- `docs/platform/modules/medical/`

Existing module-level journeys are related deep dives unless a future change explicitly marks them superseded by an app-level journey.

## Assumptions and Confidence

- High confidence means direct route/page/component evidence was found.
- Medium confidence means the route is direct but important behavior is inherited from shared module components or plugin-composed behavior.
- Low confidence means the route exists but static inspection shows placeholder or route-only behavior.
- Runtime validation, permissions beyond route/page attributes, API error copy, and production usage were not verified.

## Maintenance Rules

- Every Clinic route and user-visible feature must be represented in `implementation-coverage-matrix.md`.
- New Clinic Owner routes must link to exactly one app-level journey or be marked as supporting evidence for an existing journey.
- Do not duplicate state ownership in journey language: Appointments owns appointment state, Sea owns procedure/event state, Communications owns reminder state, and Clinic composes these workflows.
- Placeholder routes must remain documented as current state until they are implemented.
