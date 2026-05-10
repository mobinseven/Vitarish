# Patient Procedure Journey

## 1. Context
- Product / service: Vitarish Medical + Sea modules
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
