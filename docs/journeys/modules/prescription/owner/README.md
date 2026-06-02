# Prescription Module — Owner Journey Maps

Date created: 2026-05-28
Last implementation alignment: 2026-06-02

## Scope and Method

This folder documents multi-provider e-prescription journeys for clinic owners, doctors, admins, and support operators. These journey maps drive interface design — interfaces are shaped by user flows, not backend decomposition.

Implementation levels in the matrix:

- **implemented:** the user can complete the journey in current UI and supporting services.
- **partial:** enough UI/service support exists for part of the journey, but one or more objectives are missing.
- **missing:** no clear user-facing journey implementation exists yet.

## Current Implementation Summary

The current module has a working prescription architecture and routed UI/service slices, but the owner journeys remain partial until the remaining user-facing gaps are closed. The implementation matrix is the authoritative status summary.

- **Doctor prescription flow:** partial — create draft, select provider, check and persist eligibility, add/remove Medical-owned provider-code-backed items, validate, submit, view status/tracking, request edit/cancel, and enter paper fallback are present.
- **Recovery/support flow:** partial — timeout classification, outbox retry, fetch-before-retry, reconciliation route/actions, escalation records, and operational dashboard components exist; owner-facing recovery notifications, masking/audit detail, and navigation polish remain gaps.
- **Reference-code administration:** partial admin utility — Prescription can fetch provider reference data and delegate code persistence to the Medical module; freshness/history/governance remain Medical-owned implementation work rather than separate Prescription owner journeys.
- **Credential setup:** partial — routed credential grid/configure/test/revoke exists; renewal, main navigation, secret lifecycle hardening, and payer-selector health integration remain gaps.

### Current Host Routes

| Route | Purpose |
| --- | --- |
| `/owner/clinic/prescriptions` | Owner prescription list/history. |
| `/owner/clinic/prescriptions/new` | Create a new owner prescription draft. |
| `/owner/clinic/prescriptions/new/{patientId:guid}` | Create a new draft for a known patient. |
| `/owner/clinic/prescriptions/{prescriptionId:guid}` | Owner prescription detail/workspace. |
| `/owner/clinic/prescriptions/admin/operational` | Owner/admin operational prescription dashboard. |
| `/owner/clinic/prescriptions/admin/reference-data` | Owner/admin provider reference-code sync utility. |
| `/owner/clinic/prescriptions/admin/credentials` | Owner/admin credential setup. |
| `/support/prescriptions/reconciliation` | Support reconciliation desk. |

## How to Use This Folder

1. Start with the [implementation matrix](./implementation-matrix.md) for per-objective status, required routes, UI surfaces, service boundaries, and known gaps.
2. Use the focused journey files for detailed operator flow, decision rules, and domain-specific implementation detail.
3. Use the acceptance criteria in each journey to define delivery scope for implementation phases.

## Key Design Principles

1. **Journey-first design** — Interfaces shaped by user flows, not by backend decomposition.
2. **Multi-provider architecture** — All journeys are provider-aware; no single-insurer assumptions.
3. **Centralized cross-cutting** — Audit, error classification, retry/recovery consumed from platform services.
4. **Generalized abstractions** — Capability matrix, transaction logging, and outbox patterns are consumed through stable contracts and should remain reusable across modules.
5. **Medical-owned reference codes** — Prescription consumes provider code search/sync boundaries; Medical remains the source of truth for persisted provider codes.

## Personas

| Persona | Role | Primary Journeys |
|---------|------|-----------------|
| Doctor | Creates, submits, edits, cancels prescriptions | Happy path, edit, cancel, ineligible, timeout |
| Clinical Staff | Prepares drafts where policy allows | Happy path draft stages |
| Clinic Admin | Manages credentials and monitors prescription operations | Credential setup, overview/history, operational dashboard |
| Support Operator | Reconciles failures, retries, escalates | Support reconciliation, timeout recovery |

## Journey Map Files

### Core Doctor Journeys
- [Eligible Patient — Full Success Flow](./eligible-patient-happy-path.md)
- [Provider/Payer Selection](./multi-coverage-payer-selection.md)
- [Ineligible Patient — Blocked Submission](./ineligible-patient-blocked.md)
- [Edit Submitted Prescription](./edit-submitted-prescription.md)
- [Cancel Prescription](./cancel-prescription.md)
- [Prescription Overview and History](./prescription-overview-history.md)

### Recovery and Operations Journeys
- [Submission Timeout — Duplicate-Safe Recovery](./timeout-duplicate-safe-recovery.md)
- [Paper Fallback — Provider Outage](./paper-fallback-outage.md)
- [Support Reconciliation](./support-reconciliation.md)

### Configuration Journeys
- [Provider Credential Setup](./provider-credential-setup.md)

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
SyncFailed | DuplicateRisk | PaperFallback → ManuallyReconciled
```

## Cross-Journey Acceptance Outcomes

1. Doctors complete full prescription flow without duplicate data entry.
2. Ineligible/denied workflows are clearly blocked and explained.
3. Timeout/outage paths are duplicate-safe and auditable.
4. Edit/cancel actions respect official policy and lifecycle guards.
5. Admin/support roles can maintain credentials, monitor operations, and recover failures.
6. Multi-provider architecture allows adding provider adapters without redesigning UI or domain.
7. Every state transition is auditable with correlation tracking.
