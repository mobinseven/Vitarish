# Prescription Module — Implementation Plan

## Purpose

Deliver a production-ready multi-payer e-prescription capability as an integration layer inside existing HIS/EMR/clinic workflows.

The module must support multiple base insurance providers, including:
1. [Social Security Insurance / Tamin](../apis/EP-TAMIN-E-PRESCRIPTION-SERVICES-API.md)
2. Iran Health Insurance / Salamat / IHIO
3. [Armed Forces Medical Services Insurance](../apis/DITAS-E-PRESCRIPTION_SERVICES_API.md)
4. Future base or complementary insurance providers

The module must avoid duplicate data entry by keeping one clinical source of truth in existing patient, encounter, doctor, and prescription contexts, while isolating insurer-specific integration rules behind provider-specific adapters.

---

## 0) Design Alignment with Existing Vitarish Architecture

### Architecture placement

- Host Pages (`src/Hosts/Main/Pages`) own routable doctor-facing prescription screens.
- Module Interface (`src/Modules/Prescription/Interface`) owns reusable components:
  - eligibility panel
  - payer selector
  - coverage status panel
  - prescription status panel
  - provider warning panel
- Module Application owns:
  - use cases
  - commands and queries
  - validators
  - lifecycle coordination
  - provider routing
  - submission orchestration
- Module Domain owns:
  - entities
  - value objects
  - prescription lifecycle
  - payer-neutral prescription model
  - provider capability rules
- Module Infrastructure owns:
  - EF persistence
  - provider-specific API adapters
  - DITAS adapter, if required
  - outbox/retry workers
  - reference-data sync jobs
  - provider credential storage
- Module Hosting/Interface module class owns:
  - DI registration
  - endpoint initialization
  - provider adapter registration
- Modules Aggregator adds module registration.

### Design principles applied

- One clinical prescription model, many insurer integration mappings.
- One adapter boundary per insurance provider.
- One provider registry to manage capabilities and routing.
- Command-query separation:
  - commands mutate prescription state
  - queries read projections and remote status
- No external DTO leakage into domain/application.
- Provider-specific complexity must stay inside provider adapter and mapping layer.
- Start with minimum common operation set, then add provider-specific extensions through capability flags.

---

## 1) Scope Definition

### In scope

1. Doctor authentication mapping and role validation.
2. Doctor-to-insurer credential mapping per provider.
3. Patient identity capture and validation.
4. Multi-provider insurance entitlement check.
5. Patient coverage detection and payer selection.
6. Mobile capture and validation.
7. Prescription draft workspace.
8. Submission of visit, drug, paraclinic, medical, referral, and physiotherapy items where supported by the selected provider.
9. Fetch, edit, and cancel where officially allowed by each provider.
10. Provider-specific reference data synchronization and cache.
11. Local audit and API transaction logging.
12. Retry, idempotency, timeout recovery, and outage fallback per provider.
13. Certification/evaluation evidence preparation per provider.
14. Provider capability matrix for operation availability.

### Out of scope

1. Dispensing workflows.
2. Claims settlement accounting.
3. Full complementary insurance integration.
4. Patient mobile app.
5. Advanced clinical decision support beyond existing host capabilities.
6. Automatic claim reconciliation unless required for the provider pilot.

---

## 2) Key Architectural Correction

Replace the current single-insurer assumption:

`Insurance API Adapter`

with a multi-provider model:

`Insurance Provider Registry`
`Insurance Provider Capability Matrix`
`IInsuranceProviderAdapter`
`TaminInsuranceAdapter`
`SalamatInsuranceAdapter`
`ArmedForcesInsuranceAdapter`
`DitasGatewayAdapter`, if required
`ProviderReferenceDataSync`
`ProviderCredentialManager`
`ProviderErrorMapper`
`ProviderPrescriptionMapper`

This prevents the prescription module from becoming tightly coupled to Social Security/Tamin.

---

## 3) Proposed Module Decomposition

Create module namespace:

`Prescription`

Add the following submodules:

### 3.1. Insurance Provider Registry Submodule

Responsibilities:

1. Register supported insurance providers.
2. Store provider identity:
   - provider_key
   - provider_name
   - provider_type
   - integration_mode
   - active_flag
3. Expose provider capabilities.
4. Resolve available operations at runtime.
5. Support feature flags per provider and environment.

Example provider keys:

- `TAMIN`
- `SALAMAT`
- `ARMED_FORCES`
- `FUTURE_PROVIDER`

Example integration modes:

- `DirectApi`
- `PortalApi`
- `DitasGateway`
- `ManualFallback`
- `Disabled`

### 3.2. Provider Capability Matrix Submodule

Responsibilities:

Define which operations each provider supports.

Capabilities:

1. SupportsIdentityVerification
2. SupportsEntitlementCheck
3. SupportsVisitPrescription
4. SupportsDrugPrescription
5. SupportsParaclinicPrescription
6. SupportsMedicalServicePrescription
7. SupportsReferralPrescription
8. SupportsPhysiotherapyPrescription
9. SupportsDentalPrescription
10. SupportsFetchPrescription
11. SupportsEditPrescription
12. SupportsCancelPrescription
13. SupportsReferenceDrugSync
14. SupportsReferenceServiceSync
15. SupportsPriceInquiry
16. SupportsAllowedCountInquiry
17. SupportsWarningInquiry
18. SupportsProviderSession
19. SupportsOtpAuthentication
20. SupportsDigitalSignature
21. SupportsDitasSubmission
22. SupportsPaperFallbackRegistration

Rules:

1. UI actions must be enabled or disabled based on provider capability.
2. Submission orchestration must not call unsupported provider operations.
3. Certification scope must be generated per provider capability.
4. Provider capability must be configurable without code changes where possible.

### 3.3. Identity Access Submodule

Responsibilities:

