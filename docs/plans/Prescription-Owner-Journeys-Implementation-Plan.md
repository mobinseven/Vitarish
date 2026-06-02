# Prescription Owner Journeys Implementation Plan

Date created: 2026-06-02

## 1. Purpose

Complete the current partial Prescription owner journeys while preserving Medical-owned reference-code responsibilities.

## 2. Proposed Artifact List

| Artifact | Type | Responsibility |
| --- | --- | --- |
| `PatientCoverageDiscoveryPort` | Port/contract | Read payer/coverage options for a patient without exposing the source patient/insurance system to Prescription core logic. |
| `GetPatientPayerOptionsQuery` | Query/read operation | Return coverage-aware payer options for a draft/patient. |
| `PatientPayerOptionDto` | Data transfer shape | Carry provider key, coverage label, priority, credential health, operational health, and capability warnings to the payer selector. |
| `PayerSelectorPanel` enhancement | Presentation/UI | Display and select one payer/provider option without owning payer state. |
| `ProviderCapabilityPolicy` | Domain/core logic | Decide whether a provider supports a prescription item category. |
| `PrescriptionAuthoringCapabilityDto` | Data transfer shape | Explain disabled authoring categories to the UI. |
| `EligibilityBlockedPanel` | Presentation/UI | Explain non-eligible/unknown eligibility outcomes and safe next actions. |
| `EligibilityRefreshPrompt` | Presentation/UI | Prompt the user to refresh stale eligibility before submission. |
| `SubmitPrescriptionConfirmationDialog` | Presentation/UI | Confirm final submission summary before executing the submit command. |
| `PrescriptionPrintAction` wiring | Presentation/UI | Expose the existing print view from the detail and fallback journeys. |
| `EditPrescriptionWorkspace` | Presentation/UI | Let a doctor prepare accepted-prescription edits in an explicit edit mode. |
| `EditPrescriptionReasonDialog` | Presentation/UI | Capture an edit reason before provider submission. |
| `CancellationPolicyNotice` | Presentation/UI | Show provider cancellation-window guidance before cancellation. |
| `RecoveryStatusNotice` | Presentation/UI | Show doctor-safe pending/outage/retry status without support internals. |
| `PrescriptionAuditTimelineQuery` | Query/read operation | Return masked owner-facing audit/timeline entries for a prescription. |
| `ReconciliationActionNoteDialog` | Presentation/UI | Require operator notes for reconciliation actions. |
| `SensitivePrescriptionMaskingPolicy` | Domain/core logic | Centralize masking rules for patient/provider identifiers in support views. |
| `CredentialRenewalNotice` | Presentation/UI | Show renewal warnings and actions in credential setup. |
| `CredentialSecretWriterPort` | Port/contract | Persist provider credential secrets through a vault/secret store boundary. |
| `CredentialSecretWriterAdapter` | Adapter/infrastructure implementation | Store credential secrets in the configured secret backend and return secret references. |
| `OwnerPrescriptionNavigation` updates | Presentation/UI | Add discoverable owner/admin/support entry points to existing routed pages. |
| Medical reference-code admin work | Separate module plan item | Complete freshness/history/governance in Medical, because Medical owns persisted provider codes. |

## 3. Dependency Direction

1. Prescription domain/application logic depends on stable ports and DTOs only.
2. UI components depend on application commands/queries and DTOs.
3. Infrastructure adapters implement ports for provider APIs, patient coverage sources, background retry, and secret storage.
4. Prescription consumes Medical code search/sync contracts; Prescription must not persist its own competing provider-code catalog.
5. Support and owner presentation surfaces read masked query results instead of reaching through domain entities or provider payloads.

## 4. State Ownership / Source of Truth

| State/rule | Owner/source of truth |
| --- | --- |
| Prescription lifecycle | `PrescriptionRecord.Status` and `PrescriptionStatus`. |
| Draft selected provider | `PrescriptionRecord.SelectedProviderKey`. |
| Latest eligibility gate | `PrescriptionRecord.LastEligibilityStatus`, `LastEligibilityCheckedAt`, `LastEligibilityTrackingCode`. |
| Prescription items | `PrescriptionRecord.Items` / `PrescriptionItem`. |
| Provider code catalog | Medical module provider-code data. |
| Retry schedule/idempotency | `PrescriptionOutboxRecord`. |
| Support escalation | `PrescriptionEscalation`. |
| Credential metadata | `DoctorProviderCredential`. |
| Credential secret material | Secret store through `CredentialSecretWriterPort`; only references are stored in Prescription. |
| Masking rules | `SensitivePrescriptionMaskingPolicy`. |

