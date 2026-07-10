# Patient List, Search, and Selection

## 1. Context

- App: clinic-owner
- Module: Clinic
- Capability: Patient discovery
- Related apps: none
- Related modules: Clinic, Appointments, Sea, Communications, Medical/Prescription when linked from clinic-owner operations
- Product / service: Vitarish Clinic Owner app
- Journey type: Current state with gaps
- Scenario: Review patient list, search, sort, paginate, select a patient, or start patient creation.
- User goal: Complete the visible patient discovery workflow with clear next steps and recoverable outcomes.
- Business goal: Keep clinic operations traceable to implemented Clinic Owner routes and supporting modules.
- Scope start: User reaches /owner/clinic/patients.
- Scope end: User completes the visible action, navigates away, or reaches an explicit empty/failure/placeholder state.
- Primary persona: receptionist / clinic admin
- Evidence sources: static code review of src/Hosts/Main/Pages/Clinic/Owners/Sea/ClinicOwnerPatientsPage.razor; src/Apps/Clinic/Interface/Components/Patients/PatientsGrid.razor
- Confidence level: High

## 2. Persona Snapshot

**Name / segment:** receptionist / clinic admin

**Situation:** The user is operating inside the Clinic Owner app and needs the implemented workflow to support daily clinic operations.

**Motivation:** Finish the task quickly without losing patient, appointment, reminder, or configuration context.

**Key needs:** Clear route entry, recognizable state, safe actions, useful empty/loading/failure feedback, and cross-module ownership boundaries.

**Constraints:** Evidence is limited to static implementation inspection; runtime behavior and production analytics were not inspected.

**Success means:** The user can complete the documented path or understand that the current implementation is partial or placeholder-only.

## 3. Journey Summary

**Narrative:** The user enters the workflow through the Clinic Owner shell, follows visible controls in ClinicOwnerPatientsPage; PatientsGrid, and relies on module-owned state where Clinic composes Sea, Appointments, or Communications capabilities.

**Emotional arc:** Start: focused -> Middle: cautious while reviewing data and actions -> End: confident if the action succeeds, or informed if the visible implementation is partial.

**Biggest friction point:** Static evidence does not always expose all runtime validation, confirmation, or failure messages.

**Highest-value opportunity:** Make empty, loading, failure, and ownership boundaries explicit in UI copy and future tests.

## 4. Journey Stages

### Stage 1: Enter workflow

**User goal:** Reach the correct Clinic Owner entry point.

**User actions:** Navigate to /owner/clinic/patients or open the embedded component from a related page.

**Touchpoints / channels:** Clinic Owner web app route, page, and component composition.

**User thoughts / questions:** Am I in the right clinic workflow and can I see what to do next?

**Emotions:** Focused, 3-5, because the route or component gives a clear starting point.

**Pain points:** Deep links can redirect away when required IDs are missing or data is not found.

**Needs:** Clear route availability, page title, and fallback navigation.

**Opportunities:** Add visible route-level loading and not-found messaging before redirect where user context could be lost.

**Success metrics:** Route loads; primary component renders; missing data exits safely.

**Evidence:** src/Hosts/Main/Pages/Clinic/Owners/Sea/ClinicOwnerPatientsPage.razor; src/Apps/Clinic/Interface/Components/Patients/PatientsGrid.razor

### Stage 2: Review current state

**User goal:** Understand the records, choices, or placeholder state currently available.

**User actions:** Inspect lists, fields, panels, dialogs, calendars, or placeholder content surfaced by ClinicOwnerPatientsPage; PatientsGrid.

**Touchpoints / channels:** Razor page/component UI and supporting module client calls.

**User thoughts / questions:** What is already known, what is missing, and which action is safe?

**Emotions:** Attentive, 3-5, because operational data may affect patients, reservations, or settings.

**Pain points:** Empty states often use reload affordances; runtime errors are not always visible from static review.

**Needs:** Search/filter affordances where available, meaningful empty states, and stable IDs for detail pages.

**Opportunities:** Standardize partial/failure messages across Clinic-composed modules.

**Success metrics:** Relevant records or a clear empty/placeholder state is visible.

**Evidence:** ClinicOwnerPatientsPage; PatientsGrid in src/Hosts/Main/Pages/Clinic/Owners/Sea/ClinicOwnerPatientsPage.razor; src/Apps/Clinic/Interface/Components/Patients/PatientsGrid.razor

### Stage 3: Complete visible action

**User goal:** Finish the workflow without ambiguity.

**User actions:** Select, save, cancel, book, confirm, delete, link, unlink, send, or navigate according to the visible controls for this journey.

**Touchpoints / channels:** Clinic UI components; Sea, Appointments, or Communications clients where composed.

**User thoughts / questions:** Did the system save the change or explain why it could not?

**Emotions:** Confident, 4-5, when the UI returns to a list/detail view or refreshes after the action.

**Pain points:** Some actions are inherited from shared module components, so exact confirmation and validation copy is indirect.

**Needs:** Explicit command outcome, refresh behavior, and safe cancellation.

**Opportunities:** Add journey-linked tests for success, empty, and failure states.

**Success metrics:** The command or query completes, the page refreshes or navigates, and no duplicate state ownership is introduced.

**Evidence:** Queries: list/count patients. Commands: none.

## 5. Current Implementation Evidence

- Routes: /owner/clinic/patients
- Pages/components: ClinicOwnerPatientsPage; PatientsGrid
- Application/API/contract evidence: src/Hosts/Main/Pages/Clinic/Owners/Sea/ClinicOwnerPatientsPage.razor; src/Apps/Clinic/Interface/Components/Patients/PatientsGrid.razor
- State source of truth: Clinic patient view over Sea project-backed patient records.
- Command/query split: Queries: list/count patients. Commands: none.
- Known validation/failure behavior: Missing route parameters or failed detail loads redirect to list pages where page bases are used; component-level validation and API failures are documented only where static evidence is direct.

## 6. Gaps and Risks

- Implemented: route/component evidence exists.
- Partial: Runtime copy, permissions, validation details, and API failure paths were not fully verified by static inspection.
- Missing: Production analytics, user research, and screenshots are not included in this current-state documentation pass.
- Risks: Future implementation could duplicate state ownership unless Clinic continues to compose Appointments, Sea, and Communications instead of owning their state.

## 7. Acceptance Criteria for Documentation Completeness

- All route entry points documented.
- All visible user actions documented.
- All user-visible success, empty, loading, and failure states documented where evident.
- All cross-module dependencies documented.
- All assumptions marked explicitly.

## 8. Related Journeys

- [Clinic Owner journey collection README](README.md)
- [React interface coverage audit](../tests/react-interface-coverage-audit.md)
