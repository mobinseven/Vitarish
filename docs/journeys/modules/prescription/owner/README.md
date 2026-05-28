# Prescription Module — Owner Journey Maps

Date created: 2026-05-28

## Scope and Method

This folder documents multi-provider e-prescription journeys for clinic owners, doctors, admins, and support operators. These journey maps drive interface design — interfaces are shaped by user flows, not backend decomposition.

Implementation levels in the matrix:

- **implemented:** the user can complete the journey in current UI and supporting services.
- **partial:** enough UI/service support exists for part of the journey, but one or more objectives are missing.
- **missing:** no clear user-facing journey implementation exists yet.

## How to Use This Folder

1. Start with the [implementation matrix](./implementation-matrix.md) for per-objective status, required routes, UI surfaces, service boundaries, and known gaps.
2. Use the focused journey files for detailed operator flow, decision rules, and domain-specific implementation detail.
3. Use the acceptance criteria in each journey to define delivery scope for implementation phases.

## Key Design Principles

1. **Journey-first design** — Interfaces shaped by user flows, not by backend decomposition.
2. **Multi-provider architecture** — All journeys are provider-aware; no single-insurer assumptions.
3. **Centralized cross-cutting** — Audit, error classification, retry/recovery consumed from platform services.
4. **Generalized abstractions** — Capability matrix, transaction logging, outbox patterns reusable across modules.

## Personas

| Persona | Role | Primary Journeys |
|---------|------|-----------------|
| Doctor | Creates, submits, edits, cancels prescriptions | Happy path, edit, cancel, ineligible, timeout |
| Clinical Staff | Prepares drafts where policy allows | Happy path (draft stages) |
| Clinic Admin | Manages credentials, monitors reference data | Credential setup, reference data admin |
| Platform Admin | Registers providers, configures capabilities | Provider onboarding |
| Support Operator | Reconciles failures, retries, escalates | Support reconciliation, timeout recovery |

## Journey Map Files

### Core Doctor Journeys
- [Eligible Patient — Full Success Flow](./eligible-patient-happy-path.md)
- [Multi-Coverage Payer Selection](./multi-coverage-payer-selection.md)
- [Ineligible Patient — Blocked Submission](./ineligible-patient-blocked.md)
- [Edit Submitted Prescription](./edit-submitted-prescription.md)
- [Cancel Prescription](./cancel-prescription.md)
- [Prescription Overview and History](./prescription-overview-history.md)

### Recovery and Operations Journeys
- [Submission Timeout — Duplicate-Safe Recovery](./timeout-duplicate-safe-recovery.md)
- [Paper Fallback — Provider Outage](./paper-fallback-outage.md)
- [Reference Data Administration](./reference-data-admin.md)
- [Support Reconciliation](./support-reconciliation.md)

### Configuration Journeys
- [Provider Credential Setup](./provider-credential-setup.md)
- [Provider Onboarding](./provider-onboarding.md)

## Shared Lifecycle States

All journeys share the canonical prescription lifecycle:

```
Draft → ReadyForSubmission → Submitting → Submitted → AcceptedByProvider
                                       ↘ SyncPending → SyncFailed
                                       ↘ RejectedByProvider → NeedsCorrection → Draft
AcceptedByProvider → EditRequested → Submitting → Edited → AcceptedByProvider
AcceptedByProvider → CancelRequested → Submitting → Cancelled
Any pre-submit → PaperFallback
ProviderUnavailable → PaperFallback | RetryScheduled
```

## Cross-Journey Acceptance Outcomes

1. Doctors complete full prescription flow without duplicate data entry.
2. Ineligible/denied workflows are clearly blocked and explained.
3. Timeout/outage paths are duplicate-safe and auditable.
4. Edit/cancel actions respect official policy and lifecycle guards.
5. Admin/support roles can maintain data freshness and recover failures.
6. Multi-provider architecture allows adding providers without redesigning UI or domain.
7. Every state transition is auditable with correlation tracking.
