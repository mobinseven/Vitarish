# Sea Projects & Procedures Journey

## 1. Context

- App: Clinic (primary consumer), any app using Sea module
- Module: Sea
- Capability: Project management, procedure tracking, event recording
- Related apps: Clinic (patient procedures)
- Related modules: Users (role assignment), Files (attachments), Communications (reminders)
- Product / service: Vitarish Sea module
- Journey type: Current state with gaps
- Scenario: Owner manages projects and procedures; user records events and attachments within a project context.
- User goal: Organize work into projects, track procedure progress, and record step-level history.
- Business goal: Preserve structured project and procedure history with clear roles and accountability.
- Scope start: User opens the Sea dashboard or a project list.
- Scope end: Procedure events, notes, and attachments are recorded; project stats reflect current state.
- Primary persona: Project owner (admin) and project participant (user)
- Evidence sources: Static code review of Sea Interface components, host pages, and module structure
- Confidence level: Medium

## 2. Persona Snapshot

**Name / segment:** Project owner / administrator

**Situation:** Managing multiple projects with procedures, events, and team assignments.

**Motivation:** Keep work organized, track progress, and ensure accountability through structured history.

**Key needs:** Project CRUD, procedure lifecycle management, role assignment, event recording, attachment management.

**Constraints:** No explicit procedure status machine; role semantics may leak across module boundaries.

**Success means:** Team can see what happened, what is in progress, and what is next for every project.

---

**Name / segment:** Project participant / user

**Situation:** Contributing to a project by recording events and uploading files.

**Motivation:** Document work accurately and keep the team informed.

**Key needs:** View assigned projects, record events, upload attachments, see timeline.

**Constraints:** Limited to own project scope; cannot manage projects or roles.

**Success means:** Contributions are recorded and visible to the team.

## 3. Journey Summary

**Narrative:** The owner creates a project, defines procedures within it, assigns team roles, and participants record events and attachments. The project dashboard shows aggregate stats. Templates allow copying procedure structures for repeated workflows.

**Emotional arc:** Start: organized (project creation) → Middle: attentive (procedure/event recording) → End: informed (dashboard overview)

**Biggest friction point:** No explicit procedure status; progress is inferred from event counts rather than workflow state.

**Highest-value opportunity:** Add procedure status machine, project archiving, and bulk operations.

## 4. Journey Stages

### Stage 1: Project Creation & Setup

**User goal:** Create a new project and configure its initial structure.

**User actions:** Open project list; create project with name, description; set project avatar.

**Touchpoints / channels:** `ProjectsGrid`, `ProjectCreateDialog`, `ProjectDetails`, `ProjectAvatar`.

**User thoughts / questions:** "How do I organize this project? What goes in it?"

**Emotions:** Purposeful, 3, because the structure is clear.

**Pain points:** No project archiving; no template-based project creation from the grid.

**Needs:** Quick project creation; optional template seeding.

**Opportunities:** Add project templates that pre-populate procedures and events.

**Success metrics:** Time to create project; project creation completion rate.

**Evidence:** Static code review assumption.

### Stage 2: Procedure Management

**User goal:** Add and manage procedures within a project.

**User actions:** Create procedure with name and description; edit procedure; delete procedure; browse procedure templates.

**Touchpoints / channels:** `ProceduresGrid`, `ProcedureCreateDialog`, `ProcedureEditDialog`, `ProcedureDetails`, `ProcedureTemplatesBrowser`.

**User thoughts / questions:** "Is this procedure on track? What steps are done?"

**Emotions:** Focused, 3, because procedure structure is visible.

**Pain points:** No procedure status (active/completed/cancelled); no provider assignment; template browsing doesn't show usage stats.

**Needs:** Explicit procedure status, provider assignment, template preview.

**Opportunities:** Add procedure workflow state machine independent from event history.

**Success metrics:** Procedure creation rate; template reuse rate.

**Evidence:** Static code review assumption.

### Stage 3: Event Recording

**User goal:** Record step-level history within a procedure.

**User actions:** Add events; commit/uncommit event steps; add timeline notes and attachments.

**Touchpoints / channels:** `EventsGrid`, `EventDetails`, `EventItemsTimeline`, `EventItemsInput`, `EventEditDialog`.

**User thoughts / questions:** "Did I record this step correctly? Is the timeline complete?"

**Emotions:** Careful, 4, because history matters.

**Pain points:** Event commit writes a text attachment as an audit note rather than a structured event state; no bulk event operations.

**Needs:** Structured event state tracking; bulk event creation.

**Opportunities:** Separate event state from attachment-based audit notes.

**Success metrics:** Events recorded per procedure; timeline completeness.

**Evidence:** Static code review assumption.

### Stage 4: Role & Access Management

**User goal:** Assign team members to project roles.

**User actions:** View user roles; edit role assignments; view role-based permissions.