## 5. Command/Query Split

### Commands

- Keep existing state-changing commands: `SetProviderCommand`, `RecordEligibilityCommand`, `ValidateDraftCommand`, `SubmitPrescriptionCommand`, `EditPrescriptionCommand`, `CancelPrescriptionCommand`, `PaperFallbackCommand`, support recovery commands, and credential commands.
- Add no command that returns operational dashboards or list data.
- Any new secret-write operation writes a secret and returns only a secret reference/result status.

### Queries

- `GetPatientPayerOptionsQuery` reads payer options and does not mutate prescription state.
- `PrescriptionAuditTimelineQuery` reads masked audit/timeline data only.
- Existing list/detail/observability/reconciliation queries remain read-only.

## 6. Implementation Phases

### Phase 1 — Clean journey completion for doctor submission

1. Add coverage-aware payer option query behind `PatientCoverageDiscoveryPort`.
2. Enrich `PayerSelectorPanel` with coverage priority, credential health, provider health, and capability warnings.
3. Add `ProviderCapabilityPolicy` and use it in validation plus UI disabled-state projection to avoid duplicated capability rules.
4. Add `EligibilityBlockedPanel` and `EligibilityRefreshPrompt` so ineligible, unknown, unavailable, and stale eligibility states are explicit before submit.
5. Add `SubmitPrescriptionConfirmationDialog` and wire the existing print view from the detail page.

### Phase 2 — Lifecycle completion for edit/cancel/print

1. Add an explicit edit workspace that loads the accepted prescription version and stages item changes before sending `EditPrescriptionCommand`.
2. Add an edit reason dialog instead of using a generic confirm dialog.
3. Add provider cancellation-window guidance before `CancelPrescriptionCommand`.
4. Render cancellation/edit audit events in the owner timeline through a read-only audit timeline query.
5. Wire `PaperFallbackCertificate` into the paper fallback completion state.

### Phase 3 — Recovery and support hardening

1. Render `RecoveryStatusNotice` for `SyncPending`, `ProviderUnavailable`, `DuplicateRisk`, and `SyncFailed` in the owner detail/submit area.
2. Require structured notes for manual reconcile, duplicate-risk resolution, paper fallback reconciliation, refresh-with-resolution, and escalation.
3. Apply `SensitivePrescriptionMaskingPolicy` to every support queue/detail projection.
4. Add owner-visible recovery timeline entries without exposing request hashes or idempotency keys.
5. Add navigation links for support and owner operational dashboards according to role policy.

### Phase 4 — Credential setup completion

1. Add credential renewal notice and renewal action flow.
2. Add `CredentialSecretWriterPort` and infrastructure adapter so credential configuration stores secret material outside the prescription database.
3. Surface credential health in payer selection through the payer option query.
4. Add owner navigation to credential setup and operational dashboards.

### Phase 5 — Medical-owned reference-code administration

1. Keep Prescription reference sync as a utility that fetches provider data and delegates persistence to Medical.
2. Complete freshness, import history, breaking-change review, and governance UI in the Medical module, not in Prescription.
3. Keep Prescription authoring search pointed at Medical query services.

## 7. Failure-Handling Strategy

- Fail fast for invalid provider keys, missing prescription records, missing eligibility, stale eligibility, unsupported provider capabilities, and invalid credential input.
- Fail safe for provider timeouts by preserving idempotency keys, scheduling outbox recovery, and blocking duplicate submission.
- Record support actions with required notes and correlation IDs.
- Mask sensitive identifiers before rendering support views.
- Treat secret-store failures as command failures; do not persist credential records with missing/invalid secret references.
- Keep patient coverage source failures read-only and visible as non-blocking payer-option load errors unless no provider can be selected safely.

## 8. Extensibility Points, Only Where Justified

- `PatientCoverageDiscoveryPort` is justified because coverage data is external to Prescription and should not leak source-system details.
- `CredentialSecretWriterPort` is justified because secret storage is infrastructure-specific and must be replaceable.
- `ProviderCapabilityPolicy` is justified to prevent duplicated capability decisions across validation and UI disablement.
- `SensitivePrescriptionMaskingPolicy` is justified to centralize security-sensitive masking rules.

No mediator pipeline, generic repository, event sourcing, or plugin system is needed for this work.

## 9. Simplicity Check — Intentionally Not Added

- No new Prescription-owned reference-code model; Medical remains the source of truth.
- No separate CQRS subsystem beyond the existing MediatR command/query naming pattern.
- No workflow engine; each journey remains a small set of explicit UI components and command/query handlers.

