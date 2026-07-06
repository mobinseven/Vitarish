# Sea Event Lifecycle Journey

## 1. Context

- App: Platform-wide Sea consumers
- Module: Sea
- Capability: Event chain management
- Related apps: Clinic, Gym, tenant-specific Sea hosts
- Related modules: Users, Communications, Files
- Product / service: Vitarish Sea module
- Journey type: Current state with gaps
- Scenario: Coordinator creates, orders, edits, commits, uncommits, secures, and deletes events under a procedure.
- User goal: Maintain an ordered procedure timeline with clear state and accountability.
- Business goal: Preserve step-level workflow history and make event readiness visible.
- Scope start: User opens a procedure event list.
- Scope end: Event is selected, created, reordered, edited, committed/uncommitted, access-adjusted, or deleted.
- Primary persona: Procedure coordinator and event contributor
- Evidence sources: Static code review of Sea event interface components and event item plugins.
- Confidence level: Medium

## 2. Journey Stages

### Stage 1: Review ordered events

- **User actions:** Open procedure; scan events; distinguish committed and uncommitted steps; notice redacted no-access rows; select an event.
- **Touchpoints / channels:** `EventsGrid`, `StatsDisplay`, access-controlled redaction rendering.
- **User questions:** What has happened, what remains, and what am I allowed to see?
- **Emotions:** Focused when the sequence is clear; frustrated when rows are redacted without next-step guidance.
- **Pain points:** Redacted events preserve sequence presence but hide context; users may not know whom to ask for access.
- **Needs:** Clear ordering, visible state, and access request path.
- **Opportunities:** Add access-request guidance for redacted events.
- **Success metrics:** Event selection time; redacted-event support requests.

### Stage 2: Create an event in the chain

- **User actions:** Add an event at the end or after a selected event; default title is assigned; edit dialog opens for refinement.
- **Touchpoints / channels:** `EventsGrid`, `EventEditDialog`.
- **User questions:** Where will this step appear in the procedure? Can I rename it immediately?
- **Emotions:** Efficient when inserted in expected order.
- **Pain points:** Default naming uses event count context; users still need to confirm semantic event title and description.
- **Needs:** Clear insertion point and immediate edit capability.
- **Opportunities:** Add inline title prompt before creating if empty/default names cause noise.
- **Success metrics:** Created event rename rate; event reorder frequency after creation.

### Stage 3: Reorder event chain

- **User actions:** Move event up or down in the chain.
- **Touchpoints / channels:** `EventsGrid` command handlers and chain sorting extension.
- **User questions:** Did the event move to the right place? Is the chain still valid?
- **Emotions:** Attentive.
- **Pain points:** The inspected journey evidence proves move commands, but not a visual chain-integrity warning if ordering repair is needed.
- **Needs:** Immediate refresh and chain integrity assurance.
- **Opportunities:** Surface repair/reorder status if chain commands fail or produce unexpected order.
- **Success metrics:** Reorder success rate; repair command usage.

### Stage 4: Edit event identity and access

- **User actions:** Edit title/description; open roles dialog; assign reader and writer roles; save.
- **Touchpoints / channels:** `EventDetails`, `EventEditDialog`, `EventRolesEditDialog`, `EventRolesDisplay`.
- **User questions:** Who can see this event, who can write it, and what does the step mean?
- **Emotions:** Responsible, because access affects evidence visibility.
- **Pain points:** Role assignment is role-based, not person-explained in the event dialog; users need role naming discipline.
- **Needs:** Role names that communicate real access responsibilities.
- **Opportunities:** Show affected users for each role before save.
- **Success metrics:** Event role edits per event; access correction rate.

### Stage 5: Commit or uncommit event

- **User actions:** Commit uncommitted event; uncommit committed event after confirmation; review automatic text audit item.
- **Touchpoints / channels:** `EventDetails`, `AttachmentCreator`/attachment command path, `EventItemsTimeline`.
- **User questions:** Is this step final? Is there a record that I changed the state?
- **Emotions:** Certain when commit is successful; cautious when uncommitting.
- **Pain points:** Commit/uncommit state history is represented as text attachment metadata in the event-item timeline, not as a dedicated event state history surface.
- **Needs:** Clear state transition and audit trail.
- **Opportunities:** Separate event state history from user-authored attachments if audit traceability becomes regulated.
- **Success metrics:** Commit completion rate; uncommit frequency; audit note discoverability.

### Stage 6: Delete event safely

- **User actions:** Click delete; confirm; event is removed and parent procedure refreshes.
- **Touchpoints / channels:** `EventDetails`, `StrictConfirmDialog`.
- **User questions:** What attachments, reminders, and access assignments are tied to this event?
- **Emotions:** Careful.
- **Pain points:** Confirmation does not show all event-item consequences in the inspected component.
- **Needs:** Clear deletion impact across attachments, reminders, and plugin items.
- **Opportunities:** Include event-item counts in destructive confirmation.
- **Success metrics:** Accidental deletion reports; event restore/support requests.

## 3. Project Management Mindset Trace

| Hierarchy level | Event lifecycle implication |
|---|---|
| Project | Event inherits project context for display, plugins, and permissions. |
| Procedure | Event belongs to an ordered procedure chain. |
| Event | Owns title, description, committed state, order, stats, and event role assignments. |
| Event items | Attachments, text audit notes, reminders, and plugin-rendered items describe the event over time. |

## 4. Documentation Architecture Audit

Artifact: `sea-event-lifecycle-journey.md`  
Type: Documentation journey  
Responsibility: Document ordered event creation, editing, access, commit state, reordering, and deletion flows.

- SRP: Pass — covers only event lifecycle interactions.
- Single state model: Pass — event is documented as procedure-scoped and item-owning.
- Low coupling: Pass — keeps implementation evidence separate from journey stages.
- Abstraction: Pass — describes user-visible state rather than persistence internals.
- Command-query: Pass — review is query; create/reorder/edit/commit/delete are commands.
- Failure safety: Pass — redaction, uncommit confirmation, and delete consequences are covered.
- Open/Closed: Pass — future event state history can be added without changing project/procedure docs.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — does not require a new state machine in current docs.
- Single source of truth: Pass — event lifecycle journey is scoped to this file.