1. Doctor-to-provider credential mapping.
2. Provider-specific token lifecycle.
3. Provider-specific session lifecycle.
4. OTP or two-step authentication flow where required.
5. Role-based submission authorization.
6. Credential isolation per provider, clinic, and doctor.

Data must support:

1. one doctor mapped to multiple insurance providers
2. one clinic mapped to multiple provider contracts
3. different credentials per branch or medical center
4. provider-specific credential expiration

### 3.4. Eligibility and Coverage Submodule

Responsibilities:

1. Patient national ID validation.
2. Patient identity verification.
3. Coverage discovery.
4. Provider-specific entitlement check.
5. Multi-coverage handling.
6. Entitlement cache with provider-specific expiration.
7. Primary payer selection.

The eligibility result must be provider-aware.

Example statuses:

- Eligible
- NotEligible
- Unknown
- ProviderUnavailable
- RequiresManualVerification
- RequiresPatientConfirmation
- RequiresFallback

Rules:

1. Normal electronic submission requires successful entitlement for the selected provider.
2. If the patient has multiple active coverages, the UI must let the doctor or operator choose the payer according to clinic policy.
3. Coverage result must be stored with provider_key.
4. Eligibility must not be globally cached across providers.
5. Each provider may have a different validity window for entitlement results.

### 3.5. Prescription Drafting Submodule

Responsibilities:

1. Maintain one payer-neutral prescription draft.
2. Manage item sections:
   - visit
   - drug
   - lab
   - imaging
   - paraclinic
   - medical service
   - physiotherapy
   - dental
   - referral
   - medical equipment
3. Validate clinical completeness.
4. Validate provider-specific submission readiness.
5. Store provider-specific mapping preview before submission.

Rules:

1. The draft is clinical and payer-neutral.
2. Provider-specific DTOs are generated only at submission time.
3. Provider-specific warnings are attached to the draft but not embedded in the clinical aggregate.
4. Item availability depends on selected provider capability and synchronized reference data.

### 3.6. Submission Orchestrator Submodule

Responsibilities:

1. Resolve selected provider.
2. Resolve patient coverage.
3. Resolve doctor-provider credentials.
4. Check provider capability.
5. Build provider-specific submission plan.
6. Generate idempotency key.
7. Submit through correct provider adapter.
8. Store remote prescription ID and tracking code.
9. Handle partial acceptance, rejection, warning, timeout, and duplicate risk.
10. Coordinate fetch/edit/cancel through selected provider adapter.

Submission routing inputs:

1. provider_key
2. patient_id
3. coverage_id
4. doctor_id
5. clinic_id
6. prescription_type
7. item_categories
8. operation_name
9. environment

### 3.7. Insurance Provider Adapter Layer

Define one internal interface:

`IInsuranceProviderAdapter`

Required methods:

1. GetProviderKey
2. GetCapabilities
3. Authenticate
4. RefreshToken
5. CreateProviderSession, if required
6. VerifyIdentity
7. CheckEntitlement
8. RegisterPrescription
9. RegisterVisitPrescription, if separated by provider
10. RegisterDrugPrescription, if separated by provider
11. RegisterParaclinicPrescription, if separated by provider
12. RegisterMedicalServicePrescription, if separated by provider
13. RegisterReferralPrescription, if supported
14. RegisterPhysiotherapyPrescription, if supported
15. FetchRegisteredPrescription
16. EditElectronicPrescription
17. CancelElectronicPrescription
18. FetchDrugReferenceData
19. FetchServiceReferenceData
20. FetchAllowedCount
21. FetchPrice
22. FetchWarnings
23. MapProviderError
24. NormalizeProviderStatus

Provider adapters:

1. `TaminInsuranceAdapter`
2. `SalamatInsuranceAdapter`
3. `ArmedForcesInsuranceAdapter`
4. `DitasGatewayAdapter`, if Armed Forces or national gateway integration requires it
5. `NullInsuranceAdapter` for disabled/manual providers

Rules:

1. No UI direct API calls.
2. No provider DTOs in domain.
3. No provider-specific logic in Host Pages.
4. Every provider adapter must have contract tests.
5. Every provider adapter must expose capability metadata.
6. Every provider adapter must map provider-specific errors into canonical errors.

### 3.8. Reference Data Submodule

Responsibilities:

1. Sync provider-specific drug lists.
2. Sync provider-specific service lists.
3. Sync national or gateway-level code systems where required.
4. Store versioned reference data.
5. Maintain crosswalk between local codes, national codes, and provider codes.
6. Keep active/inactive flags per provider.
7. Detect breaking reference-data changes.

Reference data must be namespaced by:

1. provider_key
2. code_system
3. code
4. version
5. effective_from
6. effective_to
7. record_origin

Example code systems:

- ProviderDrugCode
- ProviderServiceCode
- NationalDrugCode
- IRC
- ERX_IRC
- NationalServiceCode

Rules:

1. The same code value may mean different things for different providers.
2. Never store reference codes without provider_key and code_system.
3. Old prescriptions must remain readable after reference data changes.
4. UI selectors must filter codes by selected provider and supported item category.
5. Reference sync failure for one provider must not break other providers.

### 3.9. Lifecycle and Recovery Submodule

Responsibilities:

1. Maintain canonical prescription lifecycle.
2. Maintain provider-specific remote lifecycle.
3. Guard legal transitions.
4. Recover SyncPending and SyncFailed records.
5. Handle timeout without duplicate submission.
6. Handle provider-specific paper fallback.
7. Reconcile local and remote status.

Canonical lifecycle states:

- Draft
- ReadyForSubmission
- Submitting
- Submitted
- AcceptedByProvider
- RejectedByProvider
- NeedsCorrection
- Edited
- Cancelled
- PaperFallback
- SyncPending
- SyncFailed
- DuplicateRisk
- ProviderUnavailable
- ManuallyReconciled

Provider remote status must be stored separately.

Rules:

