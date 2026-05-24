# Social Security Insurance E-Prescription Module — Implementation Plan

## Purpose
Deliver a production-ready Social Security Insurance e-prescription capability as an **integration layer** inside existing HIS/EMR/clinic workflows.

This plan aligns with current Vitarish architecture and avoids duplicate data entry by keeping one source of truth in existing patient, encounter, and user contexts.

---

## 0) Design Alignment with Existing Vitarish Architecture

### Architecture placement
- **Host Pages (`src/Hosts/Main/Pages`)** own routable doctor-facing prescription screens.
- **Module Interface (`src/Modules/InsuranceEprescription/Interface`)** owns reusable components (selectors, eligibility panel, status panel).
- **Module Application** owns use cases, contracts, validators, lifecycle rules.
- **Module Domain** owns entities, value objects, state transitions, and policy rules.
- **Module Infrastructure** owns EF persistence, external Insurance API adapter, outbox/retry workers, and reference-data sync jobs.
- **Module Hosting/Interface module class** owns DI and endpoint initialization.
- **Modules Aggregator** adds module registration.

### Design principles applied
- One responsibility per unit: separate identity/eligibility, drafting, adapter, reference sync, lifecycle, and audit concerns.
- One active context model: one `Prescription` aggregate with explicit lifecycle states.
- One adapter boundary: all external insurance APIs isolated behind versioned gateway contract.
- Command-query separation: state changes via commands; reads through query services/projections.
- Keep it simple: start with minimum supported operation set and extensible operation registry.

---

## 1) Scope Definition (Release 1)

### In scope
1. Doctor authentication mapping + role validation.
2. Patient identity capture + insurance entitlement check.
3. Mobile capture + validation.
4. Prescription draft workspace.
5. Submission of visit/drug/paraclinic/medical/referral (+ physiotherapy if licensed).
6. Fetch/edit/cancel where officially allowed.
7. Reference data synchronization + cache.
8. Local audit and API transaction logging.
9. Retry/idempotency and outage fallback (paper tracking workflow).
10. Certification/evaluation evidence preparation.

### Out of scope (Release 1)
- Dispensing workflows.
- Claims settlement accounting.
- Supplementary insurance integration.
- Patient mobile app.
- Advanced CDS beyond existing host capabilities.

---

## 2) Proposed Module Decomposition

Create module namespace: `InsuranceEprescription`.

1. **Identity Access Submodule**
   - Doctor-to-insurance credential mapping.
   - Token lifecycle abstraction.
   - Role-based submission authorization.

2. **Eligibility Submodule**
   - National ID verification request orchestration.
   - Entitlement status evaluation and expiration-aware caching.

3. **Prescription Drafting Submodule**
   - Draft aggregate commands.
   - Item section management (visit/drug/service types).

4. **Submission Orchestrator Submodule**
   - Submission pipeline by item category.
   - Idempotency key generation.
   - State transition coordination.

5. **Insurance API Adapter (v1)**
   - `IInsuranceEprescriptionGatewayV1` with all official operations.
   - Transport concerns: timeout/retry/circuit breaker.

6. **Reference Data Submodule**
   - Scheduled and manual sync.
   - Versioned code snapshots, active/inactive flags.

7. **Lifecycle & Recovery Submodule**
   - State machine guards.
   - SyncPending/SyncFailed recovery.
   - Paper fallback reconciliation.

8. **Audit & Compliance Submodule**
   - Append-only event log per action and transition.
   - Correlation IDs and transaction observability.

---

## 3) Domain Model and State Machine

### Core entities
- `Doctor`
- `Patient`
- `EligibilityInquiry`
- `Prescription`
- `PrescriptionItem`
- `PrescriptionApiTransaction`
- `ReferenceCode`
- `AuditLog`
- `OutageFallback`

### Lifecycle states
`Draft -> ReadyForSubmission -> Submitting -> Submitted -> AcceptedByInsurance | RejectedByInsurance -> NeedsCorrection -> Edited -> Submitted`

Side states:
- `Cancelled`
- `PaperFallback`
- `SyncPending`
- `SyncFailed`

### Non-negotiable rules
- Illegal transitions blocked in domain layer.
- Cancelled prescriptions cannot reuse same local `prescription_id` for submission.
- Retries allowed only after duplicate-risk check.
- Every transition emits auditable domain event.

---

## 4) API Adapter Contract Strategy

Define one internal versioned gateway:
- `GetToken`
- `VerifyIdentity`
- `CheckEntitlement`
- `RegisterVisitPrescription`
- `RegisterDrugPrescription`
- `RegisterParaclinicPrescription`
- `RegisterMedicalServicePrescription`
- `RegisterReferralServicePrescription`
- `RegisterPhysiotherapyPrescription` (feature-flagged by license scope)
- `FetchRegisteredPrescription`
- `EditElectronicPrescription`
- `DeleteElectronicPrescription`
- `FetchDrugList`
- `FetchServiceList`
- `FetchAllowedCount`
- `FetchPrice`

