# Sea Plugin Extension Journey

## 1. Context

- App: Tenant-specific Sea consumers
- Module: Sea
- Capability: Interface extension points
- Related apps: Clinic, Gym
- Related modules: Files, Communications
- Product / service: Vitarish Sea module
- Journey type: Current state with gaps
- Scenario: Tenant-specific plugins replace or extend project and event-item surfaces while preserving Sea workflow boundaries.
- User goal: Work in a tenant-appropriate interface without losing the common project → procedure → event → event items model.
- Business goal: Support app-specific workflows without forking Sea core interaction contracts.
- Scope start: Tenant plugin identifier selects a registered Sea interface plugin.
- Scope end: Plugin renders project grid/edit/details or event item creation/timeline content.
- Primary persona: Tenant-specific operator and platform maintainer
- Evidence sources: Static code review of Sea interface plugin contracts and default attachment/reminder plugins.
- Confidence level: Medium

## 2. Journey Stages

### Stage 1: Render tenant-specific project surfaces

- **User actions:** Open project list, project edit form, or project details in a tenant with a plugin identifier.
- **Touchpoints / channels:** `IProjectsGridPlugin`, `IProjectEditPlugin`, `IProjectDetailsPlugin`, `PluginIdentifiers`.
- **User questions:** Why does this project UI look tailored to my clinic/gym workflow?
- **Emotions:** Comfortable when tenant terminology matches work.
- **Pain points:** Plugin replacement must preserve core Sea callbacks for open/add/create/update/delete/cancel.
- **Needs:** Stable plugin contracts and consistent journey outcomes.
- **Opportunities:** Add plugin journey acceptance checklist per tenant.
- **Success metrics:** Tenant plugin render success; callback completion rate.

### Stage 2: Extend event item creation

- **User actions:** Open event item input; use default attachment input and action plugins such as reminders; tenant or module plugins may add more item creators.
- **Touchpoints / channels:** `IEventItemCreationPlugin`, `AttachmentPlugin`, `ReminderPlugin`, `EventItemsInput`.
- **User questions:** What kinds of items can I add to this event?
- **Emotions:** Empowered when available actions match the workflow.
- **Pain points:** Too many plugins can clutter item input if placement is not controlled.
- **Needs:** Clear main vs. action placement and consistent labels.
- **Opportunities:** Group plugin actions by evidence, follow-up, or tenant-specific activity.
- **Success metrics:** Event item creation by plugin type; abandoned item input rate.

### Stage 3: Merge plugin timeline items

- **User actions:** Review event timeline; see items from attachments, reminders, and future plugins sorted into one timeline.
- **Touchpoints / channels:** `IEventItemTimelinePlugin`, `EventTimelineItem`, `EventItemsTimeline`.
- **User questions:** Is this a complete chronological story for the event?
- **Emotions:** Trusting when all item sources load consistently.
- **Pain points:** Mixed plugin sources may have different loading/error behavior and visual styles.
- **Needs:** Consistent placeholder, error, and deletion patterns.
- **Opportunities:** Define plugin timeline visual standards.
- **Success metrics:** Timeline load failures by plugin; user confusion by item type.

## 3. Boundary Expectations

| Extension point | Allowed responsibility | Must not become |
|---|---|---|
| Project grid plugin | Tenant-specific project list rendering using Sea callbacks. | A new project state model. |
| Project edit plugin | Tenant-specific project creation/editing UI. | A bypass around Sea project commands. |
| Project details plugin | Tenant-specific project detail display/actions. | A hidden procedure/event manager. |
| Event item creation plugin | Add one event-item creation affordance. | A generic module manager or unrelated workflow. |
| Event timeline plugin | Render one source of event timeline items. | A second event history source of truth. |

## 4. Documentation Architecture Audit

Artifact: `sea-plugin-extension-journey.md`  
Type: Documentation journey  
Responsibility: Document how Sea interface plugins affect user journeys while preserving workflow boundaries.

- SRP: Pass — covers only plugin extension journeys.
- Single state model: Pass — plugins extend, not replace, the Sea hierarchy.
- Low coupling: Pass — plugin boundaries are described without tenant implementation details.
- Abstraction: Pass — focuses on extension contracts and user outcomes.
- Command-query: Pass — plugin callbacks preserve command/query split by the underlying journey.
- Failure safety: Pass — inconsistent plugin loading and callback risks are documented.
- Open/Closed: Pass — plugins are the explicit extension mechanism.
- Core protection: Pass — documentation reinforces that plugins must not create a second state model.
- KISS/YAGNI: Pass — no new plugin framework is proposed.
- Single source of truth: Pass — plugin journey is scoped to this file.
