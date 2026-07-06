# Sea Procedure Lifecycle Journey

## 1. Context

- App: Platform-wide Sea consumers
- Module: Sea
- Capability: Procedure management
- Related apps: Clinic, Gym, tenant-specific Sea hosts
- Related modules: Users, Files
- Product / service: Vitarish Sea module
- Journey type: Current state with gaps
- Scenario: Coordinator manages procedures inside a selected project.
- User goal: Create and maintain procedure containers that organize ordered events and evidence.
- Business goal: Keep repeatable work steps traceable under a project.
- Scope start: User opens a project procedure list.
- Scope end: Procedure is opened, created, edited, deleted, or seeded from a template.
- Primary persona: Procedure coordinator
- Evidence sources: Static code review of Sea procedure interface components.
- Confidence level: Medium

## 2. Journey Stages

### Stage 1: Review procedure list

- **User actions:** Open project; scan procedure names and stats; select a procedure.
- **Touchpoints / channels:** `ProceduresGrid`, `StatsDisplay`.
- **User questions:** Which procedure has new event items? Which one is complete or in progress?
- **Emotions:** Focused when counts are visible; uncertain when status must be inferred.
- **Pain points:** Progress is represented through stats/state display rather than an explicit procedure lifecycle state in the inspected UI.
- **Needs:** Procedure status, new-item count, and event progress cues.
- **Opportunities:** Clarify whether procedure state is derived from event stats or should be user-controlled.
- **Success metrics:** Time to choose target procedure; procedure reopen frequency due to missed updates.

### Stage 2: Create procedure manually

- **User actions:** Click add procedure; enter procedure name; confirm or cancel.
- **Touchpoints / channels:** `ProcedureCreateDialog`.
- **User questions:** Has this procedure already been created for the project?
- **Emotions:** Efficient unless duplicate detection blocks saving.
- **Pain points:** Duplicate check is based on procedure names in the project; user sees a message box when a duplicate exists.
- **Needs:** Duplicate prevention before commit, clear error wording, easy correction.
- **Opportunities:** Show duplicate state inline as the name is typed.
- **Success metrics:** Duplicate attempt rate; procedure creation success rate.

### Stage 3: Create procedure from template

- **User actions:** Select a template procedure; confirm procedure creation; wait while template event chain, event roles, and attachments are copied.
- **Touchpoints / channels:** `ProcedureTemplatesBrowser`, `ProcedureCreateDialog`.
- **User questions:** Which template is right? What exactly will copy?
- **Emotions:** Confident if the template name is recognizable; cautious when copied content is not previewed.
- **Pain points:** Current UI lists template project/procedure names but does not show a pre-copy preview of events, roles, attachments, or reminders.
- **Needs:** Template preview and explicit copied/not-copied item categories.
- **Opportunities:** Preview event chain, roles, text/media attachments, and clarify reminder behavior before confirm.
- **Success metrics:** Template reuse rate; corrections after template creation.

### Stage 4: Edit procedure identity

- **User actions:** Open admin procedure details; edit name and description; save or close.
- **Touchpoints / channels:** `ProcedureDetails`, `ProcedureEditDialog`.
- **User questions:** Does this procedure name still represent the workflow?
- **Emotions:** Neutral.
- **Pain points:** Procedure edit focuses only on name/description; no explicit status, owner, due date, or risk fields are visible.
- **Needs:** Clear identity fields and safe update feedback.
- **Opportunities:** Add workflow metadata only if a current product requirement needs it.
- **Success metrics:** Procedure edit completion; procedure name ambiguity reports.

### Stage 5: Delete procedure safely

- **User actions:** Click delete; review confirmation; cancel or confirm.
- **Touchpoints / channels:** `ProcedureDetails`, `StrictConfirmDialog`.
- **User questions:** What event history and event items are removed with this procedure?
- **Emotions:** Careful.
- **Pain points:** Confirmation does not show event/item counts in the inspected component.
- **Needs:** Deletion consequence summary for events, attachments, reminders, and access assignments.
- **Opportunities:** Use stats to explain destructive impact before confirmation.
- **Success metrics:** Accidental deletion reports; delete cancellation rate.

## 3. Project Management Mindset Trace

| Hierarchy level | Procedure lifecycle implication |
|---|---|
| Project | Procedure exists only in a selected project. |
| Procedure | Owns name, description, stats, and event chain entry point. |
| Event | Manual and template-created procedures contain ordered events. |
| Event items | Template copy currently copies attachments; reminder copy is not evidenced in the inspected implementation. |

## 4. Documentation Architecture Audit

Artifact: `sea-procedure-lifecycle-journey.md`  
Type: Documentation journey  
Responsibility: Document procedure listing, creation, template seeding, editing, and deletion flows.

- SRP: Pass — covers only procedure lifecycle interactions.
- Single state model: Pass — procedure is documented as project-scoped and event-owning.
- Low coupling: Pass — component evidence is cited at journey level only.
- Abstraction: Pass — template behavior is described from user effect, not persistence details.
- Command-query: Pass — list/open are queries; create/edit/delete/copy are commands.
- Failure safety: Pass — duplicate names and destructive delete risks are included.
- Open/Closed: Pass — future procedure metadata can be added as a new stage or linked journey.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — avoids inventing status fields as current behavior.
- Single source of truth: Pass — procedure lifecycle journey is scoped to this file.
