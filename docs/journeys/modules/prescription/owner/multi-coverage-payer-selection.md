# Journey: Multi-Coverage Payer Selection

## 1. Context
- **Persona:** Doctor
- **Trigger:** Patient has multiple active insurance coverages; doctor must choose which payer to submit to.
- **Goal:** Select the appropriate payer with full visibility of coverage status and provider capabilities.

## Current Implementation Alignment
- **Status:** `partial`.
- **Implemented now:** `PayerSelectorPanel` lists API adapters from the provider registry using `GetAvailableProvidersQuery`, auto-selects the only provider when applicable, and applies provider changes to a draft through `SetProviderCommand`.
- **Current artifacts:** `PayerSelectorPanel`, `PrescriptionDetail`, `GetAvailableProvidersQuery`, `SetProviderCommand`, `IInsuranceProviderRegistry`.
- **Authoritative state:** `PrescriptionRecord.SelectedProviderKey` owns the selected payer/provider for a draft; available providers come from the registry, not from a prescription-owned provider catalog.
- **Known gaps:** true patient coverage discovery is not implemented, and the selector does not yet show credential health, provider online/degraded/offline status, coverage priority, or capability-driven disabled categories per option.

## 2. Entry Conditions
1. Patient identity confirmed.
2. Patient has more than one active payer/coverage option from the source patient coverage system.
3. Doctor has valid credentials for at least one of the patient's covered providers.

## 3. Journey Stages

### Stage 1: Coverage Discovery
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Enters/confirms patient national ID | System queries all known coverages for this patient across enabled providers. |
| 1.2 | — | Payer selector panel displays available coverages with: provider name, coverage type, last eligibility check date, credential status for doctor. |

### Stage 2: Payer Selection
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | Reviews coverage options | Each option shows: provider name, coverage identifier, doctor credential status (valid/expired/missing), provider operational status (online/degraded/offline). |
| 2.2 | Selects preferred payer | System highlights selected payer. Loads provider-specific capabilities for the draft workspace. |
| 2.3 | — | Item categories adjust: only categories supported by selected provider are enabled. Reference selectors reload for selected provider's code system. |

### Stage 3: Eligibility Verification for Selected Payer
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | Confirms selection and triggers eligibility check | System calls `CheckEntitlement` on selected provider adapter. |
| 3.2 | — | On eligible: draft workspace unlocks with provider-specific constraints. |
| 3.3 | — | On ineligible for selected payer: doctor can switch to another payer or follow ineligible path. |

## 4. Exit Conditions
1. Single payer is selected and confirmed eligible.
2. Draft workspace reflects selected provider's capabilities and reference data.
3. Selection is recorded on the prescription draft (`selected_provider_key`, `selected_patient_coverage_id`).

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| No valid doctor credential for any patient payer | All payer options show "credential missing/expired" | Submission blocked; admin setup link shown |
| All providers offline | Each option shows degraded/offline badge | Paper fallback action available |
| Patient has coverage but eligibility fails for all | Each checked payer shows ineligible status | Follows ineligible path or fallback |
| Coverage data stale (> configured freshness threshold) | Stale indicator badge on coverage entry | System suggests re-check; does not block selection |

## 6. Acceptance Criteria
1. Payer selector only appears when patient has multiple active coverages.
2. Single-coverage patients skip payer selection entirely (auto-select).
3. Doctor can switch payer before submission without losing draft item data.
4. Provider capability changes dynamically disable/enable item categories on payer switch.
5. Eligibility check is mandatory for the selected payer before submission.
6. Doctor cannot submit to a provider for which they have no valid credentials.
