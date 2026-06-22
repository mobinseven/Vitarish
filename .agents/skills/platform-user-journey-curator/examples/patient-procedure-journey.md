# Patient Procedure Journey

## 1. Context
- Product / service: Vitarish Clinic owner app + Sea modules
- Journey type: Current state with gaps
- Scenario: Admin adds a procedure and records step-level history.
- User goal: Track care/procedure work for a patient.
- Business goal: Preserve clinical procedure history.
- Scope start: Admin opens a patient profile.
- Scope end: Procedure details, events, and notes are recorded.
- Primary persona: Provider admin / clinician
- Evidence sources: Static code review assumptions
- Confidence level: Medium

## 2. Persona Snapshot
**Name / segment:** Clinician

**Situation:** Documenting patient procedure progress.

**Motivation:** Keep procedure history understandable.

**Key needs:** Add procedure, update details, record events/notes.

**Constraints:** No procedure provider assignment or cancel status.

**Success means:** Team can see what happened and what is next.

## 3. Journey Summary
**Narrative:** The clinician opens a patient, adds a procedure, edits its name/description, opens the procedure, adds events, commits steps, and records timeline notes/attachments.

**Emotional arc:** Start: purposeful → Middle: attentive → End: informed

**Biggest friction point:** Status is inferred from events instead of shown as a procedure status.

**Highest-value opportunity:** Add procedure status, provider assignment, and cancel action.

## 4. Journey Stages

### Stage 1: Add or open procedure
**User goal:** Select the procedure to work on.

**User actions:** Open patient; add procedure or select existing procedure.

**Touchpoints / channels:** Patient details; Procedures grid.

**User thoughts / questions:** “Which procedure does this note belong to?”

**Emotions:** Focused, 3, because the list is simple.

**Pain points:** No provider/status at a glance.

**Needs:** Clear ownership and status.

**Opportunities:** Show procedure status and assigned provider in the list.

**Success metrics:** Procedure selection errors; time to locate procedure.

**Evidence:** Static code review assumption.

### Stage 2: Record history
**User goal:** Document progress.

**User actions:** Add/edit events; commit a step; add notes/attachments.

**Touchpoints / channels:** Procedure page; event page; timeline.

**User thoughts / questions:** “Is this step complete?”

**Emotions:** Careful, 4, because clinical history matters.

**Pain points:** Procedure-level cancellation is missing.

**Needs:** Status, cancel reason, provider responsibility.

**Opportunities:** Add a procedure workflow state machine.

**Success metrics:** Completed procedure documentation; missing-note rate.

**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points
1. Missing provider assignment — occurs at procedure setup.
2. Missing explicit status — occurs during tracking.
3. Missing cancel workflow — occurs when work stops.

### Moments that matter
- **Moment of truth:** Committing an event step.
- **Moment of friction:** Explaining whether a procedure is active, done, or cancelled.
- **Moment of delight:** Timeline notes preserve procedure context.

### Design principles implied by the journey
- Separate procedure status from event history.
- Keep notes close to the clinical step they describe.
- Make assignment explicit, not inferred.

## 6. Implementation Derivation

### Required artifacts

| Artifact | Why it is needed | Journey source | Priority |
|---|---|---|---|
| Clinical procedure metadata | Stores provider, status, planned date, cancellation reason, and clinical owner without leaking generic Sea semantics | Stage 1 / Stage 2 | High |
| Procedure status display | Makes active, completed, and cancelled procedure state explicit | Stage 1 | High |
| Cancel/close workflow | Preserves stopped clinical work with reason and timestamp instead of deleting history | Stage 2 | High |

### Feature slices

| Slice | User value | Includes | Excludes | Acceptance signal |
|---|---|---|---|---|
| Show provider and status | Clinician can choose the right procedure quickly | List/detail display, metadata read model | Scheduling engine | Procedure selection errors decrease |
| Cancel procedure with reason | Clinician can stop work without losing history | Cancel action, reason, timestamp, audit note | Billing integration | Cancelled procedures are visible and auditable |

### Domain / data implications

- Add medical-specific procedure metadata or wrapper.
- Keep event commit history independent from procedure status.
- Preserve clinical owner/provider assignment separately from Sea role semantics.

### UI implications

- Show provider and status in procedure list.
- Add status and cancel action to procedure details.
- Keep timeline notes close to the clinical event they describe.

### API / service implications

- Add read/write support for clinical procedure metadata.
- Add cancel/close command with validation and audit trail.

### Validation, permissions, and safety

- Only authorized clinical/admin users can change procedure status.
- Cancellation requires a reason.
- Deletion should not be the default clinical stop action.

### Tests and acceptance criteria

- Status and provider persist across reloads.
- Cancelled procedures remain visible and auditable.
- Event commits still represent step-level history.

### Metrics

- Procedure selection errors.
- Missing-note rate.
- Cancelled procedure audit completeness.

## 7. Technical Detail Addendum

### Current implementation touchpoints
- `ClinicOwnerPatientPage.razor` embeds Sea `ProceduresGrid` below `PatientDetails`; created or clicked procedures navigate to `/owner/clinic/patients/{patientId}/procedures/{procedureId}`.
- `ProceduresGrid` queries `IProceduresService.GetListAsync` with `ProcedureDescriptorDto.ProjectId`, sorts unread/new procedures first, and opens `ProcedureCreateDialog` for admin users.
- `ProcedureCreateDialog` can copy a template procedure by duplicating event chain entries, event role assignments, and attachments into the new patient procedure.
- `PatientProcedurePage.razor` renders `EventsGrid` and `ProcedureDetails`; `ProcedureDetails` exposes edit and delete actions through `ProcedureEditDialog` and `ProceduresService.DeleteAsync`.
- PatientEventPage.razor renders EventDetails plus EventItemsTimeline; EventDetails checks write permissions, commits/uncommits events through EventsService, and writes a text attachment noting the change.
- `EventItemsTimeline` renders attachment/activity items and delegates new timeline content to `EventItemsInput` plugin slots when the current user can write the event.

### Issues to address
- `ProcedureReadDto`/`ProcedureWriteDto` are used as generic Sea procedure records and do not include medical-specific provider, room, planned date, procedure status, cancellation reason, or clinical owner fields. Such fields can be stored in the procedure metadata.
- `ProceduresGrid` labels each procedure and shows aggregate stats, but it cannot show assigned provider, status, cancellation state, next step, or overdue risk because those values are not modeled.
- Procedure status is inferred from event commits and stats. Add an explicit procedure workflow state machine independent from event history, while keeping event commits as auditable step-level facts.
- `ProcedureDetails.DeleteProcedure()` permanently removes the procedure from the user journey; add a cancel/close action with a reason and timestamp for stopped clinical work.
- Event role editing in `EventDetails` is authorization/access management, not provider assignment. Provider assignment should be a medical abstraction rather than leaking Sea role semantics into clinical scheduling.
- `ProcedureEditDialog` supports only name and description. Add structured fields through a medical procedure wrapper or extension plugin so future fields do not require rewriting Sea procedure components.