1. Canonical status is used by Vitarish.
2. Provider remote status is preserved exactly for audit/support.
3. State machine decisions use mapped canonical status.
4. Support users can view both canonical and provider-native status.
5. Retry is allowed only after duplicate-risk check.

### 3.10. Audit and Compliance Submodule

Responsibilities:

1. Append-only event log.
2. Provider-aware API transaction log.
3. Doctor action audit.
4. Admin action audit.
5. Credential change audit.
6. Provider configuration change audit.
7. Audit export per prescription, patient, doctor, provider, and date range.

Every audit record must include:

1. provider_key, if applicable
2. actor_id
3. actor_role
4. entity_type
5. entity_id
6. action
7. old_status
8. new_status
9. correlation_id
10. request_id
11. result
12. timestamp

---

## 4) Corrected Domain Model

### Core entities

- `Doctor`
- `Patient`
- `Clinic`
- `Encounter`
- `Prescription`
- `PrescriptionItem`
- `InsuranceProvider`
- `InsuranceProviderCapability`
- `ProviderContract`
- `DoctorProviderCredential`
- `PatientCoverage`
- `EligibilityInquiry`
- `PrescriptionProviderSubmission`
- `PrescriptionApiTransaction`
- `ReferenceCode`
- `ReferenceCodeCrosswalk`
- `ProviderWarning`
- `AuditLog`
- `OutageFallback`

### Important changes from the original model

The original model had:

- `insurance_mapping_id`
- `insurance_prescription_id`
- one generic `ReferenceCode`
- one generic `EligibilityInquiry`

Replace them with provider-aware models:

DoctorProviderCredential:
doctor_provider_credential_id
doctor_id
provider_key
clinic_id
provider_doctor_identifier
provider_username
credential_secret_ref
token_secret_ref
session_id
status
expires_at

PatientCoverage:
patient_coverage_id
patient_id
provider_key
coverage_identifier
insurance_booklet_identifier
coverage_type
eligible_flag
last_checked_at
expires_at
raw_status_code

EligibilityInquiry:
inquiry_id
provider_key
patient_id
doctor_id
clinic_id
request_time
result_status
tracking_code
raw_status_code
raw_status_message
expires_at

Prescription:
prescription_id
patient_id
doctor_id
clinic_id
encounter_id
selected_provider_key
selected_patient_coverage_id
status
created_at
submitted_at
version_number

PrescriptionProviderSubmission:
submission_id
prescription_id
provider_key
operation_name
remote_prescription_id
remote_tracking_code
remote_status_code
remote_status_text
submission_version
submitted_at
last_synced_at
canonical_status
raw_response_ref

ReferenceCode:
reference_code_id
provider_key
code_system
code
display_name
category
active_flag
version
effective_from
effective_to
record_origin
checksum

ReferenceCodeCrosswalk:
crosswalk_id
local_code
local_code_system
provider_key
provider_code
provider_code_system
mapping_confidence
active_flag
effective_from
effective_to

PrescriptionApiTransaction:
transaction_id
provider_key
prescription_id
submission_id
operation_name
request_hash
idempotency_key
request_id
correlation_id
response_code
response_message
duration_ms
started_at
finished_at
success_flag
error_category

---

## 5) Corrected State Machine

Canonical state transitions:

Draft
-> ReadyForSubmission
-> Submitting
-> Submitted
-> AcceptedByProvider

Draft
-> ReadyForSubmission
-> Submitting
-> RejectedByProvider
-> NeedsCorrection
-> Draft

AcceptedByProvider
-> EditRequested
-> Submitting
-> Edited
-> AcceptedByProvider

AcceptedByProvider
-> CancelRequested
-> Submitting
-> Cancelled

Submitting
-> SyncPending
-> ReconciliationInProgress
-> AcceptedByProvider | RejectedByProvider | DuplicateRisk | SyncFailed

Any allowed pre-submit state
-> PaperFallback

ProviderUnavailable
-> PaperFallback | RetryScheduled

Non-negotiable rules:

1. Illegal transitions are blocked in the domain layer.
2. Cancelled prescriptions cannot reuse the same local prescription_id for a new remote submission.
3. Retries are allowed only after duplicate-risk check.
4. Every transition emits an auditable domain event.
5. Canonical lifecycle must not depend on one provider’s status codes.
6. Provider-native statuses must be preserved for support and audit.

---

## 6) Corrected API Adapter Contract Strategy

Define one internal versioned gateway:

`IInsuranceProviderAdapter`

Generic operations:

- Authenticate
- RefreshToken
- CreateProviderSession
- VerifyIdentity
- DiscoverCoverage
- CheckEntitlement
- FetchWarnings
- SubmitPrescription
- FetchPrescription
- EditPrescription
- CancelPrescription
- FetchDrugList
- FetchServiceList
- FetchAllowedCount
- FetchPrice
- FetchProviderStatus
- NormalizeStatus
- MapError

Provider-specific operations may exist inside the concrete adapter, but must not leak into application services.

Rules:

1. No UI direct API calls.
2. No provider DTO leakage into domain/application.
3. Masked logging for patient-sensitive fields.
4. Record provider_key, operation name, response code, correlation ID, request hash, duration.
5. Every provider adapter must be independently testable.
6. Every provider adapter must support sandbox and production configuration.
7. Every adapter must declare unsupported operations explicitly.

---

## 7) Corrected Validation Model

### Payer-neutral local validation

- Required clinical fields.
- National ID format.
- Mobile number format.
- Doctor authorization.
- Clinic authorization.
- Duplicate item detection.
- Quantity and duration sanity checks.
- Prescription state-transition validation.

### Provider-specific local validation

- Selected provider is active.
- Doctor has valid provider credentials.
- Clinic has active provider contract.
- Patient has valid coverage for selected provider.
- Item category is supported by provider.
- Reference code exists for selected provider.
- Required provider-specific fields are present.
- Provider operation capability exists.

### Remote validation

- Provider entitlement response interpretation.
- Provider business-rule response mapping.
- Provider warning interpretation.
- Provider price/allowed-count interpretation, if supported.
- Provider-native rejection mapping.

