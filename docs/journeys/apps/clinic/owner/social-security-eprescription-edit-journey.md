# Edit Submitted Prescription Journey

## 1. Context
- Persona: Doctor
- Trigger: Doctor needs to amend a submitted prescription.
- Goal: Apply allowed changes through official edit path.

## 2. Entry Conditions
1. Prescription exists in submitted/eligible-for-edit state.
2. Doctor has edit permission.
3. Insurance policy allows edit operation for the current state.

## 3. Journey Flow
1. Doctor opens submitted prescription.
2. System checks lifecycle and policy edit eligibility.
3. Doctor updates allowed fields/items.
4. Local and remote validations run.
5. Adapter sends `EditElectronicPrescription`.
6. Local lifecycle records `Edited` and refreshed remote status.

## 4. Exit Conditions
1. Edited version is stored with incremented version number.
2. Audit links original and edited states.
3. If rejected, prescription enters `NeedsCorrection` with clear reason.

## 5. Guardrails
- Disallow edits in non-editable terminal states.
- Preserve full audit trace across versions.
