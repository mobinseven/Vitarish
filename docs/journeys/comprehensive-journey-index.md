# Comprehensive User Journey Index

This document maps all user journey maps across the Vitarish platform, organized by domain, with cross-journey dependencies and persona coverage.

## Journey Map Inventory

### Platform Module — Owner & Tenant Management

| # | Journey | Type | Persona | Location |
|---|---------|------|---------|----------|
| 1 | Owner Onboarding | Current state | First-time owner | `docs/platform/modules/platform/journeys/owner-onboarding-journey.md` |
| 2 | Owner Dashboard | Current state | Owner | `docs/platform/modules/platform/journeys/owner-dashboard-journey.md` |
| 3 | Create Tenant | Current state | Owner | `docs/platform/modules/platform/journeys/owner-create-tenant-journey.md` |
| 4 | Tenant Overview | Current state | Owner | `docs/platform/modules/platform/journeys/owner-tenant-overview-journey.md` |
| 5 | Tenant Settings | Current state | Owner | `docs/platform/modules/platform/journeys/owner-tenant-settings-journey.md` |
| 6 | Tenant Switcher | Current state | Owner | `docs/platform/modules/platform/journeys/owner-tenant-switcher-journey.md` |
| 7 | Global Tenant List | Current state | Owner | `docs/platform/modules/platform/journeys/owner-global-tenant-list-journey.md` |
| 8 | Members & Roles | Current state | Owner | `docs/platform/modules/platform/journeys/owner-members-roles-journey.md` |
| 9 | Invite/Remove Users | Current state | Owner | `docs/platform/modules/platform/journeys/owner-invite-remove-users-journey.md` |
| 10 | Billing Page | Current state | Owner | `docs/platform/modules/platform/journeys/owner-billing-page-journey.md` |
| 11 | Action Inbox | Current state | Owner | `docs/platform/modules/platform/journeys/owner-action-inbox-journey.md` |
| 12 | Audit Log | Current state | Owner | `docs/platform/modules/platform/journeys/owner-audit-log-journey.md` |
| 13 | Danger Zone | Current state | Owner | `docs/platform/modules/platform/journeys/owner-danger-zone-journey.md` |
| 14 | Platform Admin — Users & Owners | Current state with gaps | Platform admin | `docs/platform/modules/platform/journeys/platform-admin-users-journey.md` |
| 15 | Tenant Indicator Management | Current state with gaps | Platform admin | `docs/platform/modules/platform/journeys/tenant-indicator-management-journey.md` |

### Base Module — Authentication

| # | Journey | Type | Persona | Location |
|---|---------|------|---------|----------|
| 16 | Authentication & Login | Current state with gaps | All users | `docs/platform/modules/base/journeys/authentication-login-journey.md` |

### Sea Module — Projects & Procedures

| # | Journey | Type | Persona | Location |
|---|---------|------|---------|----------|
| 17 | Sea Projects & Procedures | Current state with gaps | Owner + User | `docs/platform/modules/sea/journeys/sea-project-procedure-journey.md` |

### Medical Module — Prescriptions