Result:

No silent warning, rejection, unsupported-operation, or provider-mismatch path.

---

## 8) Corrected Data and Persistence Plan

### Storage boundaries

1. Canonical prescription and draft data in module DB context.
2. Provider submissions stored separately from prescription aggregate.
3. API transactions append-only for support and audit.
4. Reference code snapshots versioned by provider_key and code_system.
5. Provider credentials stored through secure secret references.
6. Outbox table supports provider-aware asynchronous retries.
7. Provider configuration stored in administrative tables.

### Idempotency approach

1. Deterministic request hash per:
   - provider_key
   - prescription_id
   - prescription_version
   - operation_name
   - item payload
2. Store outbound transaction before external call.
3. Use request_id and correlation_id for each provider call.
4. On timeout, fetch provider status before retry.
5. Duplicate risk routes to explicit support action.

---

## 9) Corrected Security and Compliance Controls

Security controls:

1. TLS enforced for all external and internal transport.
2. Secrets in protected store, never plaintext appsettings.
3. Provider credentials isolated per provider, clinic, and doctor.
4. Token encryption at rest.
5. Short-lived token/session handling.
6. Strict RBAC.
7. Object-level authorization for every command and query.
8. Provider-level authorization checks.
9. National ID, tokens, payloads, and medical data masked in logs.
10. Session timeout and internal API rate limiting.
11. Admin action monitoring.
12. Provider configuration audit.
13. Audit evidence export support.
14. Separate sandbox and production credentials for every provider.

Additional multi-payer rule:

A user authorized for one provider must not automatically gain access to another provider’s submission, credentials, or support operations.

---

## 10) Corrected Implementation Phases

### Phase 1 — Multi-Payer Discovery and Contract Baseline

Tasks:

1. Confirm official API access for each provider.
2. Confirm sandbox and production access per provider.
3. Confirm provider operation scope:
   - prescription writing
   - dispensing
   - paraclinic
   - physiotherapy
   - dental
   - referral
4. Confirm whether each provider is direct API, portal API, DITAS gateway, or manual fallback.
5. Build provider capability matrix.
6. Build endpoint and credential matrix.

Deliverables:

1. Provider access checklist.
2. Provider capability matrix.
3. Endpoint matrix.
4. Credential model.
5. Provider risk register.

### Phase 2 — Provider-Aware Domain and Schema Foundation

Tasks:

1. Implement provider registry entities.
2. Implement provider capability entities.
3. Implement patient coverage model.
4. Implement doctor-provider credential model.
5. Implement provider-aware prescription submission model.
6. Implement lifecycle state machine.
7. Implement audit and transaction schema.

Deliverables:

1. ERD.
2. State diagram.
3. Provider model documentation.
4. Migration scripts.

### Phase 3 — Adapter Framework and First Provider

Tasks:

1. Implement `IInsuranceProviderAdapter`.
2. Implement provider adapter factory.
3. Implement provider error mapper.
4. Implement provider status normalizer.
5. Implement first concrete adapter.
6. Add timeout, retry, and circuit breaker.
7. Add mock server tests.

Deliverables:

1. Adapter framework.
2. First working provider integration.
3. Contract tests.
4. Integration tests.

### Phase 4 — Additional Provider Adapters

Tasks:

1. Implement Tamin adapter.
2. Implement Salamat adapter.
3. Implement Armed Forces adapter or DITAS-backed adapter.
4. Implement unsupported-operation behavior.
5. Implement provider-specific authentication/session flows.
6. Implement provider-specific entitlement and submission mapping.

Deliverables:

1. Provider adapters.
2. Adapter capability declarations.
3. Provider-specific contract tests.
4. Provider-specific error catalog.

### Phase 5 — Drafting and Multi-Payer Validation

Tasks:

1. Build payer-neutral draft commands.
2. Build selected-provider validation.
3. Build reference-code validation per provider.
4. Build provider warning model.
5. Build payer selector and coverage selector logic.
6. Build unsupported-operation messages.

Deliverables:

1. Draft command handlers.
2. Validation engine.
3. Validation tests.
4. Provider-specific warning display model.

### Phase 6 — Submission, Retry, and Reconciliation

Tasks:

1. Build provider routing in submission orchestrator.
2. Build idempotency per provider.
3. Build fetch-on-timeout recovery.
4. Build provider-aware SyncPending and SyncFailed workflows.
5. Build manual reconciliation workflow.
6. Build paper fallback per provider.

Deliverables:

1. End-to-end submit/retry/reconcile coverage.
2. Duplicate-risk handling.
3. Support workflow.
4. Recovery tests.

### Phase 7 — Reference Sync and Code Crosswalk

Tasks:

1. Sync provider-specific drug lists.
2. Sync provider-specific service lists.
3. Sync national/gateway coding where required.
4. Implement code crosswalk.
5. Add freshness metrics.
6. Add checksum and version validation.

Deliverables:

1. Reference sync jobs.
2. Crosswalk tables.
3. Sync dashboard.
4. Sync alert rules.

### Phase 8 — UI Journey Completion

Tasks:

1. Build doctor prescription page.
2. Add payer/coverage selector.
3. Add provider capability-based UI controls.
4. Add provider-specific warnings.
5. Add doctor-safe error messages.
6. Add support status panel.
7. Add Persian labels.

Deliverables:

1. Doctor-facing UI.
2. Admin provider configuration UI.
3. Support dashboard.
4. UAT scripts.

### Phase 9 — Security, Certification, and Provider Evaluation

Tasks:

1. Execute security tests.
2. Execute provider-specific certification scenarios.
3. Prepare evidence package per provider.
4. Validate credential isolation.
5. Validate BOLA and RBAC.
6. Validate log masking.
7. Validate provider-specific fallback.

Deliverables:

1. Security report.
2. Provider evaluation package.
3. Certification evidence.
4. Go/no-go checklist per provider.

