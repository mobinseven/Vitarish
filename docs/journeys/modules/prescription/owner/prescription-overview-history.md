# Journey: Prescription Overview and History

## 1. Context
- **Persona:** Doctor / Clinical Staff
- **Trigger:** Doctor needs to view, search, or filter past prescriptions for a patient or across the clinic.
- **Goal:** Quickly access prescription history with relevant lifecycle and tracking information.

## Current Implementation Alignment
- **Status:** `partial`.
- **Implemented now:** owner prescription list and detail routes load clinic/patient history, status badges, item counts, provider labels, detail items, status panel, basic filters, pagination, and a simple timeline; the operational dashboard has a routed owner admin page.
- **Current artifacts:** `OwnerPrescriptionsPage`, `OwnerPrescriptionDetailPage`, `OwnerPrescriptionOperationalDashboardPage`, `PrescriptionsList`, `PrescriptionDetail`, `PrescriptionStatusBadge`, `PrescriptionTimeline`, `PrescriptionOperationalDashboard`, `GetPrescriptionHistoryQuery`, `GetPrescriptionQuery`, observability queries.
- **Authoritative state:** prescription list/detail projections read from `PrescriptionRecord` and related items/submissions; operational counts read from query handlers without changing state.
- **Known gaps:** date-range filters are supported by the query shape but not surfaced in list UI, complete audit trail rendering is not present, `PrescriptionPrintView` exists but is not wired into the detail journey, and operational navigation/role discoverability needs confirmation.

## 2. Entry Conditions
1. User is authenticated with prescription view permission.
2. Patient context is available (for patient-scoped view) or clinic context (for clinic-wide view).

## 3. Journey Stages

### Stage 1: Patient-Scoped History
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Opens patient encounter/profile → prescription history tab | System loads prescriptions for this patient. |
| 1.2 | — | List shows: date, provider, type (drug/paraclinic/etc.), status badge, tracking code. Sorted by date descending. |
| 1.3 | Filters by: status, provider, date range, item type | List updates with matching records. |
| 1.4 | Clicks on specific prescription | Detail view opens with full item list, lifecycle timeline, tracking codes, audit trail summary. |

### Stage 2: Clinic-Wide Overview (Admin/Doctor)
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | Opens prescription overview dashboard | Shows: today's prescriptions, pending/failed counts, recent submissions. |
| 2.2 | Filters by: doctor, patient, provider, status, date range | Filtered results displayed. |
| 2.3 | — | Status distribution summary: accepted, rejected, pending, cancelled counts per provider. |

### Stage 3: Prescription Detail View
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | Views prescription detail | Full information: patient identity, doctor, clinic, provider, coverage used, items, status, versions. |
| 3.2 | Views lifecycle timeline | Chronological state transitions with timestamps and actors. |
| 3.3 | Views tracking information | Insurance tracking code, remote prescription ID, submission/acceptance timestamps. |
| 3.4 | (If editable/cancellable) Takes action | Links to edit or cancel journey if lifecycle and permissions allow. |
| 3.5 | Prints prescription | Print view with tracking code, items, and clinic branding. |

## 4. Exit Conditions
1. Doctor can find any past prescription for a patient quickly.
2. Status and tracking information are immediately visible without drill-down.
3. Action paths (edit/cancel/print) are accessible from history view.

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| No prescriptions found for patient | Empty state with clear message | No error; informational state |
| Search returns too many results | Pagination + filter suggestion | Standard paginated response |
| Prescription in error/pending state | Status badge clearly shows state | Links to relevant resolution journey |
| User lacks permission to view detail | "Access denied" or redacted view | Object-level auth enforced |

## 6. Acceptance Criteria
1. Patient prescription history is accessible from encounter/profile context.
2. Status badges provide at-a-glance lifecycle information.
3. Filtering by provider, status, date, and type is supported.
4. Detail view shows complete lifecycle timeline.
5. Actions (edit/cancel/print) are available based on current state and permissions.
6. Clinic-wide overview supports operational monitoring.
7. Sensitive data respects role-based visibility (staff may see less detail than doctor).
