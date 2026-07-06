# Sea Template Reuse Journey

## 1. Context

- App: Platform-wide Sea consumers
- Module: Sea
- Capability: Template project and procedure reuse
- Related apps: Clinic, Gym, tenant-specific Sea hosts
- Related modules: Files, Users, Communications
- Product / service: Vitarish Sea module
- Journey type: Current state with gaps
- Scenario: Owner marks projects as templates and uses template procedures to seed new procedure workflows.
- User goal: Reuse proven procedure structure instead of recreating events and evidence manually.
- Business goal: Reduce setup time and improve workflow consistency across projects.
- Scope start: User creates/edits a project or adds a procedure with template selection available.
- Scope end: Template project/procedure is saved or a new procedure is created from a template.
- Primary persona: Project owner / procedure coordinator
- Evidence sources: Static code review of Sea project edit, project grid, procedure template browser, and procedure create logic.
- Confidence level: Medium

## 2. Journey Stages

### Stage 1: Mark a project as a template

- **User actions:** Create or edit project; enable template flag; save.
- **Touchpoints / channels:** `ProjectEditForm`, optional `IProjectEditPlugin`.
- **User questions:** Is this a real active project or a reusable pattern?
- **Emotions:** Intentional if template state is clear.
- **Pain points:** Template flag meaning is not explained in inspected default form.
- **Needs:** Clear template labeling and separation from active project work.
- **Opportunities:** Add helper text explaining how template projects appear in procedure creation.
- **Success metrics:** Template project creation rate; accidental template flag toggles.

### Stage 2: Browse template procedures

- **User actions:** Open procedure creation; browse template projects and procedures; choose none or a template procedure.
- **Touchpoints / channels:** `ProcedureTemplatesBrowser`, `ProcedureCreateDialog`.
- **User questions:** Which template procedure is closest to this workflow?
- **Emotions:** Efficient when names are clear; uncertain without preview.
- **Pain points:** Browser shows project/procedure labels but no event list, attachment count, role summary, or reminder details.
- **Needs:** Preview and copied item scope.
- **Opportunities:** Add template detail panel with event chain and item provenance.
- **Success metrics:** Template selection rate; procedure edits immediately after template creation.

### Stage 3: Create from template

- **User actions:** Confirm procedure name; duplicate name check runs; procedure is created; template event chain is copied in order.
- **Touchpoints / channels:** `ProcedureCreateDialog`, Sea event/role/attachment commands.
- **User questions:** Did everything copy correctly? What did not copy?
- **Emotions:** Productive when seeded workflow appears complete.
- **Pain points:** Current evidence shows event title/description, event read/write roles, and attachments are copied; reminder copy is not evidenced.
- **Needs:** Explicit copy summary after creation.
- **Opportunities:** Show post-create report: events copied, roles copied, attachments copied, reminders not copied/not applicable.
- **Success metrics:** Template copy success rate; missing item reports.

### Stage 4: Adapt copied procedure

- **User actions:** Open copied procedure; rename/edit events; remove irrelevant attachments; add reminders or new event items.
- **Touchpoints / channels:** `EventsGrid`, `EventDetails`, `EventItemsTimeline`, `AttachmentCreator`, `EventReminderAction`.
- **User questions:** Which parts came from the template and which are now project-specific?
- **Emotions:** Careful because stale copied evidence can mislead users.
- **Pain points:** Copied attachments may look like current project evidence unless provenance is clear.
- **Needs:** Template provenance labels and cleanup checklist.
- **Opportunities:** Mark copied items until reviewed.
- **Success metrics:** Copied item deletion/edit rate; stale evidence incidents.

## 3. Copied Content Matrix

| Content | Current evidence | User-facing documentation stance |
|---|---|---|
| Procedure name | Template can populate the procedure name before save. | Document as copied/editable. |
| Event chain order | Template events are listed and recreated sequentially. | Document as copied. |
| Event title/description | Copied to new events. | Document as copied. |
| Event reader/writer roles | Copied from template events to new events. | Document as copied. |
| Attachments | Existing attachments are copied to new event records. | Document as copied, with provenance gap. |
| Reminders | Not evidenced in procedure copy path. | Document as not proven copied; user should recreate reminders. |
| Project avatar/template flag | Project-level metadata, not procedure template payload. | Document as not copied through procedure creation. |

## 4. Documentation Architecture Audit

Artifact: `sea-template-reuse-journey.md`  
Type: Documentation journey  
Responsibility: Document current template project/procedure reuse and copied content expectations.

- SRP: Pass — covers only template reuse.
- Single state model: Pass — template project/procedure is the source for procedure seeding.
- Low coupling: Pass — references copied content without redefining command internals.
- Abstraction: Pass — separates user expectation from implementation mechanics.
- Command-query: Pass — browse is query; mark template and create from template are commands.
- Failure safety: Pass — duplicate name, stale evidence, and not-proven reminder copy are explicit.
- Open/Closed: Pass — future copy types can be added to the copied content matrix.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — does not invent a template marketplace or versioning as current behavior.
- Single source of truth: Pass — template reuse journey is scoped to this file.