### Phase 10 — Pilot and Rollout

Tasks:

1. Pilot one provider first.
2. Pilot second and third providers after adapter stabilization.
3. Monitor rejection distribution per provider.
4. Monitor latency per provider.
5. Tune validation and UX.
6. Roll out by clinic, provider, and specialty.

Deliverables:

1. Pilot report per provider.
2. Rejection analysis.
3. Production readiness report.
4. Final handover document.

---

## 11) Corrected Test Strategy

### Unit tests

1. Format validators.
2. Provider capability checks.
3. Provider routing.
4. State transition guards.
5. Duplicate detection.
6. Error classifier.
7. Code crosswalk resolution.
8. Eligibility cache expiration.

### Integration tests

1. Token/session per provider.
2. Entitlement per provider.
3. Reference sync per provider.
4. Submit/fetch/edit/cancel per provider.
5. Timeout recovery per provider.
6. Provider error mapping.
7. DITAS gateway integration, if applicable.

### End-to-end tests

1. Tamin successful drug prescription.
2. Salamat successful drug prescription.
3. Armed Forces successful prescription through supported integration mode.
4. Ineligible patient blocked.
5. Multi-coverage patient chooses payer.
6. Unsupported item category blocked before submission.
7. Timeout does not create duplicate.
8. Provider outage routes to fallback.
9. Reference sync failure for one provider does not affect others.
10. Support user reconciles a duplicate-risk transaction.

### Security tests

1. Broken authentication.
2. Broken object-level authorization.
3. Cross-provider access violation.
4. Credential leakage.
5. Sensitive data exposure.
6. Rate-limit enforcement.
7. Session timeout.
8. Admin-only provider configuration protection.

---

## 12) Corrected Operational Runbook Baseline

Daily checks:

1. Token failures per provider.
2. Session failures per provider.
3. Failed queue items per provider.
4. Rejection distribution per provider.
5. Reference sync freshness per provider.
6. Latency spikes per provider.
7. Duplicate-risk incidents.
8. Audit pipeline health.
9. Provider outage status.
10. Credential expiration warnings.

Support actions:

1. Safe retry.
2. Remote status refresh.
3. Reconcile local vs provider status.
4. Export audit trail.
5. Re-sync reference data.
6. Disable provider temporarily.
7. Switch provider to fallback mode.
8. Escalate provider-specific incidents.

Incident modes:

1. Local defect.
2. Provider credential issue.
3. Provider outage.
4. DITAS gateway outage.
5. Network timeout storm.
6. Reference data mismatch.
7. Provider rule change.
8. Data mismatch between local and remote prescription.
9. Duplicate submission risk.

---

## 13) Corrected Delivery Checklist

The original 8-week timeline is realistic only for one provider.

For three providers, use a staged rollout checklist instead of a fixed timeline.

### A) Multi-Payer Foundation

- [ ] Confirm multi-payer discovery scope.
- [ ] Identify all target insurance providers:
  - [ ] Social Security / Tamin
  - [ ] Health Insurance / Salamat
  - [ ] Armed Forces Insurance
- [ ] Confirm integration mode for each provider:
  - [ ] Direct API
  - [ ] Portal API
  - [ ] DITAS-backed API
  - [ ] Manual fallback
- [ ] Prepare provider capability matrix.
- [ ] Prepare provider endpoint matrix.
- [ ] Prepare provider credential matrix.
- [ ] Define contract baseline for each provider.
- [ ] Define security baseline for multi-provider integration.
- [ ] Document provider-specific risks and unknowns.

### B) Provider-Aware Domain and Schema

- [ ] Implement provider-aware schema.
- [ ] Implement `InsuranceProvider` entity.
- [ ] Implement `ProviderCapability` entity.
- [ ] Implement `ProviderContract` entity.
- [ ] Implement `DoctorProviderCredential` entity.
- [ ] Implement `PatientCoverage` entity.
- [ ] Implement `PrescriptionProviderSubmission` entity.
- [ ] Implement provider-aware `EligibilityInquiry`.
- [ ] Implement provider-aware `PrescriptionApiTransaction`.
- [ ] Implement canonical prescription domain model.
- [ ] Implement canonical prescription state machine.
- [ ] Implement credential model per provider, clinic, and doctor.
- [ ] Prepare database migrations.
- [ ] Prepare ERD and state transition documentation.

### C) Adapter Framework

- [ ] Define `IInsuranceProviderAdapter`.
- [ ] Implement provider adapter registry.
- [ ] Implement provider adapter factory.
- [ ] Implement provider routing.
- [ ] Implement provider capability checks.
- [ ] Implement provider status normalization.
- [ ] Implement provider error mapping.
- [ ] Implement provider-specific DTO isolation.
- [ ] Implement timeout policy.
- [ ] Implement retry policy.
- [ ] Implement circuit breaker policy.
- [ ] Implement sandbox/production configuration separation.

### D) First Provider Enablement

- [ ] Select first provider for implementation.
- [ ] Implement first provider authentication.
- [ ] Implement first provider entitlement check.
- [ ] Implement first provider identity/coverage inquiry.
- [ ] Implement first provider submit flow.
- [ ] Implement first provider fetch flow.
- [ ] Implement first provider edit flow, if supported.
- [ ] Implement first provider cancel flow, if supported.
- [ ] Implement first provider reference sync baseline.
- [ ] Implement first provider draft validation.
- [ ] Implement first provider contract tests.
- [ ] Implement first provider integration tests.

### E) Second Provider Enablement

- [ ] Implement second provider adapter.
- [ ] Implement second provider authentication/session flow.
- [ ] Implement second provider entitlement check.
- [ ] Implement second provider submit flow.
- [ ] Implement second provider fetch/edit/cancel flows where supported.
- [ ] Implement provider-specific validation rules.
- [ ] Extend reference sync for second provider.
- [ ] Extend provider error mapping.
- [ ] Add second provider contract tests.
- [ ] Add second provider integration tests.

