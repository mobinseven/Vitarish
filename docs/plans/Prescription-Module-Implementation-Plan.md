# Prescription Module — Enhanced Phased Implementation Plan

## Philosophy

The original plan is comprehensive but suffers from three weaknesses this enhanced plan corrects:

- **Journey-first design** — Interfaces must be shaped by user journey maps, not by backend decomposition. We create journey maps first, then derive interfaces and services.
- **Centralized cross-cutting concerns** — Audit, observability, error mapping, retry/recovery, and validation should be centralized services consumed by the module, not scattered per-submodule.
- **Generalized abstractions** — Where the original plan creates prescription-specific infrastructure (outbox, error classification, capability matrix), we generalize these so other modules benefit and debugging is uniform platform-wide.

## Pre-Phase: User Journey Maps

**Status: Complete**

Before any code, create journey maps under `docs/journeys/modules/prescription/owner/` following the established pattern (see `docs/journeys/apps/clinic/owner/`):

### Journey maps to create

| # | Journey Map | Persona | Purpose |
|---|-------------|---------|---------|
| 1 | `eligible-patient-happy-path.md` | Doctor | Full success flow from encounter → eligibility → draft → submit → tracking |
| 2 | `multi-coverage-payer-selection.md` | Doctor | Patient has multiple insurers; doctor selects payer |
| 3 | `ineligible-patient-blocked.md` | Doctor | Entitlement fails; UI blocks submission clearly |
| 4 | `timeout-duplicate-safe-recovery.md` | Doctor/Support | Submission timeout; system prevents duplicates |
| 5 | `edit-submitted-prescription.md` | Doctor | Modify accepted prescription within provider rules |
| 6 | `cancel-prescription.md` | Doctor | Cancel/delete an existing submission |
| 7 | `paper-fallback-outage.md` | Doctor/Admin | Provider outage; switch to manual with audit trail |
| 8 | `reference-data-admin.md` | Admin | View freshness, trigger sync, handle sync failures |
| 9 | `support-reconciliation.md` | Support Operator | Filter failed txns, safe retry, reconcile, escalate |
| 10 | `provider-credential-setup.md` | Clinic Admin | Map doctor credentials to providers, manage expiration |
| 11 | `provider-onboarding.md` | Platform Admin | Register new provider, configure capabilities |
| 12 | `prescription-overview-history.md` | Doctor/Staff | View, search, filter past prescriptions per patient |

### Each journey map must include:

- Persona and trigger
- Entry conditions
- Step-by-step stages (what the user sees and does)
- System responses at each step
- Error/edge states and how they manifest in UI
- Exit conditions
- Acceptance criteria


## Phase 1: Centralized Platform Infrastructure (Generalized)

**Status: Complete**

**Goal:** Build/enhance platform-level services that the prescription module (and future modules) will consume. This increases debuggability and observability uniformly.

### 1.1 Centralized Audit Pipeline
Generalize audit logging into a platform service (if not already) with standard schema: actor, entity, action, old_state, new_state, correlation_id, provider_key (optional), timestamp.

Prescription module emits audit events through this pipeline — does not own its own audit infrastructure.

### 1.2 Centralized API Transaction Logger
Create a generalized `IExternalApiTransactionLogger` service that any module can use to log outbound API calls with: provider key, operation, request hash, correlation ID, duration, response code, error category.

Prescription module consumes this; future modules (Communications, Intelligence) also can.

### 1.3 Centralized Retry/Outbox Infrastructure
Generalize a provider-aware outbox/retry worker pattern (leveraging existing Hangfire infrastructure) that supports: idempotency keys, fetch-before-retry, duplicate-risk detection, configurable backoff per provider.

Prescription module registers its retry strategies with this infrastructure.

### 1.4 Centralized Error Classification
Define a platform-level canonical error taxonomy: Authentication, Authorization, Validation, BusinessRule, Timeout, Outage, DuplicateRisk, RateLimit, Unknown.

Provider adapters map their errors into this taxonomy; UI error rendering consumes the taxonomy uniformly.

### 1.5 Centralized Provider Capability Framework
Generalize the capability-matrix pattern so any integration module can declare capabilities and the platform can gate UI/operations accordingly.

Pattern: `ICapabilityProvider<TCapability>` → `CapabilityRegistry` → UI consumption via `CapabilityGate` component.

## Phase 2: Module Skeleton & Domain Foundation

**Status: Complete**

**Goal:** Create the prescription module structure and domain core.

### 2.1 Module scaffolding
Create `src/Modules/Prescription/{Domain,Application,Infrastructure,Interface}` projects.

Register in Module Aggregator.

Add `PrescriptionModule.cs` with DI registration following existing conventions (transient DbContext).

