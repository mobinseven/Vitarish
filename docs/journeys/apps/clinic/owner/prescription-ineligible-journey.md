# Ineligible Patient — Blocked Submission Journey

## 1. Context
- Persona: Doctor
- Trigger: Eligibility check returns non-eligible status.
- Goal: Prevent improper submission and provide clear next action.

## 2. Entry Conditions
1. Patient identity selected.
2. Entitlement check requested.

## 3. Journey Flow
1. System receives `NotEligible` or equivalent entitlement denial.
2. UI shows understandable medical/business explanation.
3. Normal e-submission action remains disabled.
4. If policy allows, user can move to controlled outage/override path.

## 4. Exit Conditions
1. No normal insurance submission is sent.
2. Entitlement result + tracking metadata is logged.
3. User has explicit next-step guidance.

## 5. Guardrails
- Never allow silent bypass of eligibility gate.
- Keep technical API wording out of doctor-facing UI.