### F) Third Provider / DITAS-Backed Provider Enablement

- [ ] Implement third provider adapter or DITAS-backed adapter.
- [ ] Implement DITAS token/session handling, if required.
- [ ] Implement third provider entitlement check.
- [ ] Implement third provider submit flow.
- [ ] Implement third provider fetch/edit/cancel flows where supported.
- [ ] Implement fallback workflow for unsupported operations.
- [ ] Implement reconciliation workflow.
- [ ] Extend reference sync for third provider.
- [ ] Add third provider contract tests.
- [ ] Add third provider integration tests.

### G) Drafting, Validation, and Reference Data

- [ ] Implement payer-neutral prescription draft.
- [ ] Implement selected-provider validation.
- [ ] Implement patient coverage validation.
- [ ] Implement doctor-provider credential validation.
- [ ] Implement provider capability validation.
- [ ] Implement provider-specific required-field validation.
- [ ] Implement active reference-code validation per provider.
- [ ] Implement reference-data versioning.
- [ ] Implement reference-data checksum or change detection.
- [ ] Implement code crosswalk where needed.
- [ ] Ensure reference sync failure for one provider does not affect others.

### H) Submission, Retry, and Recovery

- [ ] Implement submission orchestrator.
- [ ] Implement provider-aware idempotency key generation.
- [ ] Store outbound transaction before external call.
- [ ] Implement fetch-on-timeout recovery.
- [ ] Implement `SyncPending` workflow.
- [ ] Implement `SyncFailed` workflow.
- [ ] Implement `DuplicateRisk` workflow.
- [ ] Implement manual reconciliation workflow.
- [ ] Implement paper fallback workflow per provider.
- [ ] Implement support-safe retry.
- [ ] Validate that retries do not create duplicate prescriptions.

### I) UI and User Journey

- [ ] Implement doctor prescription page.
- [ ] Implement payer selector.
- [ ] Implement patient coverage selector.
- [ ] Implement eligibility status panel.
- [ ] Implement provider capability-based UI controls.
- [ ] Implement provider-specific warning panel.
- [ ] Implement prescription status panel.
- [ ] Implement support status panel.
- [ ] Implement doctor-safe error messages.
- [ ] Implement Persian labels.
- [ ] Hide unsupported provider operations from normal users.
- [ ] Show support-only reconciliation actions only to authorized users.

### J) Observability and Support

- [ ] Implement provider-aware API transaction dashboard.
- [ ] Implement failed queue dashboard.
- [ ] Implement duplicate-risk dashboard.
- [ ] Implement rejection distribution by provider.
- [ ] Implement latency monitoring by provider.
- [ ] Implement token/session failure monitoring.
- [ ] Implement reference sync freshness monitoring.
- [ ] Implement credential expiration alerts.
- [ ] Implement audit export by prescription, patient, doctor, provider, and date range.
- [ ] Prepare operational runbook.

### K) Security and Compliance

- [ ] Validate TLS enforcement.
- [ ] Validate secrets are not stored in plaintext.
- [ ] Validate provider credential isolation.
- [ ] Validate token encryption at rest.
- [ ] Validate RBAC.
- [ ] Validate object-level authorization.
- [ ] Validate cross-provider authorization boundaries.
- [ ] Validate log masking for national ID, tokens, session IDs, and medical payloads.
- [ ] Validate admin action audit.
- [ ] Validate provider configuration audit.
- [ ] Validate rate limiting.
- [ ] Validate session timeout.
- [ ] Prepare security test report.

### L) Certification and Evaluation

- [ ] Prepare certification package for first provider.
- [ ] Execute first provider certification/evaluation scenarios.
- [ ] Fix first provider certification findings.
- [ ] Prepare certification package for second provider.
- [ ] Execute second provider certification/evaluation scenarios.
- [ ] Fix second provider certification findings.
- [ ] Prepare certification package for third provider.
- [ ] Execute third provider certification/evaluation scenarios.
- [ ] Fix third provider certification findings.
- [ ] Store evidence for all providers.

### M) Pilot Readiness

- [ ] Select pilot doctors.
- [ ] Select pilot clinics or centers.
- [ ] Select pilot providers.
- [ ] Enable feature flags for pilot users only.
- [ ] Train pilot users.
- [ ] Validate real patient eligibility flow.
- [ ] Validate real prescription submission flow.
- [ ] Validate provider-specific rejection handling.
- [ ] Validate support escalation process.
- [ ] Validate audit trail export.
- [ ] Validate fallback workflow.

### N) Pilot Execution and Stabilization

- [ ] Run pilot with selected doctors and selected providers.
- [ ] Monitor successful submissions.
- [ ] Monitor failed submissions.
- [ ] Monitor rejection distribution.
- [ ] Monitor duplicate-risk incidents.
- [ ] Monitor provider latency.
- [ ] Collect doctor feedback.
- [ ] Fix pilot defects.
- [ ] Tune validation rules.
- [ ] Tune doctor-facing messages.
- [ ] Tune support workflow.
- [ ] Prepare pilot report.
- [ ] Prepare go/no-go recommendation.

### O) Production Readiness

- [ ] Confirm production credentials for first provider.
- [ ] Confirm production credentials for second provider.
- [ ] Confirm production credentials for third provider.
- [ ] Confirm production feature flags.
- [ ] Confirm database migrations.
- [ ] Confirm backup and restore.
- [ ] Confirm monitoring and alerting.
- [ ] Confirm support team readiness.
- [ ] Confirm rollback strategy.
- [ ] Confirm provider outage procedure.
- [ ] Confirm paper fallback procedure.
- [ ] Confirm final security approval.
- [ ] Confirm final business approval.

### P) Phased Rollout

