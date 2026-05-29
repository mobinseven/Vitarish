# Prescription Module Journey Implementation Matrix

This matrix is the source of truth for Prescription Module journey implementation status.
Status enum values: `implemented`, `partial`, `missing`.

## E-Prescription Submission

| Objective | Canonical journey file path | Implementation status | Required route(s) | Required page/component identifiers | Required backend/query/command identifiers | Known gaps |
| --- | --- | --- | --- | --- | --- | --- |
| Submit prescription for eligible patient (happy path) | `docs/journeys/modules/prescription/owner/eligible-patient-happy-path.md` | `missing` | `AppRoutes.Prescription.New(encounterId)` | `PrescriptionPage.razor`<br/>`EligibilityPanel`<br/>`PrescriptionItemGrid`<br/>`SubmissionResultPanel` | `CreateDraftCommand`<br/>`AddItemCommand`<br/>`ValidateDraftCommand`<br/>`SubmitPrescriptionCommand`<br/>`IInsuranceProviderAdapter.CheckEntitlement`<br/>`IInsuranceProviderAdapter.SubmitPrescription` | No prescription module exists yet. |
| Select payer when patient has multiple coverages | `docs/journeys/modules/prescription/owner/multi-coverage-payer-selection.md` | `missing` | `AppRoutes.Prescription.New(encounterId)` | `PayerSelectorPanel`<br/>`CoverageStatusBadge` | `GetPatientCoveragesQuery`<br/>`SelectPayerCommand` | No prescription module exists yet. |
| Block submission for ineligible patient | `docs/journeys/modules/prescription/owner/ineligible-patient-blocked.md` | `missing` | `AppRoutes.Prescription.New(encounterId)` | `EligibilityPanel`<br/>`IneligibleGuidancePanel` | `IInsuranceProviderAdapter.CheckEntitlement`<br/>`EligibilityInquiry` entity | No prescription module exists yet. |

## Prescription Lifecycle Management

| Objective | Canonical journey file path | Implementation status | Required route(s) | Required page/component identifiers | Required backend/query/command identifiers | Known gaps |
| --- | --- | --- | --- | --- | --- | --- |
| Edit submitted prescription | `docs/journeys/modules/prescription/owner/edit-submitted-prescription.md` | `missing` | `AppRoutes.Prescription.Edit(prescriptionId)` | `PrescriptionPage.razor` (edit mode)<br/>`EditEligibilityGuard` | `EditPrescriptionCommand`<br/>`IInsuranceProviderAdapter.EditPrescription` | No prescription module exists yet. |
| Cancel prescription | `docs/journeys/modules/prescription/owner/cancel-prescription.md` | `missing` | `AppRoutes.Prescription.Detail(prescriptionId)` | `CancelConfirmDialog`<br/>`PrescriptionTimeline` | `CancelPrescriptionCommand`<br/>`IInsuranceProviderAdapter.CancelPrescription` | No prescription module exists yet. |
| View prescription history and overview | `docs/journeys/modules/prescription/owner/prescription-overview-history.md` | `missing` | `AppRoutes.Prescription.PatientHistory(patientId)`<br/>`AppRoutes.Prescription.Dashboard` | `PrescriptionHistoryList`<br/>`PrescriptionDetailView`<br/>`PrescriptionDashboard` | `GetPrescriptionHistoryQuery`<br/>`GetPrescriptionDetailQuery` | No prescription module exists yet. |

## Recovery and Resilience

| Objective | Canonical journey file path | Implementation status | Required route(s) | Required page/component identifiers | Required backend/query/command identifiers | Known gaps |
| --- | --- | --- | --- | --- | --- | --- |
| Duplicate-safe timeout recovery | `docs/journeys/modules/prescription/owner/timeout-duplicate-safe-recovery.md` | `missing` | N/A (background worker + support queue) | `SyncPendingStatusPanel`<br/>`ReconciliationQueue` | `TimeoutRecoveryWorker`<br/>`FetchRemoteStatusCommand`<br/>`SafeRetryCommand` | No prescription module exists yet. |
| Paper fallback during outage | `docs/journeys/modules/prescription/owner/paper-fallback-outage.md` | `missing` | `AppRoutes.Prescription.New(encounterId)` | `PaperFallbackDialog`<br/>`FallbackReasonForm` | `InitiatePaperFallbackCommand`<br/>`ReconcileFallbackCommand` | No prescription module exists yet. |
| Support reconciliation desk | `docs/journeys/modules/prescription/owner/support-reconciliation.md` | `missing` | `AppRoutes.Prescription.SupportQueue` | `ReconciliationDashboard`<br/>`TransactionDetailPanel`<br/>`SafeRetryAction`<br/>`ManualReconcileAction` | `GetFailedTransactionsQuery`<br/>`SafeRetryCommand`<br/>`ManualReconcileCommand`<br/>`EscalateCommand` | No prescription module exists yet. |

## Administration and Configuration

| Objective | Canonical journey file path | Implementation status | Required route(s) | Required page/component identifiers | Required backend/query/command identifiers | Known gaps |
| --- | --- | --- | --- | --- | --- | --- |
| Reference data synchronization (admin) | `docs/journeys/modules/prescription/owner/reference-data-admin.md` | `missing` | `AppRoutes.Prescription.AdminReferenceData` | `ReferenceFreshnessDashboard`<br/>`SyncHistoryPanel`<br/>`ManualSyncAction` | `TriggerReferenceSyncCommand`<br/>`GetReferenceFreshnessQuery`<br/>`ReferenceSyncWorker` | No prescription module exists yet. |
| Provider credential setup | `docs/journeys/modules/prescription/owner/provider-credential-setup.md` | `missing` | `AppRoutes.Prescription.AdminCredentials` | `CredentialMatrixDashboard`<br/>`CredentialConfigForm`<br/>`CredentialTestAction` | `ConfigureCredentialCommand`<br/>`TestCredentialCommand`<br/>`GetCredentialStatusQuery` | No prescription module exists yet. |
| Provider onboarding | `docs/journeys/modules/prescription/owner/provider-onboarding.md` | `missing` | `AppRoutes.Prescription.AdminProviders` | `ProviderRegistrationForm`<br/>`CapabilityMatrixEditor`<br/>`EndpointConfigForm`<br/>`ConnectivityTestAction` | `RegisterProviderCommand`<br/>`UpdateCapabilityMatrixCommand`<br/>`ConfigureEndpointsCommand`<br/>`TestConnectivityCommand` | No prescription module exists yet. |
