# Patient Profile CRUD Journey

## 1. Context
- Product / service: Vitarish Clinic owner app
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

**Touchpoints / channels:** Clinic owner patients page.

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

## 6. Technical Detail Addendum

### Current implementation touchpoints
- `ClinicOwnerPatientsPage.razor` owns the `/owner/clinic/patients` route and enables the global navigation search before rendering `PatientsGrid`.
- `PatientsGrid` inherits `ProjectsGrid`, loads API-filtered `ProjectReadDto` records, maps them to `PatientReadDto`, then splits the list into `projectsViaSurgeryData` and `projectsLackSurgeryData`.
- `PatientNewPage.razor` and `PatientEditPage.razor` both render `PatientEditForm`; `PatientEditForm` inherits `ProjectEditForm`, composes `Name` from `FirstName` and `LastName`, and calls `ProjectsService.AddAsync` or `ProjectsService.UpdateAsync`.
- `PatientDetails` inherits `ProjectDetails`, displays patient metadata from `PatientReadDto`, and delegates deletion to the base `ProjectDetails.DeleteProject()` strict-confirm flow.
- PatientWriteDto and PatientReadDto store medical fields as typed metadata on top of Sea ProjectWriteDto/ProjectReadDto; the mapper uses ModelMapper and includes fallback logic to map the base project name to the patient's last name.

### Issues to address
- `PatientEditForm.SaveAsync()` only blocks an empty composed name; it should validate date of birth, contact fields, linked user selection rules, and clinically required fields before calling the Sea project service.
- Duplicate detection is absent in the patient save flow. Add a read-only duplicate-candidate query before mutation, ideally matching normalized name plus date of birth and/or phone/email/linked user.
- Patient status is not represented in `PatientReadDto`/`PatientWriteDto`; add one source-of-truth status field (for example `Active`, `Inactive`, `Archived`) before adding UI filters or badges.
- `PatientsGrid` only filters through global search and surgery-date grouping. Add explicit filter state for status, linked user, demographics, date-of-birth range, and missing clinical data without coupling grid rendering to service-specific query details.
- Deletion currently removes the underlying project; if the product means archive, introduce a patient archive/status action rather than overloading `ProjectDetails.DeleteProject()`.