**Touchpoints / channels:** `UsersGrid`, `UserRolesDisplay`, `UserRolesSelect`, `RolesGrid`, `RoleEditDialog`.

**User thoughts / questions:** "Who has access? What can each role do?"

**Emotions:** Neutral, 3, because role management is straightforward.

**Pain points:** Role semantics may leak into other modules (e.g., medical provider assignment uses Sea roles).

**Needs:** Clear role definitions; separation from cross-module concerns.

**Opportunities:** Define role types explicitly (viewer, contributor, admin) rather than generic roles.

**Success metrics:** Role assignment completion; unauthorized access attempts.

**Evidence:** Static code review assumption.

### Stage 5: Attachment Management

**User goal:** Upload and manage files within projects and procedures.

**User actions:** Upload files; view file details; attach files to events.

**Touchpoints / channels:** `AttachmentsDisplay`, `AttachmentCreator`, `AttachmentDetailsDialog`.

**User thoughts / questions:** "Is this file attached to the right procedure? Can I find it later?"

**Emotions:** Satisfied, 3, if upload is smooth.

**Pain points:** No file type restrictions visible; no preview for non-image files.

**Needs:** Clear file type guidance; file preview; attachment search.

**Success metrics:** File upload success rate; attachment retrieval rate.

**Evidence:** Static code review assumption.

### Stage 6: Project Dashboard & Stats

**User goal:** Understand project health at a glance.

**User actions:** View project stats; check unread indicators; review procedure progress.

**Touchpoints / channels:** `StatsDisplay`, `StatsDialog`, `UnreadIndicator`, `ProgressDisplay`.

**User thoughts / questions:** "What needs my attention? Which procedures are behind?"

**Emotions:** Informed, 4, if stats are clear; Anxious, 2, if stats are incomplete.

**Pain points:** Stats are aggregate counts without trend data; no overdue or at-risk indicators.

**Needs:** Trend data, overdue alerts, at-risk procedure highlighting.

**Opportunities:** Add trend charts and risk indicators to project stats.

**Success metrics:** Dashboard view frequency; time-to-first-action from dashboard.

**Evidence:** Static code review assumption.

### Stage 7: Template Management

**User goal:** Create and reuse procedure templates.

**User actions:** Create template project; define template procedures and events; apply template to new project.

**Touchpoints / channels:** `ProcedureTemplatesBrowser`, `TemplateProjectsPage`, `TemplateProjectCreatePage`.

**User thoughts / questions:** "Can I reuse this procedure structure? How do I create a template?"

**Emotions:** Efficient, 4, when templates work; Frustrated, 2, when template application is unclear.

**Pain points:** Template browsing doesn't show usage count or last-used date; template application is not previewed before commit.

**Needs:** Template usage stats; preview before applying; template versioning.

**Opportunities:** Add template marketplace or sharing across tenants.

**Success metrics:** Template creation rate; template reuse rate; time saved vs. manual setup.

**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points

1. No explicit procedure status — occurs during procedure management and project health review.
2. Role semantics leak across module boundaries — occurs during cross-module integration (Clinic/Sea).
3. Stats lack trend data and risk indicators — occurs during project dashboard review.

### Moments that matter

- **Moment of truth:** First event is recorded in a procedure — confirms the workflow is understood.
- **Moment of friction:** Explaining whether a procedure is active, done, or cancelled when there's no status.
- **Moment of delight:** Template application pre-populates a new project with the right structure.

### Design principles implied by the journey

- Separate procedure status from event history.
- Keep role definitions local to the Sea module.
- Provide actionable stats, not just counts.

## 6. Implementation Derivation

### Required artifacts

| Artifact | Why it is needed | Journey source | Priority |
|---|---|---|---|
| Procedure status machine | Makes active/completed/cancelled state explicit without relying on event counts | Stage 2 | High |
| Project archiving | Prevents completed projects from cluttering active lists | Stage 1 | Medium |
| Template usage stats | Helps users choose the right template | Stage 7 | Medium |
| Overdue procedure indicators | Surfaces at-risk work on the project dashboard | Stage 6 | High |

### Feature slices

| Slice | User value | Includes | Excludes | Acceptance signal |
|---|---|---|---|---|
| Procedure status | Users see procedure state at a glance | Status field, status transitions, status badge | Full workflow engine | Procedure selection errors decrease |
| Project archive | Completed projects don't clutter active list | Archive action, archive filter, restore | Bulk archive | Active project list stays manageable |
| Overdue indicators | At-risk procedures are visible on dashboard | Due date, overdue badge, dashboard highlight | Notification system | Overdue procedures are addressed faster |

### Domain / data implications

- Add procedure status enum (Draft, Active, Completed, Cancelled) independent from event state.
- Add project archived flag or status.
- Add procedure due date or target date for overdue calculation.

### UI implications