Rules:
- No UI direct API calls.
- No external DTO leakage into domain/application.
- Masked logging for patient-sensitive fields.
- Record operation name, response code, correlation ID, request hash, duration.

---

## 5) Validation Model

### Local validation
- Required fields and format checks (national ID/mobile).
- Role and object-level authorization.
- Duplicate item detection.
- Quantity/duration sanity.
- Active reference code validation.
- State-transition guard validation.

### Remote validation
- Entitlement response interpretation.
- Insurance business-rule response mapping.
- Warnings surfaced before final confirmation.

Result: no silent warning/rejection paths.

---

## 6) Data and Persistence Plan

### Storage boundaries
- Canonical prescription/draft data in module DB context.
- API transactions append-only for support and audit.
- Reference code snapshots versioned using non-overlapping effective-date intervals (PostgreSQL EXCLUDE USING GIST) and 'record_origin' field.
- Outbox table for guaranteed-safe asynchronous retries.

### Idempotency approach
- Deterministic request hash per prescription-version-operation.
- Store outbound transaction before call; reconcile by `correlation_id` + fetch-on-timeout.
- Duplicate risk state routes to explicit support action.

---

## 7) Security and Compliance Controls

- TLS enforced for all external/internal transport.
- Secrets in protected store (no plaintext in appsettings).
- Token encryption at rest and in memory lifecycle discipline.
- Strict RBAC + object-level authorization for every command/query.
- Log masking for national IDs/tokens/payloads.
- Session timeout and internal API rate limiting.
- Admin action monitoring and audit evidence export support.

---

## 8) Implementation Phases (Aligned Delivery)

### Phase 1 — Discovery & Contract Baseline
- Confirm official API access, sandbox, credentials, endpoint inventory.
- Confirm licensed operation scope (including physiotherapy decision).
- Deliverables: access checklist, endpoint matrix, credential model.

### Phase 2 — Domain + Schema Foundation
- Implement entities, migration scripts, lifecycle state machine.
- Implement audit and transaction table schema.
- Deliverables: ERD, state diagram, migrations.

### Phase 3 — Adapter + Connectivity
- Implement gateway interface and concrete adapter.
- Add timeout/retry/circuit breaker and error mapper.
- Deliverables: adapter v1, mock server tests, integration tests.

### Phase 4 — Drafting + Validation
- Build draft workspace services and item management.
- Build local validation engine and warning model.
- Deliverables: draft command handlers, validation tests.

### Phase 5 — Submission + Recovery
- Build submission orchestrator and idempotent retry logic.
- Implement fetch/edit/delete flows and fallback path.
- Deliverables: end-to-end submit/retry/reconcile coverage.

### Phase 6 — Reference Sync + Observability
- Implement scheduled/manual reference sync (using logical version and checksum constraints) and freshness metrics.
- Add support dashboards for failed transactions and queue depth.
- Deliverables: operational dashboard + alerts + runbook draft.

### Phase 7 — UI Journey Completion
- Host doctor pages + reusable module components.
- Persian labels and doctor-safe messaging.
- Deliverables: UAT scripts and acceptance checklist.

### Phase 8 — Security, Evaluation, Pilot
- Execute security tests and certification scenarios.
- Pilot with selected doctors, tune validation and UX.
- Deliverables: security report, pilot report, go/no-go.

---

## 9) Test Strategy

1. **Unit**: format validators, transition guards, duplicate detection, error classifier.
2. **Integration**: token, entitlement, list sync, submit/fetch/edit/delete, timeout recovery.
3. **E2E**: success paths, blocked ineligible path, timeout-no-duplicate recovery, fallback reconciliation.
4. **Security**: broken auth, BOLA, sensitive data exposure, rate-limit and session controls.

---

## 10) Operational Runbook Baseline

Daily checks:
- token failures
- failed queue items
- rejection distribution
- sync freshness SLA
- latency spikes
- duplicate-risk incidents
- audit pipeline health

Support actions:
- safe retry
- remote status refresh
- reconcile local vs remote
- export audit trail
- re-sync reference data

Incident modes:
- local defect
- credential issue
- external outage
- network timeout storm
- data mismatch

---

## 11) Delivery Timeline (8 Weeks)

- **Week 1**: discovery, contract, security baseline.
- **Week 2**: schema/domain/state machine.
- **Week 3**: adapter/token/entitlement.
- **Week 4**: draft + validation + reference sync.
- **Week 5**: submit/fetch/edit/delete + retry logic.
- **Week 6**: UI integration + observability.
- **Week 7**: security + certification prep.
- **Week 8**: pilot + readiness sign-off.

---

## 12) Definition of Done

Done means:
1. One-screen doctor workflow without duplicate entry.
2. Identity + entitlement checked before normal submit.
3. Only synchronized active codes selectable.
4. Tracking/correlation stored for every submission.
5. Allowed fetch/edit/delete workflows operational.
6. End-to-end auditability across lifecycle transitions.
7. Duplicate-safe timeout/outage handling.
8. Security controls validated.
9. Certification evidence package prepared.
10. Pilot doctors successfully run in real workflow.
