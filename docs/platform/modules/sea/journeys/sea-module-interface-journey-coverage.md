# Sea Module Interface Journey Coverage

## 1. Context

- App: Platform-wide Sea consumers
- Module: Sea
- Capability: Interface journey coverage
- Related apps: Clinic, Gym, any tenant-specific Sea host
- Related modules: Files, Communications, Users, Search, Feed
- Product / service: Vitarish Sea module
- Journey type: Current state with gaps
- Scenario: Exhaustive journey index for Sea interface components using the project → procedure → event → event items mental model.
- User goal: Understand every user path exposed by Sea interface components without losing attachment, reminder, permission, template, or statistics details.
- Business goal: Maintain one documentation source of truth for Sea module interaction coverage and future acceptance reviews.
- Scope start: User enters any Sea surface that renders project, procedure, event, event item, role, user-role, template, statistics, or plugin UI.
- Scope end: The journey is completed, blocked, cancelled, or escalated to a follow-up opportunity.
- Primary persona: Project owner, procedure coordinator, event contributor, project participant, tenant-specific plugin user
- Evidence sources: Static code review of `src/Modules/Sea/Interface/Components/**`, `src/Modules/Sea/Interface/Plugins/**`, and existing Sea journey docs.
- Confidence level: Medium

## 2. Coverage Principle

Sea interface journeys are organized around one active management hierarchy:

```txt
Project → Procedure → Event → Event items
```

Event items include advanced-input text notes, uploaded file/media attachments, reminders, and plugin-rendered timeline items. Project avatars are file-backed but belong to project setup rather than the event-item timeline.

## 3. Exhaustive Journey Inventory

| Journey | Primary persona | Source of truth | Interface scope | Dedicated doc |
|---|---|---|---|---|
| Project discovery and selection | Project owner / participant | Project list and project stats | `ProjectsGrid`, `StatsDisplay`, tenant grid plugin | `sea-project-lifecycle-journey.md` |
| Project create/edit/delete | Project owner | `ProjectReadDto` / `ProjectWriteDto` | `ProjectEditForm`, `ProjectDetails`, `ProjectAvatar`, Files stream transfer | `sea-project-lifecycle-journey.md` |
| Template project and procedure reuse | Project owner / procedure coordinator | Template flag and template procedure copy operation | `ProjectEditForm`, `ProjectsGrid`, `ProcedureTemplatesBrowser`, `ProcedureCreateDialog` | `sea-template-reuse-journey.md` |
| Procedure create/edit/delete | Procedure coordinator | Procedure record under a project | `ProceduresGrid`, `ProcedureCreateDialog`, `ProcedureEditDialog`, `ProcedureDetails` | `sea-procedure-lifecycle-journey.md` |
| Event chain creation and ordering | Procedure coordinator | Event linked chain in a procedure | `EventsGrid`, `EventEditDialog`, chain move commands | `sea-event-lifecycle-journey.md` |
| Event commit/uncommit | Event contributor / coordinator | Event committed state plus audit text attachment | `EventDetails`, `StatsDisplay`, `AttachmentPlugin` | `sea-event-lifecycle-journey.md` |
| Event role access | Project owner / access administrator | Event reader/writer role assignments | `EventRolesDisplay`, `EventRolesEditDialog`, redacted event rendering | `sea-access-and-roles-journey.md` |
| User role access | Project owner / access administrator | User-role assignments by role and read/write type | `UsersGrid`, `UserRolesSelect`, `UserRolesDisplay`, `RolesGrid`, `RoleEditDialog` | `sea-access-and-roles-journey.md` |
| Advanced attachment capture | Event contributor | Event attachment metadata and Files upload id | `EventItemsInput`, `AttachmentCreator`, `AttachmentsDisplay`, `AttachmentDetailsDialog`, `AttachmentPlugin` | `sea-event-items-journey.md` |
| Reminder capture and timeline review | Event contributor / coordinator | Communications reminder linked to event source id | `EventReminderAction`, `ReminderPlugin`, `ReminderDisplay` | `sea-event-items-journey.md` |
| Timeline review and unread triage | Project participant / coordinator | Event item timeline and stats | `EventItemsTimeline`, `StatsDisplay`, `ProgressDisplay`, `StatsDialog`, `UnreadIndicator` | `sea-observability-and-triage-journey.md` |
| Tenant plugin replacement | Tenant-specific operator | Tenant plugin identifier | `IProjectsGridPlugin`, `IProjectEditPlugin`, `IProjectDetailsPlugin`, `IEventItemPlugin` | `sea-plugin-extension-journey.md` |