- Show status badge on procedure list and detail.
- Add archive/restore actions to project detail.
- Add overdue indicator to project stats and procedure list.

### API / service implications

- Add status transition validation to procedure commands.
- Add archive/restore commands to project service.
- Add due date to procedure write model.

### Validation, permissions, and safety

- Status transitions should follow a defined state machine.
- Archive should soft-delete, not hard-delete.
- Only project owners should archive projects.

### Tests and acceptance criteria

- Procedure status persists across reloads.
- Archived projects don't appear in active lists.
- Overdue indicators appear when due date is passed.

### Metrics

- Procedure selection errors.
- Time to locate active project.
- Overdue procedure resolution time.

## 7. Technical Detail Addendum

### Current implementation touchpoints

- `src/Modules/Sea/Interface/Components/ProjectsGrid.razor` — lists projects with stats and unread indicators.
- `src/Modules/Sea/Interface/Components/ProjectDetails.razor` — project detail view with edit/delete actions.
- `src/Modules/Sea/Interface/Components/ProjectEditForm.razor` — create/edit project form.
- `src/Modules/Sea/Interface/Components/ProceduresGrid.razor` — lists procedures within a project, sorted by unread/new.
- `src/Modules/Sea/Interface/Components/ProcedureCreateDialog.razor` — create procedure dialog, supports template copying.
- `src/Modules/Sea/Interface/Components/ProcedureEditDialog.razor` — edit procedure name and description.
- `src/Modules/Sea/Interface/Components/ProcedureDetails.razor` — procedure detail with edit/delete actions.
- `src/Modules/Sea/Interface/Components/EventsGrid.razor` — lists events within a procedure.
- `src/Modules/Sea/Interface/Components/EventDetails.razor` — event detail with commit/uncommit and role editing.
- `src/Modules/Sea/Interface/Components/EventItemsTimeline.razor` — renders attachment/activity timeline.
- `src/Modules/Sea/Interface/Components/EventItemsInput.razor` — input for new timeline content.
- `src/Modules/Sea/Interface/Components/UsersGrid.razor` — lists project users.
- `src/Modules/Sea/Interface/Components/UserRolesDisplay.razor` — displays user roles.
- `src/Modules/Sea/Interface/Components/RolesGrid.razor` — lists available roles.
- `src/Modules/Sea/Interface/Components/RoleEditDialog.razor` — edit role assignments.
- `src/Modules/Sea/Interface/Components/AttachmentsDisplay.razor` — lists file attachments.
- `src/Modules/Sea/Interface/Components/AttachmentCreator.razor` — file upload for attachments.
- `src/Modules/Sea/Interface/Components/StatsDisplay.razor` — project/procedure stats.
- `src/Modules/Sea/Interface/Components/UnreadIndicator.razor` — unread count badge.
- `src/Hosts/Main/Pages/Sea/Common/SeaDashboardPage.razor` — Sea dashboard landing.
- `src/Hosts/Main/Pages/Sea/Users/SeaUserProjectsPage.razor` — user's project list.
- `src/Hosts/Main/Pages/Sea/Users/SeaUserProjectPage.razor` — user's project detail.
- `src/Hosts/Main/Pages/Sea/Users/SeaUserProcedurePage.razor` — user's procedure detail.
- `src/Hosts/Main/Pages/Sea/Users/SeaUserEventPage.razor` — user's event detail.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerProjectCreatePage.razor` — owner create project.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerProjectEditPage.razor` — owner edit project.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerTemplateProjectsPage.razor` — template projects list.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerTemplateProjectCreatePage.razor` — create template project.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerTemplateProjectPage.razor` — template project detail.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerTemplateProjectEditPage.razor` — edit template project.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerTemplateProcedurePage.razor` — template procedure detail.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerTemplateEventPage.razor` — template event detail.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerSettingsUsersPage.razor` — owner user settings.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerSettingsStatisticsPage.razor` — owner statistics.
- `src/Hosts/Main/Pages/Sea/Owners/SeaOwnerRemindersPage.razor` — owner reminders.

### Issues to address

- `ProcedureReadDto`/`ProcedureWriteDto` lack a status field; status is inferred from event counts. Add explicit procedure status enum.
- `ProjectDetails.DeleteProcedure()` permanently removes procedures; add soft-delete or archive for stopped work.
- Event commit in `EventDetails` writes a text attachment as audit note rather than tracking structured event state.
- `ProceduresGrid` sorts by unread/new but cannot show status, provider, or due date because those fields don't exist.
- Role editing in `EventDetails` uses Sea role semantics that leak into cross-module contexts (e.g., Clinic provider assignment).
- Template browsing (`ProcedureTemplatesBrowser`) doesn't show usage stats or last-applied date.
- No bulk operations for events, procedures, or attachments.
- `StatsDisplay` shows aggregate counts without trend data or risk indicators.