### 2.2 Domain entities and value objects
- InsuranceProvider (registry entry)
- InsuranceProviderCapability (capability flags per provider)
- ProviderContract (clinic-provider relationship)
- DoctorProviderCredential (doctor-provider auth mapping)
- PatientCoverage (patient-provider coverage record)
- EligibilityInquiry (provider-aware entitlement check result)
- Prescription (payer-neutral clinical aggregate)
- PrescriptionItem (visit/drug/paraclinic/etc.)
- PrescriptionProviderSubmission (remote submission record)
- ReferenceCode + ReferenceCodeCrosswalk

### 2.3 Canonical state machine
Implement prescription lifecycle with domain-enforced transition guards.

Every transition emits a domain event (consumed by audit pipeline).

States: Draft → ReadyForSubmission → Submitting → Submitted → AcceptedByProvider → Edited → Cancelled + error/recovery states.

## Phase 3: Adapter Framework & Provider Registry

**Status: Complete**

**Goal:** Build the multi-provider integration layer.

### 3.1 `IInsuranceProviderAdapter` interface
Define the canonical adapter contract: Authenticate, CheckEntitlement, SubmitPrescription, FetchPrescription, EditPrescription, CancelPrescription, FetchReferenceData, MapError, NormalizeStatus.

Unsupported operations return NotSupported result (not exceptions).

### 3.2 Provider registry and routing
`InsuranceProviderRegistry` resolves adapters by provider key.

`ProviderAdapterFactory` creates the correct adapter.

Provider routing uses: provider_key + operation → adapter + capability check.

### 3.3 Provider error mapping
Each adapter maps provider-specific errors → centralized error taxonomy.

`ProviderErrorMapper` per adapter; consumed by centralized error classification.

### 3.4 Resilience policies
Timeout, retry, circuit-breaker per provider (using Polly or similar).

Configuration-driven thresholds per provider + environment.

### 3.5 `NullInsuranceAdapter`
For disabled/manual providers; returns graceful "not available" for all operations.

## Phase 4: First Provider Vertical Slice (Tamin)

**Status: Complete**

**Goal:** End-to-end working flow for one provider, guided by journey maps.

### 4.1 Tamin adapter implementation
- Authentication/session management.
- Entitlement check.
- Prescription submission (visit + drug minimum).
- Fetch registered prescription.
- Edit/cancel where API supports.
- Reference data sync (drug list, service list).

### 4.2 Application layer
- **Commands:** CreateDraft, AddItem, RemoveItem, ValidateDraft, SubmitPrescription, EditPrescription, CancelPrescription.
- **Queries:** GetPrescription, GetPrescriptionHistory, GetEligibility, SearchReferenceCodes.
- **Validators:** payer-neutral + provider-specific validation pipeline.

### 4.3 Host pages and components (journey-driven)
- Prescription page shell following journey map stages.
- Patient identity + eligibility panel.
- Payer selector (shows single provider for now, ready for multi).
- Draft workspace with item grid.
- Validation + warning panel.
- Submit + result panel with tracking code.
- Status timeline.

### 4.4 Integration with centralized services
- Audit events emitted on every state transition (CreateDraft, Submit, Edit, Cancel via IAuditWriter).
- API transactions logged via centralized logger (LoggingApiTransactionLogger registered in Main host; consumed by ResilientAdapterDecorator).
- Retry/recovery registered with centralized outbox (PrescriptionOutboxService + PrescriptionRetryProcessor Hangfire job, every 5 minutes).

## Phase 5: Multi-Provider Expansion

**Status: Complete**

**Goal:** Prove the architecture by adding second and third providers.

### 5.1 Armed Forces / DITAS adapter
- Implemented `DitasInsuranceAdapter` behind same `IInsuranceProviderAdapter`.
- Capabilities: entitlement check, visit/drug/paraclinic submission, fetch, cancel. Edit returns NotSupported (DITAS requires cancel+resubmit).
- API-key-per-clinic auth model (no per-doctor session).
- `DitasErrorMapper` maps HTTP status codes to canonical error taxonomy.
- `DitasOptions` config section: `Prescription:Providers:Ditas`.

### 5.2 Salamat adapter (stub)
- Implemented `SalamatInsuranceAdapter` as a full stub (all operations return NotSupported).
- Capabilities declared: full set including edit and reference data sync.
- Stub logs intent on every call; ready for HTTP wiring once Ministry of Health API access is obtained.
- `SalamatOptions` config section: `Prescription:Providers:Salamat`.

### 5.3 Multi-coverage UX
- Added `GetPatientCoveragesQuery` + `PatientCoverageDto` — queries PatientCoverages joined with InsuranceProviders.
- Added `SetProviderCommand` + `SetProviderHandler` — updates SelectedProviderKey/SelectedPatientCoverageId on draft.
- Created `PayerSelectorPanel` component: single-coverage shows read-only label; multi-coverage activates a `MudSelect` payer picker.
- Auto-selects the sole provider when patient has exactly one active coverage.
- Integrated into `PrescriptionDetail` above the eligibility panel, visible only while status is Draft.