## 4. Shared User Questions Across Journeys

- **Project:** Which project needs attention, and do I have permission to change it?
- **Procedure:** Which procedure represents the current workflow, and was it created from the right template?
- **Event:** What is the next ordered step, who may read or write it, and is it committed?
- **Event items:** What evidence, media, text, or reminder is attached to this step, and who can see it?
- **Stats:** Are unread or newly added items telling me to act now?
- **Tenant plugin:** Is this the default Sea experience or a tenant-specific rendering of the same Sea workflow?

## 5. Coverage Gaps Found During Inspection

| Gap | User impact | Affected journeys | Documentation implication |
|---|---|---|---|
| Project search descriptor is constructed but the current list path loads all projects before ordering. | Users may expect search filtering from the interface base but journey evidence only proves ordering and reload behavior. | Project discovery | Keep search as a supported surface question, not a guaranteed filtering behavior. |
| Procedure template copy includes event titles, descriptions, event role assignments, and attachments, but not reminder items. | Coordinators may assume all event items copy from templates when reminders are currently timeline plugins, not procedure-template payload. | Template reuse, event items | Call out copied vs. not-proven copied event-item details. |
| Commit/uncommit adds text attachment audit notes. | Audit evidence appears in attachment timeline rather than a separate event state history component. | Event lifecycle, event items | Treat commit audit note as an event item in journeys. |
| Attachment advanced input accepts text and uploaded files/media through one surface. | Users may mix narrative notes, images, videos, and files in a single event evidence flow. | Event items | Document advanced input as a first-class event-item journey. |
| User role selection saves immediately on checkbox changes. | Administrators need confidence that each toggle is persisted and errors are surfaced immediately. | Access and roles | Include immediate-save behavior and partial-update risk. |

## 6. Cross-Journey Moments That Matter

1. **Choosing the right project:** Stats and unread indicators direct attention before the user opens details.
2. **Creating from a template:** Procedure setup can either save time or propagate stale event structure.
3. **Committing an event:** The user changes workflow state and generates a text audit item.
4. **Adding an event item:** Advanced input may capture plain text, uploaded documents, images, videos, or other file-backed media.
5. **Assigning access:** Read/write roles decide whether event details appear, are actionable, or are redacted.
6. **Reviewing the timeline:** Attachments and reminders from multiple plugins become one chronological evidence stream.

## 7. Documentation Architecture Audit

Artifact: `sea-module-interface-journey-coverage.md`  
Type: Documentation module  
Responsibility: Index every Sea interface journey and map each to its dedicated journey source.

- SRP: Pass — covers only journey coverage and cross-journey inventory.
- Single state model: Pass — uses Project → Procedure → Event → Event items as the single interaction hierarchy.
- Low coupling: Pass — references interface components as evidence without defining implementation behavior.
- Abstraction: Pass — separates user journeys from component implementation details.
- Command-query: N/A — documentation artifact.
- Failure safety: Pass — explicitly marks current-state gaps and confidence.
- Open/Closed: Pass — new journeys can be added by linking another dedicated doc.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — no speculative feature plan beyond documented gaps.
- Single source of truth: Pass — establishes this file as the Sea journey coverage index.
