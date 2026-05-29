# Journey: Eligible Patient — Full Success Flow

## 1. Context
- **Persona:** Doctor
- **Trigger:** Doctor opens prescription workspace from an active patient encounter.
- **Goal:** Create, validate, and submit an e-prescription; receive insurance tracking confirmation.

## 2. Entry Conditions
1. Doctor is authenticated and has prescription submission role for the selected clinic.
2. Doctor has valid provider credentials mapped for at least one active insurance provider.
3. Patient context is available (from encounter or explicit selection).
4. Reference data is fresh (or within acceptable cache window for the target provider).

## 3. Journey Stages

### Stage 1: Patient Identity and Payer Resolution
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Opens prescription screen from encounter context | System pre-fills patient identity (national ID, name) from encounter. Shows payer selector if patient has multiple active coverages. |
| 1.2 | Confirms or selects patient national ID | System validates national ID format. |
| 1.3 | Confirms/enters patient mobile number | System validates mobile format. |
| 1.4 | Selects insurance payer (if multiple) or system auto-selects single payer | Payer selector shows only providers where doctor has active credentials and provider is enabled. |
| 1.5 | Triggers eligibility check | System calls provider adapter `CheckEntitlement`. Shows loading state. |
| 1.6 | — | System displays eligibility result: **Eligible** badge, inquiry tracking code, coverage details. Draft workspace unlocks. |

### Stage 2: Prescription Draft Authoring
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | Adds visit item (consultation type, date) | System adds visit row; validates required visit fields. |
| 2.2 | Searches and adds drug items from reference selector | Selector filters by active codes for selected provider. Auto-fills provider-specific identifiers. |
| 2.3 | Sets dosage, quantity, duration per item | Local validation runs inline (range checks, required fields). |
| 2.4 | Optionally adds paraclinic/medical/referral items (if provider supports) | Item categories disabled if provider capability flag is off. |
| 2.5 | Saves draft | System persists draft locally. State: `Draft`. |

### Stage 3: Validation and Confirmation
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | Clicks "Validate" or system auto-validates on submit | System runs payer-neutral validation + provider-specific validation pipeline. |
| 3.2 | — | If warnings exist: warning panel shows with clinician-safe messages. Doctor must acknowledge. |
| 3.3 | Reviews warnings and confirms readiness | State moves to `ReadyForSubmission`. Submit button enables. |
| 3.4 | Clicks "Submit to Insurance" | Confirmation dialog shows summary. |
| 3.5 | Confirms submission | System disables submit button. State: `Submitting`. |

### Stage 4: Submission and Result
| Step | User Action | System Response |
|------|-------------|-----------------|
| 4.1 | — | System generates idempotency key, logs outbound transaction, calls provider adapter `SubmitPrescription`. |
| 4.2 | — | On success: state moves to `AcceptedByProvider`. Result panel shows insurance tracking code, remote prescription ID. |
| 4.3 | Views result | Timeline shows lifecycle progression. Print/summary action available. |
| 4.4 | Optionally prints prescription summary | Print view renders with tracking code and item details. |

## 4. Exit Conditions
1. Prescription state is `AcceptedByProvider` (or `Submitted` if provider doesn't return immediate acceptance).
2. API transaction log records: provider_key, operation, correlation_id, request_hash, duration, response_code.
3. Audit event records: actor (doctor), action (submit), entity (prescription_id), new_state.
4. Print/summary view is accessible.

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| National ID format invalid | Inline field error; eligibility check blocked | No API call made |
| Provider credential expired | Payer selector shows provider as "credential expired" | Doctor directed to admin/credential setup |
| Eligibility check timeout | Loading state → timeout message; retry button appears | Transaction logged; no state change |
| Eligibility returns `NotEligible` | Redirects to ineligible journey (submit blocked) | See `ineligible-patient-blocked.md` |
| Validation failure (local) | Inline errors on failing fields; submit remains disabled | No API call made |
| Submission timeout | See `timeout-duplicate-safe-recovery.md` | State moves to `SyncPending` |
| Provider rejects submission | Rejection reason shown in clinician-safe language; state: `RejectedByProvider` | Full provider error preserved in audit log |

## 6. Acceptance Criteria
1. Doctor can complete full prescription from encounter context without duplicate data entry.
2. Eligibility gate prevents submission when ineligible.
3. Provider-specific reference codes are the only codes selectable for the chosen payer.
4. Submit action is disabled while in-flight (no duplicate clicks).
5. Tracking code is visible immediately on success.
6. Every state transition is auditable.
7. Technical provider errors never shown directly to doctor.
