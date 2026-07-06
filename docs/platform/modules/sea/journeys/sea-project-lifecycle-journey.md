# Sea Project Lifecycle Journey

## 1. Context

- App: Platform-wide Sea consumers
- Module: Sea
- Capability: Project management
- Related apps: Clinic, Gym, tenant-specific Sea hosts
- Related modules: Files, Users
- Product / service: Vitarish Sea module
- Journey type: Current state with gaps
- Scenario: Owner finds, creates, edits, personalizes, opens, and deletes Sea projects.
- User goal: Keep work grouped into clear projects that expose current activity and route users into procedures.
- Business goal: Make project containers reliable anchors for procedure, event, access, and evidence history.
- Scope start: User opens a Sea project list or tenant-specific project grid.
- Scope end: User opens a project, saves project details, cancels, or deletes the project after strict confirmation.
- Primary persona: Project owner
- Evidence sources: Static code review of Sea project interface components and plugins.
- Confidence level: Medium

## 2. Journey Stages

### Stage 1: Discover projects needing attention

- **User actions:** Open project list; scan names; notice item-count or new-item badges; reload when no projects render; select a project.
- **Touchpoints / channels:** `ProjectsGrid`, `StatsDisplay`, optional `IProjectsGridPlugin` tenant replacement.
- **User questions:** Which project changed? Which project should I open first?
- **Emotions:** Oriented when stats are visible; uncertain when no project appears.
- **Pain points:** Search state exists in the base flow, but current inspected default grid retrieves all projects and sorts by new/unread/name rather than proving server-side filtering.
- **Needs:** Clear attention ordering, reliable empty-state reload, explicit search/filter behavior.
- **Opportunities:** Document and test search semantics; add project status or archive filtering if active lists grow.
- **Success metrics:** Time to locate target project; percent of project opens from new/unread indicators.

### Stage 2: Create or edit project identity

- **User actions:** Add project; enter name and description; optionally mark as template; save or cancel.
- **Touchpoints / channels:** `ProjectEditForm`, optional `IProjectEditPlugin` tenant replacement.
- **User questions:** What is this project called? Is it reusable as a template?
- **Emotions:** Purposeful if the form is minimal; blocked if required name is empty.
- **Pain points:** Save is disabled when the project name is blank, but validation guidance beyond the disabled action is limited.
- **Needs:** Minimal required fields, clear template meaning, visible save/cancel actions.
- **Opportunities:** Add inline validation help and distinguish ordinary projects from reusable template projects in the list.
- **Success metrics:** Project creation completion rate; validation error rate.

### Stage 3: Personalize project with an avatar

- **User actions:** Upload avatar using Files stream transfer; preview image; remove avatar if needed.
- **Touchpoints / channels:** `ProjectEditForm`, `ProjectAvatar`, Files `StreamTransfer`, Files `FileImage`.
- **User questions:** Does this image represent the right project? Can I remove it?
- **Emotions:** Confident when preview updates immediately.
- **Pain points:** Avatar accepts whatever the Files transfer permits; Sea journey evidence does not show type, dimension, or size guidance.
- **Needs:** File constraints and failed-upload feedback owned by the Files boundary.
- **Opportunities:** Surface acceptable avatar media constraints in the project form.
- **Success metrics:** Avatar upload success rate; project recognition speed in list/detail surfaces.

### Stage 4: Review project details

- **User actions:** Open a project; review name, description, avatar; choose edit or delete in admin mode.
- **Touchpoints / channels:** `ProjectDetails`, `ProjectAvatar`, optional `IProjectDetailsPlugin` tenant replacement.
- **User questions:** Am I in the correct project? Do I need to update or remove it?
- **Emotions:** Calm when identity details match expected work.
- **Pain points:** Details focus on identity; project health is distributed through grids and stats rather than a consolidated detail summary.
- **Needs:** Clear project identity, admin-only mutation affordances.
- **Opportunities:** Add a project health summary that links to procedures, unread items, and access concerns.
- **Success metrics:** Misnavigation rate; project edit initiation rate from detail page.

### Stage 5: Delete project safely

- **User actions:** Click delete; review strict confirmation; cancel or confirm deletion.
- **Touchpoints / channels:** `ProjectDetails`, `StrictConfirmDialog`, Sea delete command.
- **User questions:** Am I deleting the right project and all associated workflow history?
- **Emotions:** Cautious.
- **Pain points:** Journey evidence shows strict confirmation but does not show downstream impact summary for procedures, events, attachments, or reminders.
- **Needs:** Explicit deletion consequence copy.
- **Opportunities:** Show counts of affected procedures, events, event items, and reminders before destructive confirmation.
- **Success metrics:** Accidental deletion reports; delete confirmation abandonment rate.

## 3. Project Management Mindset Trace

| Hierarchy level | Project lifecycle implication |
|---|---|
| Project | Owns name, description, template flag, avatar, stats, and entry into the rest of the module. |
| Procedure | Not edited here, but project selection scopes the procedure list. |
| Event | Project deletion/editing affects event context and history interpretation. |
| Event items | Deletion consequence should account for attachments, text notes, reminders, and plugin timeline items. |

## 4. Implementation Implications

- Keep project identity and project statistics separate in documentation: identity changes are commands; stats are read-only cues.
- Treat project avatar as a Files-backed project field, not an event attachment.
- Tenant plugins can replace project grid/edit/details rendering while preserving the project journey contract.

## 5. Documentation Architecture Audit

Artifact: `sea-project-lifecycle-journey.md`  
Type: Documentation journey  
Responsibility: Document project discovery, identity, avatar, details, and deletion user flows.

- SRP: Pass — covers only project lifecycle interactions.
- Single state model: Pass — project is the top-level source for scoped procedure/event journeys.
- Low coupling: Pass — references components as evidence without duplicating code contracts.
- Abstraction: Pass — user-facing behavior is separated from Sea client implementation.
- Command-query: Pass — discovery/detail are queries; create/edit/delete are commands.
- Failure safety: Pass — destructive delete and upload constraints are called out.
- Open/Closed: Pass — tenant plugin replacement is documented as an extension path.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — no speculative new architecture.
- Single source of truth: Pass — project lifecycle journey is scoped to this file.