## 10. Final Architecture Audit

Artifact: `PatientCoverageDiscoveryPort`
Type: `port/contract`
Responsibility: Read patient payer/coverage options from an external source.
Rule Check:

* SRP: Pass — it only discovers coverage options.
* Single active state model: Pass — it reads external coverage data and maps it into payer options without owning prescription state.
* Low coupling / Law of Demeter: Pass — consumers receive DTOs rather than source-system graphs.
* Abstraction / encapsulation: Pass — it hides patient coverage infrastructure.
* Command-query separation: Pass — read-only port.
* Validation and failure safety: Pass — implementers must return explicit failures/no-options.
* Open/Closed Principle: Pass — new sources can implement the port.
* Core protection: Pass — Prescription core does not depend on coverage infrastructure.
* KISS/YAGNI: Pass — one boundary for a present external dependency.
* Single source of truth: Pass — it does not persist coverage state.

Artifact: `ProviderCapabilityPolicy`
Type: `domain policy`
Responsibility: Decide item-category support for a provider capability set.
Rule Check:

* SRP: Pass — it only evaluates provider capability rules.
* Single active state model: Pass — it consumes provider capabilities without duplicating provider state.
* Low coupling / Law of Demeter: Pass — callers pass only provider key/capability data needed for the decision.
* Abstraction / encapsulation: Pass — capability mapping is hidden behind one policy.
* Command-query separation: Pass — pure read/decision logic.
* Validation and failure safety: Pass — unsupported or unknown categories produce explicit outcomes.
* Open/Closed Principle: Pass — new categories can be handled in one policy rather than scattered UI/validator edits.
* Core protection: Pass — no UI/provider SDK dependency.
* KISS/YAGNI: Pass — one small policy removes existing duplication risk.
* Single source of truth: Pass — one capability decision owner.

Artifact: `CredentialSecretWriterPort`
Type: `port/contract`
Responsibility: Store credential secret material and return a reference.
Rule Check:

* SRP: Pass — it only writes secrets.
* Single active state model: Pass — the secret store owns secret material; Prescription owns only references.
* Low coupling / Law of Demeter: Pass — commands do not know vault internals.
* Abstraction / encapsulation: Pass — infrastructure-specific secret storage is hidden.
* Command-query separation: Pass — state-changing write port, not a data-fetching API.
* Validation and failure safety: Pass — failures must be explicit before credential records are saved.
* Open/Closed Principle: Pass — secret backends can be replaced by adapters.
* Core protection: Pass — no vault SDK dependency in core/application logic.
* KISS/YAGNI: Pass — justified by credential security requirement.
* Single source of truth: Pass — secret material has one owner.

Artifact: `SensitivePrescriptionMaskingPolicy`
Type: `domain policy`
Responsibility: Mask sensitive prescription identifiers for support and owner read models.
Rule Check:

* SRP: Pass — it only applies masking rules.
* Single active state model: Pass — it transforms display values without owning data.
* Low coupling / Law of Demeter: Pass — callers provide scalar values, not internal entity graphs.
* Abstraction / encapsulation: Pass — masking rules are centralized.
* Command-query separation: Pass — pure read/format decision.
* Validation and failure safety: Pass — null/short values must return safe masked output.
* Open/Closed Principle: Pass — new sensitive fields extend one policy.
* Core protection: Pass — no UI/framework dependency.
* KISS/YAGNI: Pass — one security-sensitive policy is simpler than scattered masking.
* Single source of truth: Pass — one masking rule owner.

Artifact: `Prescription owner journey UI components`
Type: `presentation/UI`
Responsibility: Present journey-specific state and invoke explicit commands/queries.
Rule Check:

* SRP: Pass — each component is scoped to one visible journey concern.
* Single active state model: Pass — components display command/query DTOs and do not own domain state.
* Low coupling / Law of Demeter: Pass — components call application requests rather than provider/database internals.
* Abstraction / encapsulation: Pass — infrastructure remains behind handlers/ports.
* Command-query separation: Pass — read components query; buttons invoke commands.
* Validation and failure safety: Pass — invalid user actions are blocked or surfaced before command execution.
* Open/Closed Principle: Pass — new panels can be added without rewriting stable command handlers.
* Core protection: Pass — UI changes do not change domain state ownership.
* KISS/YAGNI: Pass — explicit components avoid a workflow engine.
* Single source of truth: Pass — UI does not duplicate lifecycle or reference-code state.