| # | Journey | Type | Persona | Location |
|---|---------|------|---------|----------|
| 18 | Prescription Overview & History | Current state | Owner | `docs/platform/modules/medical/journeys/prescription/journeys/prescription-overview-history.md` |
| 19 | Eligible Patient Happy Path | Current state | Owner | `docs/platform/modules/medical/journeys/prescription/journeys/eligible-patient-happy-path.md` |
| 20 | Ineligible Patient Blocked | Current state | Owner | `docs/platform/modules/medical/journeys/prescription/journeys/ineligible-patient-blocked.md` |
| 21 | Paper Fallback Outage | Current state | Owner | `docs/platform/modules/medical/journeys/prescription/journeys/paper-fallback-outage.md` |
| 22 | Timeout/Duplicate Safe Recovery | Current state | Owner | `docs/platform/modules/medical/journeys/prescription/journeys/timeout-duplicate-safe-recovery.md` |
| 23 | Edit Submitted Prescription | Current state | Owner | `docs/platform/modules/medical/journeys/prescription/journeys/edit-submitted-prescription.md` |
| 24 | Cancel Prescription | Current state | Owner | `docs/platform/modules/medical/journeys/prescription/journeys/cancel-prescription.md` |
| 25 | Support Reconciliation | Current state | Support | `docs/platform/modules/medical/journeys/prescription/journeys/support-reconciliation.md` |
| 26 | Provider Credential Setup | Current state | Owner | `docs/platform/modules/medical/journeys/prescription/journeys/provider-credential-setup.md` |
| 27 | Multi-Coverage Payer Selection | Current state | Owner | `docs/platform/modules/medical/journeys/prescription/journeys/multi-coverage-payer-selection.md` |
| 28 | Prescription Template List | Current state | Owner | `docs/platform/modules/medical/journeys/prescription/journeys/prescription-template-list.md` |
| 29 | Medical Reference Query | Current state | Owner | `docs/platform/modules/medical/journeys/owner-medical-reference-query-journey.md` |
| 30 | Medical Reference Needs | Current state | Owner | `docs/platform/modules/medical/journeys/owner-medical-reference-needs.md` |
| 31 | Medical Code System Registry | Current state | Owner | `docs/platform/modules/medical/journeys/owner-medical-code-system-registry-journey.md` |
| 32 | Medical Import Operations | Current state | Owner | `docs/platform/modules/medical/journeys/owner-medical-import-operations-journey.md` |
| 33 | Medical Local Curation | Current state | Owner | `docs/platform/modules/medical/journeys/owner-medical-local-curation-journey.md` |
| 34 | Medical Service Valuation | Current state | Owner | `docs/platform/modules/medical/journeys/owner-medical-service-valuation-journey.md` |
| 35 | Medical Tenant Preferences | Current state | Owner | `docs/platform/modules/medical/journeys/owner-medical-tenant-preferences-journey.md` |

### Clinic App — Patient Care

| # | Journey | Type | Persona | Location |
|---|---------|------|---------|----------|
| 36 | Patient Overview | Future state | Clinician | `docs/apps/clinic-owner/modules/patient/journeys/patient-overview-journey.md` |
| 37 | Patient Profile CRUD | Current state | Clinician | `docs/apps/clinic-owner/modules/patient/journeys/patient-profile-crud-journey.md` |
| 38 | Patient Check-In | Current state | Clinician | `docs/apps/clinic-owner/modules/patient/journeys/patient-check-in-journey.md` |
| 39 | Patient Appointment | Current state | Clinician | `docs/apps/clinic-owner/modules/patient/journeys/patient-appointment-journey.md` |
| 40 | Patient Procedure | Current state with gaps | Clinician | `docs/apps/clinic-owner/modules/patient/journeys/patient-procedure-journey.md` |

### Clinic App — Appointments

| # | Journey | Type | Persona | Location |
|---|---------|------|---------|----------|
| 41 | Patient Check-In (Appointment) | Current state | Clinician | `docs/apps/clinic-owner/modules/appointment/journeys/patient-check-in-journey.md` |
| 42 | Patient Appointment (Appointment) | Current state | Clinician | `docs/apps/clinic-owner/modules/appointment/journeys/patient-appointment-journey.md` |
| 43 | Clinic Appointment Settings | Current state | Owner | `docs/apps/clinic-owner/modules/appointment/journeys/clinic-appointment-settings.md` |

### Clinic App — Prescriptions

| # | Journey | Type | Persona | Location |
|---|---------|------|---------|----------|
| 44 | Prescription Journey (Clinic) | Current state | Owner | `docs/apps/clinic-owner/modules/prescription/journeys/prescription-journey.md` |
| 45 | Prescription Ineligible | Current state | Owner | `docs/apps/clinic-owner/modules/prescription/journeys/prescription-ineligible-journey.md` |
| 46 | Prescription Fallback | Current state | Owner | `docs/apps/clinic-owner/modules/prescription/journeys/prescription-fallback-journey.md` |
| 47 | Prescription Timeout Recovery | Current state | Owner | `docs/apps/clinic-owner/modules/prescription/journeys/prescription-timeout-recovery-journey.md` |
| 48 | Prescription Overview | Current state | Owner | `docs/apps/clinic-owner/modules/prescription/journeys/prescription-overview-journey.md` |
| 49 | Prescription Support Reconciliation | Current state | Support | `docs/apps/clinic-owner/modules/prescription/journeys/prescription-support-reconciliation-journey.md` |
| 50 | Prescription Submit | Current state | Owner | `docs/apps/clinic-owner/modules/prescription/journeys/prescription-submit-journey.md` |
| 51 | Prescription Reference Sync | Current state | Owner | `docs/apps/clinic-owner/modules/prescription/journeys/prescription-reference-sync-journey.md` |
| 52 | Prescription Edit | Current state | Owner | `docs/apps/clinic-owner/modules/prescription/journeys/prescription-edit-journey.md` |
| 53 | Prescription Cancel | Current state | Owner | `docs/apps/clinic-owner/modules/prescription/journeys/prescription-cancel-journey.md` |

