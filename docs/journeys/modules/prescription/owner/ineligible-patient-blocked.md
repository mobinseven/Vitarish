# Journey: Ineligible Patient — Blocked Submission

## 1. Context
- **Persona:** Doctor
- **Trigger:** Eligibility check returns non-eligible status for the selected provider.
- **Goal:** Clearly block normal e-submission and guide doctor to appropriate next action.

## 2. Entry Conditions
1. Patient identity confirmed.
2. Payer selected (or auto-selected for single-coverage patient).
3. Eligibility check executed.

## 3. Journey Stages

### Stage 1: Ineligibility Detection
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Triggers eligibility check (manual or auto on payer selection) | System calls provider adapter `CheckEntitlement`. |
| 1.2 | — | Provider returns non-eligible status code. System maps to canonical status: `NotEligible`, `Unknown`, `RequiresManualVerification`, or `ProviderUnavailable`. |

### Stage 2: User Notification
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | — | Eligibility panel shows clear, clinician-safe explanation: reason category + actionable guidance. |
| 2.2 | — | Submit button remains disabled for normal e-submission. |
| 2.3 | — | If patient has other covered providers: payer switch suggestion shown. |

### Stage 3: Available Actions
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | (Optional) Retry eligibility check | System re-queries provider; may return different result if timing-dependent. |
| 3.2 | (Optional) Switch to different payer | Follows multi-coverage selection flow. |
| 3.3 | (Optional) Trigger paper fallback (if policy allows) | Follows paper-fallback-outage journey; requires explicit reason. |
| 3.4 | (Optional) Close without action | Draft remains in `Draft` state; no submission attempted. |

## 4. Exit Conditions
1. No normal insurance submission sent for ineligible status.
2. Entitlement result + tracking metadata logged (provider_key, raw_status_code, inquiry_id).
3. Doctor has clear guidance on next steps.

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| `Unknown` eligibility status | "Eligibility could not be confirmed" with retry option | No submission allowed; retry permitted |
| `RequiresManualVerification` | "Manual verification required — contact support" | Blocks e-submission; support action link |
| `ProviderUnavailable` | "Insurance service temporarily unavailable" with retry/fallback options | Does not count as ineligible; fallback path available |
| Repeated retry fails | Retry count shown; suggests paper fallback | Rate-limits retries per session |

## 6. Acceptance Criteria
1. Normal e-submission is never allowed when eligibility is not confirmed positive.
2. Doctor sees human-readable reason, never raw API error codes.
3. Provider-native status codes are preserved in audit/transaction logs for support.
4. Retry is available but rate-limited.
5. Paper fallback is accessible only when policy allows and requires explicit justification.
6. Switching payers does not lose existing draft data.