- [ ] Activate clean multi-payer architecture.
- [ ] Activate first provider in production.
- [ ] Monitor first provider until operationally stable.
- [ ] Activate second provider after first provider stability.
- [ ] Monitor second provider until operationally stable.
- [ ] Activate third provider after support and reconciliation flows are proven.
- [ ] Expand rollout by clinic, provider, and specialty.
- [ ] Continue monitoring provider-specific errors and rejection trends.
- [ ] Keep provider feature flags available for emergency disablement.

### Recommended Rollout Order

1. Implement the clean multi-payer architecture first.
2. Activate one provider in production.
3. Add the second provider after operational stability.
4. Add the third provider after support and reconciliation flows are proven.

---

## 14) Corrected Definition of Done

Done means the multi-payer e-prescription module is functionally complete, secure, auditable, provider-ready, and operationally supportable.

### A) Doctor Workflow Completion

- [ ] Doctors can complete the prescription workflow from one screen without duplicate data entry.
- [ ] Patient, encounter, doctor, clinic, and prescription context are reused from existing Vitarish workflows.
- [ ] The UI supports payer selection when multiple patient coverages exist.
- [ ] The UI clearly shows selected provider, coverage status, entitlement status, prescription status, and tracking result.
- [ ] Provider-specific unavailable actions are hidden or disabled with clear doctor-safe messages.
- [ ] Persian labels and user-facing messages are complete and reviewed.

### B) Patient Identity, Coverage, and Entitlement

- [ ] Patient identity verification is implemented for each enabled provider.
- [ ] Entitlement check is executed before normal electronic submission.
- [ ] Eligibility result is stored per provider, not globally.
- [ ] Patient coverage and payer selection are supported.
- [ ] Expired or stale entitlement results are not reused for submission.
- [ ] Ineligible, unknown, unavailable, and fallback states are handled explicitly.

### C) Multi-Provider Architecture

- [ ] `InsuranceProvider` registry is implemented.
- [ ] Provider capability matrix is implemented.
- [ ] Provider routing is implemented.
- [ ] Each enabled provider has its own adapter.
- [ ] Provider-specific DTOs do not leak into domain, UI, or application layers.
- [ ] Provider-specific authentication, entitlement, submission, fetch, edit, cancel, and reference sync rules are isolated inside provider adapters.
- [ ] A new insurance provider can be added without rewriting prescription domain or doctor UI.

### D) Prescription Submission and Lifecycle

- [ ] Canonical prescription state machine is implemented.
- [ ] Provider-native statuses are stored separately from canonical statuses.
- [ ] Visit, drug, paraclinic, medical service, referral, physiotherapy, dental, or other supported item categories are handled according to provider capabilities.
- [ ] Allowed fetch, edit, and cancel workflows are operational per provider.
- [ ] Illegal lifecycle transitions are blocked.
- [ ] Cancelled prescriptions cannot be re-submitted under the same local submission identity.
- [ ] Provider tracking code, remote prescription ID, correlation ID, request ID, and submission version are stored for every submission.

### E) Reference Data and Code Mapping

- [ ] Only synchronized active codes for the selected provider are selectable.
- [ ] Reference data is versioned by provider and code system.
- [ ] Provider code crosswalk is operational where needed.
- [ ] Old prescriptions remain readable after reference data changes.
- [ ] Reference sync failure for one provider does not break other providers.
- [ ] Reference freshness is visible to support/admin users.

### F) Validation and Error Handling

- [ ] Payer-neutral local validation is complete.
- [ ] Provider-specific validation is complete.
- [ ] Unsupported operations are blocked before external API calls.
- [ ] Provider warnings are surfaced before final confirmation when required.
- [ ] Provider-specific errors are mapped to canonical user-safe messages.
- [ ] No provider warning, rejection, or error is silently ignored.
- [ ] Error categories support authentication, authorization, entitlement, validation, business-rule, timeout, outage, duplicate-risk, and unknown-error scenarios.

### G) Idempotency, Retry, and Recovery

- [ ] Provider-aware idempotency keys are implemented.
- [ ] Outbound transactions are stored before external API calls.
- [ ] Timeout recovery uses fetch/status inquiry before retry.
- [ ] Duplicate-risk state is implemented.
- [ ] `SyncPending` and `SyncFailed` recovery workflows are implemented.
- [ ] Support-safe retry is implemented.
- [ ] Manual reconciliation workflow is implemented.
- [ ] Paper fallback workflow is implemented per provider where allowed.
- [ ] Tests prove retries do not create duplicate prescriptions.

### H) Auditability and Observability

- [ ] Every create, edit, cancel, submit, fetch, print, retry, fallback, and reconciliation action is audited.
- [ ] Every lifecycle transition emits an auditable event.
- [ ] API transaction logs include provider key, operation name, request hash, request ID, correlation ID, duration, response code, success flag, and error category.
- [ ] Provider-native response codes and statuses are preserved for support.
- [ ] Support dashboards show failed transactions, duplicate-risk items, rejection trends, latency, token/session failures, and reference sync freshness.
- [ ] Audit export is available by prescription, patient, doctor, provider, and date range.

### I) Security and Privacy

- [ ] TLS is enforced for internal and external transport.
- [ ] Secrets are stored in protected storage, never plaintext configuration.
- [ ] Tokens and provider credentials are encrypted or protected at rest.
- [ ] RBAC is enforced.
- [ ] Object-level authorization is enforced for every command and query.
- [ ] Cross-provider access boundaries are validated.
- [ ] A user authorized for one provider cannot access another provider’s credentials, submissions, or support actions unless explicitly permitted.
- [ ] National ID, mobile number, tokens, session IDs, barcodes, and medical payloads are masked in logs.
- [ ] Rate limiting and session timeout are implemented.
- [ ] Admin configuration changes are audited.
- [ ] Security tests cover broken authentication, broken object-level authorization, sensitive data exposure, excessive data exposure, unsafe third-party API consumption, and insufficient logging.

### J) Testing Completion

