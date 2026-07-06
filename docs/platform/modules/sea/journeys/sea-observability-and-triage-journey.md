# Sea Observability and Triage Journey

## 1. Context

- App: Platform-wide Sea consumers
- Module: Sea
- Capability: Stats, progress, unread indicators, and timeline triage
- Related apps: Clinic, Gym, tenant-specific Sea hosts
- Related modules: Files, Communications
- Product / service: Vitarish Sea module
- Journey type: Current state with gaps
- Scenario: User uses badges, progress icons, unread indicators, and stats dialogs to decide where to act.
- User goal: Quickly find new evidence, incomplete work, and events/procedures that need attention.
- Business goal: Reduce missed updates and make project/procedure/event health actionable.
- Scope start: User sees project, procedure, event, progress, unread, or stats UI.
- Scope end: User opens the relevant project/procedure/event/item or dismisses stats detail.
- Primary persona: Project participant and procedure coordinator
- Evidence sources: Static code review of Sea stats/progress/unread components and grids.
- Confidence level: Medium

## 2. Journey Stages

### Stage 1: Notice attention cues

- **User actions:** Scan project, procedure, and event lists; observe envelope badges for item counts/new items; notice unread indicator where used.
- **Touchpoints / channels:** `StatsDisplay`, `UnreadIndicator`, `ProjectsGrid`, `ProceduresGrid`, `EventsGrid`.
- **User questions:** What changed since I last checked?
- **Emotions:** Alert.
- **Pain points:** Badge conveys count and newness but not item type, urgency, or next action.
- **Needs:** Count, recency, and item-type clarity.
- **Opportunities:** Add drill-down by new attachments, reminders, audit notes, or comments.
- **Success metrics:** Time from seeing badge to opening relevant event item.

### Stage 2: Interpret progress state

- **User actions:** Click progress display; view stats dialog with events, committed events, attachments/event items, and new items.
- **Touchpoints / channels:** `ProgressDisplay`, `StatsDialog`.
- **User questions:** How complete is this procedure or event group?
- **Emotions:** Informed when counts align with expectations.
- **Pain points:** Progress icon is compact and count-based details do not explain risk, blockers, or due dates.
- **Needs:** Clear mapping between progress state and operational meaning.
- **Opportunities:** Add tooltip/helper text for progress states.
- **Success metrics:** Stats dialog opens; follow-up action after stats review.

### Stage 3: Triage into the hierarchy

- **User actions:** Open project with new items; select procedure; select event; review event item timeline.
- **Touchpoints / channels:** `ProjectsGrid`, `ProceduresGrid`, `EventsGrid`, `EventItemsTimeline`.
- **User questions:** Where exactly is the new item, and what should I do with it?
- **Emotions:** Productive when the hierarchy narrows quickly.
- **Pain points:** Counts are visible at several levels, but source item type is only clear after timeline review.
- **Needs:** Breadcrumb-style narrowing from project → procedure → event → item.
- **Opportunities:** Add “latest item” summary on project/procedure/event rows.
- **Success metrics:** Click depth to latest item; missed update reports.

### Stage 4: Resolve or acknowledge update

- **User actions:** Open item details, review reminder, commit event, add follow-up note, or delete irrelevant item if allowed.
- **Touchpoints / channels:** `AttachmentsDisplay`, `ReminderDisplay`, `EventDetails`, `AttachmentDetailsDialog`.
- **User questions:** Is this update handled? Does the team need another action?
- **Emotions:** Satisfied when no unresolved new items remain.
- **Pain points:** The inspected UI does not show an explicit acknowledge/read action in Sea components.
- **Needs:** Clear read/unread semantics and ownership.
- **Opportunities:** Add explicit acknowledge/read markers only if product requirements demand them.
- **Success metrics:** New-item count decay time; repeated visits without action.

## 3. Project Management Mindset Trace

| Hierarchy level | Triage implication |
|---|---|
| Project | First attention filter through stats/new item counts. |
| Procedure | Narrows triage to workflow group and progress. |
| Event | Shows committed state and item count/newness. |
| Event items | Source of the update: text, file/media attachment, reminder, audit note, or plugin item. |

## 4. Documentation Architecture Audit

Artifact: `sea-observability-and-triage-journey.md`  
Type: Documentation journey  
Responsibility: Document how stats, progress, unread indicators, and timelines guide user attention.

- SRP: Pass — covers only observability and triage.
- Single state model: Pass — stats aggregate the project/procedure/event/item hierarchy.
- Low coupling: Pass — does not redefine stats calculation internals.
- Abstraction: Pass — focuses on user attention cues.
- Command-query: Pass — stats review is query; follow-up actions are delegated to relevant lifecycle journeys.
- Failure safety: Pass — ambiguity in read/acknowledge semantics is documented.
- Open/Closed: Pass — future latest-item or acknowledge features can extend this doc.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — avoids inventing an acknowledgment system as current behavior.
- Single source of truth: Pass — triage journey is scoped to this file.
