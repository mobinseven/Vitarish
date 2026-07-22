# Private Practice General App Documentation

Status: Active
Owner: Private Practice General app
Scope: Private Practice General app UX and app-specific module usage
Last reviewed: 2026-07-09

Former app identity: `clinic-owner`. The `clinic-owner` name is retained only as a temporary compatibility reference where explicitly needed.

App-level journeys are the canonical source of truth for Private Practice General UX. All collection folders are flat and owned by the app experience, not by backend modules.

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
- [0028 Patient Procedure Clinical Workspace Journey](journeys/0028-patient-procedure-workspace-journey.md)
- [0029 Procedure Template Management Journey](journeys/0029-procedure-template-management-journey.md)

## Flattened module usage journeys

Prescription and Sea clinical usage journeys migrated from legacy `modules/` nesting:

- [Prescription Journey](journeys/prescription-journey.md)
- [Prescription Submit Journey](journeys/prescription-submit-journey.md)
- [Prescription Edit Journey](journeys/prescription-edit-journey.md)
- [Prescription Cancel Journey](journeys/prescription-cancel-journey.md)
- [Prescription Overview Journey](journeys/prescription-overview-journey.md)
- [Prescription Ineligible Journey](journeys/prescription-ineligible-journey.md)
- [Prescription Fallback Journey](journeys/prescription-fallback-journey.md)
- [Prescription Timeout Recovery Journey](journeys/prescription-timeout-recovery-journey.md)
- [Prescription Support Reconciliation Journey](journeys/prescription-support-reconciliation-journey.md)
- [Prescription Reference Sync Journey](journeys/prescription-reference-sync-journey.md)
- [Clinical Sea Patient Project Journey](journeys/clinical-sea-patient-project-journey.md)
- [Clinical Sea Procedure Lifecycle Journey](journeys/clinical-sea-procedure-lifecycle-journey.md)
- [Clinical Sea Event Lifecycle Journey](journeys/clinical-sea-event-lifecycle-journey.md)
- [Clinical Sea Event Items Journey](journeys/clinical-sea-event-items-journey.md)
- [Clinical Sea Access and Roles Journey](journeys/clinical-sea-access-and-roles-journey.md)
- [Clinical Sea Template Reuse Journey](journeys/clinical-sea-template-reuse-journey.md)
- [Clinical Sea Observability and Triage Journey](journeys/clinical-sea-observability-and-triage-journey.md)
- [Clinical Sea Plugin Extension Journey](journeys/clinical-sea-plugin-extension-journey.md)

## Boundary

- App docs describe how Private Practice General composes and presents module behavior.
- Platform module docs describe app-agnostic module behavior and contracts.