- [ ] Unit tests pass.
- [ ] Integration tests pass for every enabled provider.
- [ ] Contract tests pass for every provider adapter.
- [ ] End-to-end tests pass for happy paths and failure paths.
- [ ] Timeout and duplicate-risk tests pass.
- [ ] Reference sync tests pass.
- [ ] Cross-provider isolation tests pass.
- [ ] Security tests pass.
- [ ] No critical or high-severity defect remains open.
- [ ] Medium-severity defects are either fixed or explicitly accepted by product/security owners.

### K) Certification and Provider Readiness

- [ ] Certification or evaluation evidence package is prepared per provider.
- [ ] Provider-specific test scenarios are executed.
- [ ] Provider-specific findings are fixed or formally accepted.
- [ ] Sandbox credentials and production credentials are separated.
- [ ] Production endpoint configuration is validated.
- [ ] Provider capability matrix is approved.
- [ ] Go-live checklist is completed per provider.

### L) Operational Readiness

- [ ] Support runbook is complete.
- [ ] Incident modes are documented:
  - local defect
  - provider outage
  - DITAS/API gateway outage
  - credential issue
  - timeout storm
  - reference data mismatch
  - duplicate submission risk
- [ ] Monitoring and alerting are active.
- [ ] Backup and restore are validated.
- [ ] Rollback strategy is documented.
- [ ] Provider feature flags can disable one provider without disabling the entire module.
- [ ] Support team can safely retry, reconcile, export audit trail, and re-sync reference data.

### M) Pilot and Production Acceptance

- [ ] Pilot doctors successfully run real workflow for at least one provider.
- [ ] Pilot includes real entitlement check, prescription submission, tracking, and support review.
- [ ] Pilot rejection reasons are analyzed.
- [ ] Pilot defects are fixed or formally accepted.
- [ ] Product owner approves production readiness.
- [ ] Security owner approves production readiness.
- [ ] Operations/support owner approves production readiness.
- [ ] First provider is ready for production activation.
- [ ] Additional providers are ready for phased activation only after operational stability is proven.

### Final Done Statement

The module is Done only when it can safely support real doctor prescription workflows for at least one insurance provider in production, while the architecture, data model, security controls, audit model, provider adapter framework, and operational processes are ready to add and operate additional providers without redesigning the prescription domain or doctor UI.

## Implementation Conventions (Vitarish-Specific)

### Required placement by layer

- **Host routable pages (`@page`)**: `src/Hosts/Main/Pages/Prescription/*`
- **Host route constants/builders**: `src/Hosts/Main/Common/AppRoutes.cs` and `src/Hosts/Main/Common/Routes/*Routes.cs`
- **Reusable Prescription UI components**: `src/Modules/Prescription/Interface/Components/*`
- **Module entry/registration**: `src/Modules/Prescription/Interface/PrescriptionModule.cs` (or `Hosting/PrescriptionModule.cs` if endpoint-heavy)
- **Use cases, CQS handlers, validators**: `src/Modules/Prescription/Application/*`
- **Domain entities/value objects/state machine**: `src/Modules/Prescription/Domain/*`
- **Provider adapters, EF, jobs, persistence**: `src/Modules/Prescription/Infrastructure/*`
- **Module aggregation**: `src/Modules/Aggregator/Modules.Aggregator/ModuleExtensions.cs` and aggregator `.csproj`
- **Tests (mirrored ownership)**: `tests/Modules/Prescription.Tests/*` and host behavior in `tests/Hosts/Main.Tests/*`

### Non-negotiable boundaries

1. Do not place routable pages in module Interface projects.
2. Do not place provider DTOs or transport contracts in Domain.
3. Do not call provider adapters directly from Host pages/components.
4. Keep command handlers responsible for state change and query handlers read-only.
5. Keep a single source of truth for route definitions (`AppRoutes` + route builders).

### Delivery increments (keep simple)

**Phase 1 — Core skeleton**
- Prescription module scaffolding across Domain/Application/Infrastructure/Interface.
- Provider registry + capability matrix + canonical lifecycle.
- Host page shell with provider-aware workflow composition.

**Phase 2 — First provider vertical slice (TAMIN)**
- Eligibility check, draft submit, fetch status, cancel (if supported).
- Transaction/audit logging + idempotency key handling.
- Reference data sync for only required code systems.

**Phase 3 — Second provider (SALAMAT)**
- Reuse same application contracts and orchestration.
- Add only adapter + mapping + provider capability config.

**Phase 4 — Third provider (ARMED_FORCES or DITAS path)**
- Integrate through direct/gateway mode behind the same `IInsuranceProviderAdapter` abstraction.

**Phase 5 — Hardening**
- Retry and timeout recovery workers.
- Certification evidence pack automation per provider.
- Operational dashboards and support queries.

### Definition of done per provider

1. Capability metadata is registered and environment-configurable.
2. Adapter contract tests pass for success, warning, rejection, timeout, and duplicate-risk scenarios.
3. Canonical-to-provider status mapping is documented and covered by tests.
4. Provider-specific reference data sync completes with versioned records, enforcing non-overlapping effective-date intervals, including record_origin in uniqueness constraints, and implementing separate constraints for logical versions and source_checksum.
5. Audit and API transaction logs include provider key and correlation identifiers.
6. Host UI enables only operations declared by provider capability matrix.
7. A rollback/fallback runbook exists for provider outages.

### Suggested filesystem blueprint

```text
src/Modules/Prescription/
  Domain/
    Entities/
    ValueObjects/
    Enums/
    Services/
  Application/
    Abstractions/
    Commands/
    Queries/
    Validators/
    Mappers/
  Infrastructure/
    Persistence/
    Providers/
      Tamin/
      Salamat/
      ArmedForces/
      Ditas/
    Jobs/
    Configuration/
  Interface/
    Components/
    DependencyInjection/
    PrescriptionModule.cs

src/Hosts/Main/
  Pages/Prescription/
  Common/Routes/PrescriptionRoutes.cs
```