### Knowledge Module

| # | Journey | Type | Persona | Location |
|---|---------|------|---------|----------|
| 54 | Knowledge Source Import | Current state | Owner | `docs/platform/modules/knowledge/journeys/knowledge-source-import-journey.md` |
| 55 | Knowledge Q&A Test | Current state | Owner | `docs/platform/modules/knowledge/journeys/knowledge-qa-test-journey.md` |
| 56 | Knowledge Diagnostics | Current state | Owner | `docs/platform/modules/knowledge/journeys/knowledge-diagnostics-journey.md` |
| 57 | Knowledge Corpus Management | Current state | Owner | `docs/platform/modules/knowledge/journeys/knowledge-corpus-management-journey.md` |
| 58 | Knowledge Indexing | Current state | Owner | `docs/platform/modules/knowledge/journeys/knowledge-indexing-journey.md` |
| 59 | Knowledge Evaluation | Current state | Owner | `docs/platform/modules/knowledge/journeys/knowledge-evaluation-journey.md` |

### Commerce Module

| # | Journey | Type | Persona | Location |
|---|---------|------|---------|----------|
| 60 | Commerce Storefront | Current state with gaps | Customer + Merchant + Admin | `docs/platform/modules/commerce/journeys/commerce-storefront-journey.md` |

### Communications Module

| # | Journey | Type | Persona | Location |
|---|---------|------|---------|----------|
| 61 | Reminder Management | Current state with gaps | Owner + User | `docs/platform/modules/communications/journeys/reminder-management-journey.md` |

## Cross-Journey Dependencies

```
Authentication (16)
  └── Onboarding (1)
        └── Tenant Management (3-7)
              ├── Platform Admin (14-15)
              ├── Sea Projects (17)
              ├── Patient Care (36-40)
              ├── Appointments (41-43)
              ├── Prescriptions (18-35, 44-53)
              ├── Knowledge (54-59)
              ├── Commerce (60)
              └── Communications (61)
```

## Persona Coverage Matrix

| Persona | Journeys |
|---------|----------|
| All users (unauthenticated) | Authentication (16) |
| First-time owner | Onboarding (1), Create Tenant (3) |
| Owner / Admin | Dashboard (2), Tenant Management (4-9), Members (8-9), Billing (10), Audit (12), Danger Zone (13), all Clinic/Prescription/Sea/Knowledge/Commerce owner journeys |
| Platform admin | Admin Users & Owners (14), Tenant Indicators (15), Commerce Provisioning (60), Knowledge Admin (54-59) |
| Clinician / Provider | Patient Care (36-40), Appointments (41-43) |
| Support staff | Prescription Reconciliation (25, 49) |
| Customer / End user | Commerce Storefront (60), Sea User views (17), Appointment User views (41-42) |
| Merchant / Store owner | Commerce Dashboard, Orders, Products (60) |

## Domains Without Journey Maps

The following domains have UI components but no dedicated journey maps:

| Domain | Components | Gap |
|--------|-----------|-----|
| Search | `SearchWidget`, `SearchIndexStatusPanel` | No search UX journey |
| Files | `FileImage`, `StackFileInput`, gallery transfer | No file management journey |
| Intelligence (AI) | `AiStreamWorkbench`, `AiResponderChat` | No AI interaction journey |
| Social | `SocialConnectionsPage` | No social connections journey |
| Links | `ShortLinkPage`, `QrLinkPage` | No link management journey |
| Feed | Feed components via Aggregator | No feed consumption journey |
| Financial | Interface project exists, no components | No financial journey |

## Statistics

| Metric | Count |
|--------|-------|
| Total journey maps | 61 |
| Domains with journeys | 10 (Platform, Base, Sea, Medical, Clinic/Patient, Clinic/Appointment, Clinic/Prescription, Knowledge, Commerce, Communications) |
| Domains without journeys | 7 (Search, Files, Intelligence, Social, Links, Feed, Financial) |
| Journey types: Current state | 48 |
| Journey types: Current state with gaps | 11 |
| Journey types: Future state | 2 |
| Unique personas | 7 (All users, First-time owner, Owner, Platform admin, Clinician, Support, Customer) |
