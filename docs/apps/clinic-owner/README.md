# Clinic Owner App Documentation

Status: Active  
Owner: Clinic Owner app  
Scope: Clinic Owner app UX and app-specific module usage  
Last reviewed: 2026-07-08

Clinic Owner app-level journeys are the canonical source of truth for Clinic Owner UX. Module folders under `modules/<module>/` are supporting app-scoped usage notes only; they must not compete with these app-level journeys.

## Canonical app journeys

- [0001 Owner Dashboard Navigation Journey](journeys/0001-owner-dashboard-navigation-journey.md)
- [0002 Patient List Search And Selection Journey](journeys/0002-patient-list-search-and-selection-journey.md)
- [0003 Patient Identification Lookup Journey](journeys/0003-patient-identification-lookup-journey.md)
- [0004 Patient Advanced Search Journey](journeys/0004-patient-advanced-search-journey.md)
- [0005 Patient Create Journey](journeys/0005-patient-create-journey.md)
- [0006 Patient Edit Journey](journeys/0006-patient-edit-journey.md)
- [0007 Patient Detail Overview Journey](journeys/0007-patient-detail-overview-journey.md)
- [0008 Patient Delete Journey](journeys/0008-patient-delete-journey.md)
- [0009 Patient Find Or Create Journey](journeys/0009-patient-find-or-create-journey.md)
- [0010 Patient Check In Journey](journeys/0010-patient-check-in-journey.md)
- [0011 Patient Check In Sms Confirmation Journey](journeys/0011-patient-check-in-sms-confirmation-journey.md)
- [0012 Patient Appointment Panel Booking Journey](journeys/0012-patient-appointment-panel-booking-journey.md)
- [0013 Owner Reservation Management Journey](journeys/0013-owner-reservation-management-journey.md)
- [0014 Owner Shift Management Journey](journeys/0014-owner-shift-management-journey.md)
- [0015 Owner Shift Occurrence Journey](journeys/0015-owner-shift-occurrence-journey.md)
- [0016 User Shift Browsing And Booking Journey](journeys/0016-user-shift-browsing-and-booking-journey.md)
- [0017 User Reservation Management Journey](journeys/0017-user-reservation-management-journey.md)
- [0018 Service Type Management Journey](journeys/0018-service-type-management-journey.md)
- [0019 Service Management Journey](journeys/0019-service-management-journey.md)
- [0020 Service Provider Management Journey](journeys/0020-service-provider-management-journey.md)
- [0021 Appointment Settings Journey](journeys/0021-appointment-settings-journey.md)
- [0022 Patient Procedure Management Journey](journeys/0022-patient-procedure-management-journey.md)
- [0023 Patient Procedure Event Journey](journeys/0023-patient-procedure-event-journey.md)
- [0024 Patient Reminders Journey](journeys/0024-patient-reminders-journey.md)
- [0025 Owner Reminders Journey](journeys/0025-owner-reminders-journey.md)
- [0026 Clinic Settings Users Journey](journeys/0026-clinic-settings-users-journey.md)
- [0027 Clinic Statistics Journey](journeys/0027-clinic-statistics-journey.md)

## Supporting module usage docs

- [Appointment module usage](modules/appointment/journeys/README.md)
- [Patient module usage](modules/patient/journeys/README.md)
- [Prescription module usage](modules/prescription/journeys/README.md)
- [SEA module clinical usage](modules/sea/journeys/README.md)

## Boundary

- App docs describe how Clinic Owner composes and presents module behavior.
- Platform module docs describe app-agnostic module behavior and contracts.
