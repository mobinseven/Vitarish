# Owner Members and Roles Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state member access management journey
- Entry point: Owner dashboard people/access card or tenant overview members card
- Scenario: An owner or tenant admin needs to review who has access to a tenant and which tenant role each person has.
- User goal: Understand and manage tenant access safely.
- Business goal: Make access control self-service while reducing accidental privilege changes.
- Scope start: Authorized user opens the members and roles page for a tenant.
- Scope end: User has reviewed members, changed roles through approved actions, or moved to invite/remove journeys.
- Primary persona: Organization owner
- Secondary personas: Tenant admin, member with read-only visibility if allowed, platform support/admin
- Evidence sources: Owner dashboard journey and current tenant role model
- Confidence level: Medium for role model; low for member data source because member summary is not implemented.

## 2. Current Implementation Snapshot

| Area | Current behavior | Members/roles implication |
| --- | --- | --- |
| Role model | Internal roles include `TenantOwner`, `TenantAdmin`, and `TenantUser`. | UI should map them to Owner/Admin/Member labels. |
| Setup roles | Onboarding assigns the creator to owner/admin/user tenant roles. | Initial owner can be assumed to have full access after successful setup, but page still must validate. |
| Member summary | Dashboard journey identifies member summary as future data. | Do not infer or display counts until a reliable member query exists. |
| Invite/remove | Dedicated journey is still needed. | Members page can link to invite/remove actions but should not mix every workflow into one table. |
| Audit | Sensitive access changes should be audit logged. | Role changes/removals need audit integration before production release. |

## 3. Persona Snapshot

**Name / segment:** Access manager

**Situation:** Needs to see who can use a workspace and adjust permissions over time.

**Motivation:** Keep access appropriate as staff join, change responsibilities, or leave.

**Key needs:** Member list, role clarity, safe role changes, owner protection, and audit-friendly actions.

**Constraints:** Current role names are technical and the final user/member source is not defined.

**Success means:** The owner can answer “who has access and what can they do?” and start changes safely.

## 4. Journey Summary

**Narrative:** The owner opens the members and roles page, sees a role-aware table of tenant users once the data source exists, and uses explicit actions to change roles or start invite/remove flows. The page separates read-only member review from state-changing role updates and blocks dangerous changes such as removing the last owner.

**Emotional arc:** Concerned -> informed -> cautious -> confident

**Biggest current friction point:** The owner dashboard can only promise people/access placeholders because a reliable member read model is not yet connected.

**Highest-value opportunity:** Establish one access read model and simple role labels before adding complex team administration.

## 5. Journey Stages

### Stage 1: Review access

**User goal:** See who belongs to the tenant.

**User actions:** Opens page; scans member rows and role labels.

**Touchpoints / channels:** Members table/list and role summary.

**Pain points:** Missing or fake member counts would erode trust.

**Needs:** Reliable member source, loading/error/empty states, and role labels.

**Opportunities:** Start with simple table columns: person, email/user id, role, status, last changed if available.

**Success metrics:** Member list load success; time to find member; access support tickets.

### Stage 2: Understand permissions

**User goal:** Know what each role means.

**User actions:** Reads role descriptions; filters by role if needed.

**Touchpoints / channels:** Role badges, role help text, permission copy.

**Pain points:** TenantOwner/TenantAdmin/TenantUser names are internal.

**Needs:** Owner/Admin/Member labels and concise descriptions.

**Opportunities:** Centralize role-label mapping used by dashboard, switcher, and members page.

**Success metrics:** Role-help opens; incorrect role-change reversals; unauthorized attempts.

### Stage 3: Change access intentionally

**User goal:** Update a user’s role or start invite/remove.

**User actions:** Opens role action; reviews confirmation; submits approved change.

**Touchpoints / channels:** Role-change dialog, invite/remove links, audit confirmation.

**Pain points:** Accidental demotion/removal can lock out a tenant.

**Needs:** Validation, last-owner protection, permission check, and audit logging.

**Opportunities:** Keep mutations in focused commands/dialogs rather than inline table toggles.

**Success metrics:** Successful role changes; blocked unsafe changes; audit event completeness.

## 6. Happy Path

1. User opens members and roles for a tenant.
2. System validates tenant context and access.
3. System loads member list from one access read model.
4. Page displays members with user-facing role labels.
5. User chooses a member and selects **Change role**.
6. System displays allowed target roles based on current user permission and tenant rules.
7. User confirms the role change.
8. System revalidates access, prevents last-owner removal/demotion, applies the role change, and writes an audit event.
9. Page reloads the member list from the source of truth.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| Member data source unavailable | Page may show misleading access state. | Show unavailable state; do not show fake counts. |
| User is not owner/admin | Unauthorized access changes. | Show read-only view or permission state; hide mutation actions. |
| Last owner would be removed/demoted | Tenant lockout. | Block action with clear explanation. |
| Target user already changed | Concurrency mismatch. | Reload row and ask user to review current role. |
| Role assignment fails | Access state may be uncertain. | Show failure and reload source-of-truth member list. |
| Audit write fails | Sensitive change lacks traceability. | Block or roll back according to approved audit policy. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Tenant id/name | Tenant context / `TenantReadDto` | Keep tenant identity visible. |
| Member rows | Future tenant access read model | Do not infer from unrelated collections unless intentionally chosen. |
| Role labels | Central role label mapper | Owner/Admin/Member over internal role names. |
| Permission rules | Tenant role helper/application service | Revalidate server-side on mutation. |
| Audit metadata | Future audit service | Needed for role changes and removals. |

## 9. UX and Guardrail Rules

1. Never show fake member counts.
2. Keep role review read-only until the user starts a dedicated change action.
3. Use one role-label mapping across owner surfaces.
4. Protect the last owner from removal or demotion.
5. Revalidate permission at page load and before mutation.
6. Role changes must be auditable.
7. Invite and remove actions can be linked but should remain dedicated journeys.

## 10. Acceptance Criteria

- Members page validates tenant access before loading member rows.
- Member rows show user-facing role labels from a single mapping rule.
- Read-only review is separated from role-changing commands.
- Last-owner removal or demotion is blocked.
- Successful role changes reload from the source of truth and are audit-ready.
- Missing member source displays an honest unavailable state.
