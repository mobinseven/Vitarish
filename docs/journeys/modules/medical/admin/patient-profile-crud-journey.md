# Patient Profile CRUD Journey

## 1. Context
- Product / service: Vitarish Medical module
- Journey type: Current state
- Scenario: Admin creates, finds, views, edits, or deletes a patient.
- User goal: Keep a patient profile accurate.
- Business goal: Maintain reliable patient records.
- Scope start: Admin opens Patients.
- Scope end: Patient record is saved, opened, or removed.
- Primary persona: Clinic admin / provider admin
- Evidence sources: Static code review assumptions
- Confidence level: High

## 2. Persona Snapshot
**Name / segment:** Clinic admin

**Situation:** Managing patient records during daily operations.

**Motivation:** Find and update patient information quickly.

**Key needs:** Simple create, search, edit, delete.

**Constraints:** Duplicate and active/inactive controls are absent.

**Success means:** The correct patient record is current and reachable.

## 3. Journey Summary
**Narrative:** The admin opens Patients, searches or adds a patient, completes the patient form, saves, views the profile, and later edits or deletes it when needed.

**Emotional arc:** Start: focused → Middle: confident → End: satisfied

**Biggest friction point:** No duplicate warning before save.

**Highest-value opportunity:** Add patient status and duplicate detection to the save flow.

## 4. Journey Stages

### Stage 1: Find or start
**User goal:** Locate an existing patient or start a new one.

**User actions:** Open Patients; search; select Add Patient.

**Touchpoints / channels:** Medical Patients page.

**User thoughts / questions:** “Is this patient already here?”

**Emotions:** Confident, 3, because search exists.

**Pain points:** Limited filters; no duplicate hints.

**Needs:** Search and clear empty states.

**Opportunities:** Add filters and duplicate candidates.

**Success metrics:** Search success rate; duplicate creation rate.

**Evidence:** Static code review assumption.

### Stage 2: Save and maintain
**User goal:** Create or update the patient safely.

**User actions:** Enter demographics/clinical fields; save; edit; delete if needed.

**Touchpoints / channels:** Patient edit form and details page.

**User thoughts / questions:** “Did I enter enough information?”

**Emotions:** Cautious, 3, because medical data must be accurate.

**Pain points:** Required validation is minimal.

**Needs:** Validation, status, audit-friendly deletion.

**Opportunities:** Add active/inactive status and stronger validation.

**Success metrics:** Save failures; missing required data; restoration requests.

**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points
1. Duplicate uncertainty — occurs at creation.
2. No status control — occurs during maintenance.
3. Limited filtering — occurs during lookup.

### Moments that matter
- **Moment of truth:** Saving a new patient.
- **Moment of friction:** Unsure whether the patient is a duplicate.
- **Moment of delight:** Search quickly finds the right profile.

### Design principles implied by the journey
- Prevent bad records before they are saved.
- Keep the form short but extensible.
- Make patient state visible everywhere.
