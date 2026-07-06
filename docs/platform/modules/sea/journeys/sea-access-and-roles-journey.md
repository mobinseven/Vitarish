# Sea Access and Roles Journey

## 1. Context

- App: Platform-wide Sea consumers
- Module: Sea
- Capability: Role and access management
- Related apps: Clinic, Gym, tenant-specific Sea hosts
- Related modules: Users
- Product / service: Vitarish Sea module
- Journey type: Current state with gaps
- Scenario: Access administrator creates roles, assigns read/write roles to users, and controls event visibility/writability.
- User goal: Ensure the right people can read or update the right event history.
- Business goal: Protect project evidence while preserving operational collaboration.
- Scope start: Administrator opens role, user-role, or event-role UI.
- Scope end: Roles or assignments are saved, cancelled, displayed, or reflected in redacted event access.
- Primary persona: Access administrator / project owner
- Evidence sources: Static code review of Sea roles, users, and event roles interface components.
- Confidence level: Medium

## 2. Journey Stages

### Stage 1: Define role vocabulary

- **User actions:** Open roles grid; add, edit, or delete roles.
- **Touchpoints / channels:** `RolesGrid`, `RoleEditDialog`.
- **User questions:** Which roles describe responsibilities in this project or tenant?
- **Emotions:** Organized when roles are meaningful; confused when role names are generic.
- **Pain points:** Roles are plain names in the inspected UI; meaning depends on administrator naming discipline.
- **Needs:** Clear role naming conventions and destructive delete consequences.
- **Opportunities:** Add role descriptions or affected-user counts if access mistakes become common.
- **Success metrics:** Role correction rate; number of unused roles.

### Stage 2: Assign user read/write capabilities

- **User actions:** Open users grid; expand a user; toggle individual read/write role checkboxes or select all; changes save immediately.
- **Touchpoints / channels:** `UsersGrid`, `UserRolesSelect`, `UserRolesDisplay`.
- **User questions:** Can this user read or write through this role? Did the toggle save?
- **Emotions:** Empowered when toggles respond; anxious if immediate saves partially fail.
- **Pain points:** Immediate save occurs on checkbox change; partial failures across multiple role updates require clear notifications.
- **Needs:** Per-toggle success/error feedback, understandable read/write distinction.
- **Opportunities:** Add pending/saved indicators and batch-save option if many toggles are changed.
- **Success metrics:** Failed role assignment notifications; time to assign a user access profile.

### Stage 3: Assign event-specific reader/writer roles

- **User actions:** Open an event; edit roles; choose reader roles and writer roles; save or cancel.
- **Touchpoints / channels:** `EventDetails`, `EventRolesEditDialog`, `EventRolesDisplay`.
- **User questions:** Which roles can view this event? Which roles can update it or add evidence?
- **Emotions:** Careful because access affects privacy and collaboration.
- **Pain points:** The event roles dialog lists roles but does not prove affected users before save.
- **Needs:** Role impact preview, clear read/write labels, safe cancellation.
- **Opportunities:** Show users/groups included in each role.
- **Success metrics:** Event access correction rate; unauthorized/redacted event encounters.

### Stage 4: Experience access as a participant

- **User actions:** Open event list; see accessible events normally; see inaccessible events as redacted rows; commit or add items only when writable.
- **Touchpoints / channels:** `EventsGrid`, `EventDetails`, `EventRolesDisplay`, `CanWriteEventAsync` behavior.
- **User questions:** Why can I not see or write this event? Who grants access?
- **Emotions:** Frustrated if no escalation path exists; reassured when redaction protects sensitive details.
- **Pain points:** Redacted rows preserve sequence but do not explain next action.
- **Needs:** Access explanation and request path.
- **Opportunities:** Add “request access” or owner contact metadata.
- **Success metrics:** Access request/support ticket count; redacted row click attempts.

## 3. Project Management Mindset Trace

| Hierarchy level | Access implication |
|---|---|
| Project | Project owner/admin defines roles and assigns users. |
| Procedure | Procedure visibility is experienced through accessible events and stats. |
| Event | Event-specific read/write roles control visibility and contribution. |
| Event items | Attachment/reminder visibility follows event access expectations, while item-specific reader lists are not surfaced in current UI evidence. |

## 4. Documentation Architecture Audit

Artifact: `sea-access-and-roles-journey.md`  
Type: Documentation journey  
Responsibility: Document role definition, user role assignment, event role assignment, and participant access outcomes.

- SRP: Pass — covers only access and role journeys.
- Single state model: Pass — role assignments are the documented access source.
- Low coupling: Pass — references Users as an external identity source without redefining it.
- Abstraction: Pass — separates access experience from repository/client implementation.
- Command-query: Pass — display is query; role create/edit/delete/assign/unassign are commands.
- Failure safety: Pass — partial immediate-save failure and redaction are documented.
- Open/Closed: Pass — future role descriptions or request access flow can extend this doc.
- Core protection: N/A — documentation only.
- KISS/YAGNI: Pass — no speculative permission framework is introduced.
- Single source of truth: Pass — access journey is scoped to this file.
