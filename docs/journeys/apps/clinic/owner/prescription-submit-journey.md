# Eligible Patient — Successful Submission Journey

## 1. Context
- Persona: Doctor
- Trigger: Doctor starts an e-prescription for an encounter patient.
- Goal: Submit a valid prescription and receive insurance tracking information.

## 2. Entry Conditions
1. Doctor authenticated and authorized for submission.
2. Patient context available.
3. Reference data available (fresh or acceptable cached version).

## 3. Journey Stages

### Stage 1: Patient and eligibility resolution
- Search/select patient by national ID.
- Capture/confirm patient mobile.
- Run entitlement check.
- System shows eligible status and inquiry tracking information.

### Stage 2: Draft authoring
- Add visit and item rows (drug/paraclinic/medical/referral/physiotherapy if enabled).
- Save as draft.
- Run local validation and remote prechecks.

### Stage 3: Confirm and submit
- Review warnings and messages.
- Confirm final submission.
- Submit button disables while request is in-flight.
- System shows tracking code, result state, and timeline entry.

## 4. Exit Conditions
1. Local state is `Submitted` or `AcceptedByInsurance`.
2. API transaction log and audit events are persisted.
3. Print/summary view is available.

## 5. Validation and Error Expectations
- Field validation should prevent avoidable API rejection.
- Warnings must be explicit before final confirmation.
- Sensitive details are masked from logs and operator-facing errors.