## Phase 6: Recovery, Reconciliation & Fallback

**Status: Complete**

**Goal:** Production resilience for all failure modes.

### 6.1 Timeout and duplicate-safe recovery
- Implemented fetch-before-retry pattern in `PrescriptionRetryProcessor`: checks provider status via `FetchPrescriptionAsync` before re-submitting.
- DuplicateRisk state with support resolution workflow (`ResolveDuplicateRiskCommand` + handler).
- Background worker escalates stale SyncPending records (>2 hours) to DuplicateRisk or SyncFailed.

### 6.2 Paper fallback
- Manual fallback workflow via `PaperFallbackCommand` with reason capture and paper tracking number.
- PaperFallback state; reconcilable after provider recovery via `ReconcilePaperFallbackCommand`.
- UI: `PaperFallbackPanel` + `PaperFallbackDialog` for doctor workflow.

### 6.3 Support reconciliation desk
- `GetReconciliationQueueQuery` provides filtered view of failed/pending transactions.
- Safe retry via `RefreshProviderStatusCommand`, manual state correction via `ManualReconcileCommand`.
- `ReconciliationDesk` component with status filtering, action buttons (refresh, resolve, reconcile).
- All actions audited via `IAuditWriter`.

## Phase 7: Reference Data & Code Crosswalk

**Goal:** Reliable, provider-scoped reference data management.

### 7.1 Sync jobs per provider
- Scheduled sync with freshness monitoring.
- Versioned import (old codes deactivated, not deleted).
- Sync failure for one provider doesn't affect others.

### 7.2 Code crosswalk
- Map between local codes, national codes, and provider codes.
- UI selectors filter by active codes for selected provider.

### 7.3 Admin dashboard
- Reference freshness per provider.
- Manual sync trigger.
- Breaking change detection alerts.

## Phase 8: Observability & Operational Readiness

**Goal:** Production supportability.

### 8.1 Dashboards
- Provider-aware API transaction dashboard.
- Rejection distribution by provider.
- Latency monitoring by provider.
- Token/session failure monitoring.
- Duplicate-risk incident tracker.

### 8.2 Alerting
- Credential expiration warnings.
- Reference data staleness alerts.
- Provider outage detection.
- Retry queue depth alerts.

### 8.3 Operational runbook
- Document incident modes and resolution paths.
- Support action procedures.

## Phase 9: Security, Certification & Compliance

**Goal:** Meet provider certification requirements.

### 9.1 Security hardening
- TLS enforcement validation.
- Credential isolation verification.
- RBAC + object-level authorization tests.
- Cross-provider access boundary tests.
- Log masking verification.

### 9.2 Certification evidence
- Per-provider certification scenario execution.
- Evidence package generation.
- Sandbox/production credential separation verification.

## Phase 10: Pilot & Phased Rollout

**Goal:** Controlled production activation.

### 10.1 Pilot
- Feature-flagged activation for selected doctors/clinics.
- One provider first; additional providers after stability proven.
- Rejection analysis and UX tuning.

### 10.2 Rollout
- Progressive activation by clinic, provider, specialty.
- Provider-level kill switches for emergency disablement.
- Continuous monitoring and feedback loop.

## Key Enhancements Over Original Plan

| Concern | Original Approach | Enhanced Approach |
|---------|-------------------|-------------------|
| Journey maps | Mentioned but not prioritized | Must be created first; drive all interface decisions |
| Audit | Module-owned submodule | Centralized platform pipeline — one schema, one query surface |
| API logging | Module-owned | Centralized IExternalApiTransactionLogger — reusable cross-module |
| Retry/outbox | Module-owned submodule | Centralized retry infrastructure — uniform retry behavior |
| Error taxonomy | Per-adapter informal mapping | Platform-level canonical error taxonomy — consistent error UX |
| Capability gating | Module-specific matrix | Generalized capability framework — reusable for any integration module |
| Observability | Per-module dashboards | Centralized provider monitoring — one place to see all external integration health |
| Debuggability | Implicit | Correlation IDs flow end-to-end; centralized transaction logs enable cross-module tracing |

## Execution Order Summary

1. **Journey Maps (Pre-Phase)** — design what users see and do — Complete
2. **Centralized Infrastructure (Phase 1)** — build once, use everywhere — Complete
3. **Module Skeleton (Phase 2)** — domain + persistence foundation — Complete
4. **Adapter Framework (Phase 3)** — multi-provider integration layer — Complete
5. **First Provider Slice (Phase 4)** — end-to-end working flow — **Complete**
6. **Multi-Provider (Phase 5)** — prove architecture extensibility — **Complete**
7. **Recovery (Phase 6)** — production failure handling — **Complete**
8. **Reference Data (Phase 7)** — code management and sync
9. **Observability (Phase 8)** — monitoring and support tools
10. **Security (Phase 9)** — certification readiness
11. **Pilot/Rollout (Phase 10)** — controlled production activation
