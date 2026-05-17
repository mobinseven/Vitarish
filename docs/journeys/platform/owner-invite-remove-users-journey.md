# Owner Invite and Remove Users Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state tenant user invite and removal journey
- Entry point: Owner dashboard people/access card, members and roles page, or tenant overview access card
- Scenario: An owner/admin needs to add a person to a tenant or remove someone who should no longer have access.
- User goal: Safely invite the right user with the right role or remove tenant access with confidence.
- Business goal: Enable self-service access lifecycle management without accidental over-permissioning or lockout.
- Scope start: Authorized user starts invite or remove from a tenant-scoped access surface.
- Scope end: Invitation is sent/created or access removal is completed, failed safely, or cancelled.
- Primary persona: Organization owner
- Secondary personas: Tenant admin, invited user, platform support/admin
- Evidence sources: Owner dashboard journey and current tenant role model
- Confidence level: Medium for need and safety rules; low for email/invitation implementation because it is not described as connected.

## 2. Current Implementation Snapshot

| Area | Current behavior | Invite/remove implication |
| --- | --- | --- |
| Tenant roles | Internal roles are `TenantOwner`, `TenantAdmin`, and `TenantUser`. | Invite flow should choose user-facing Owner/Admin/Member labels and map server-side. |
| Onboarding assignment | Setup assigns creator to all tenant roles. | Invite/remove must handle additional users after setup, not initial bootstrap. |
| Member source | A future member/access query is needed. | Removal should start from a reliable members list. |
| Audit | Dashboard calls out audit for sensitive actions. | Invites, removals, and role changes should write audit events. |
| Billing/action inbox | Future features may generate access-related tasks. | Invitation failures or pending invites may eventually appear in the action inbox. |

## 3. Persona Snapshot

**Name / segment:** Team access owner

**Situation:** Staff changes require adding or removing tenant access.

**Motivation:** Keep the right people productive and remove access promptly when no longer needed.

**Key needs:** Email/user identification, role selection, clear pending state, confirmation, last-owner protection, and audit trail.

**Constraints:** Invitation delivery, pending invite model, and user identity lookup may not be implemented yet.

**Success means:** The owner can complete access lifecycle changes without support and without exposing a tenant to unauthorized users.

## 4. Journey Summary

**Narrative:** The owner starts from members/access, chooses invite or remove, and completes a focused flow. Invites validate recipient identity/email and selected role before sending or creating an invitation. Removals show the target member, consequences, and confirmation, then revoke access only after permission and last-owner checks pass.

**Emotional arc:** Responsible -> careful -> reassured

**Biggest current friction point:** There is no owner-facing access lifecycle flow yet; the dashboard can only show placeholders.

**Highest-value opportunity:** Build small, explicit invite and remove actions that reuse role rules and feed audit.

## 5. Invite Flow

| Step | User action | System responsibility | Guardrail |
| --- | --- | --- | --- |
| Start invite | Select **Invite user** | Validate tenant and inviter permission | Hide if role cannot invite. |
| Identify person | Enter email or select existing user | Validate format/user lookup rules | Avoid duplicate active membership. |
| Choose role | Select Owner/Admin/Member | Map to internal tenant role | Block roles inviter cannot grant. |
| Review | Read tenant, recipient, role | Summarize consequences | No mutation before submit. |
| Send/create | Submit invite | Create invite or membership and audit event | Disable duplicate submits. |

## 6. Remove Flow

| Step | User action | System responsibility | Guardrail |
| --- | --- | --- | --- |
| Start remove | Select member removal action | Validate tenant and remover permission | Hide for users without permission. |
| Review target | Confirm selected member and role | Show consequences and active tenant | Prevent wrong-member removal. |
| Confirm | Type/confirm removal if sensitive | Revalidate access and role rules | Block last-owner removal. |
| Revoke | Submit removal | Remove membership/roles and audit event | Reload member source of truth. |

## 7. Happy Path

### Invite

1. Owner opens members/access for the active tenant.
2. Owner selects **Invite user**.
3. System validates permission and tenant context.
4. Owner enters recipient email or selects an existing user.
5. Owner selects a role label.
6. System validates duplicate membership, inviter permission, role mapping, and invitation rules.
7. Owner reviews tenant, recipient, and role.
8. System creates invitation or membership and writes an audit event.
9. Page shows pending/success state and reloads members/invites.

### Remove

1. Owner selects a member from the members page.
2. Owner selects **Remove access**.
3. System validates permission and loads current member role.
4. Owner reviews member identity, tenant, and consequence copy.
5. Owner confirms removal.
6. System revalidates access, blocks last-owner removal, revokes tenant roles/membership, and writes an audit event.
7. Members page reloads from the source of truth.

## 8. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| Invalid recipient | Invite cannot be delivered or matched. | Show inline validation and keep draft. |
| Recipient already has access | Duplicate membership/confusion. | Block duplicate invite and link to existing member row. |
| Inviter cannot grant selected role | Privilege escalation. | Disable or reject role with permission copy. |
| Invite delivery fails | User assumes person has access. | Show failure/pending state and retry options. |
| Removing last owner | Tenant lockout. | Block and require another owner first. |
| Removing self | User may lose current session context. | Require extra confirmation or route to policy decision. |
| Audit write fails | Sensitive action lacks traceability. | Block or roll back per approved audit policy. |

## 9. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Tenant identity | Tenant context / `TenantReadDto` | Display throughout invite/remove. |
| Recipient identity | Future user/invitation service | Define whether invites are email-only or user-backed. |
| Existing members | Future tenant access read model | Needed for duplicate checks and removal. |
| Role mapping | Central role label mapper | Owner/Admin/Member to internal roles. |
| Audit event | Future audit service | Required for invite/remove completion. |

## 10. UX and Guardrail Rules

1. Invite and remove are dedicated actions, not inline table mutations.
2. Tenant identity must be visible in confirmation states.
3. Role choices must be limited by inviter permission.
4. Duplicate active membership must be prevented.
5. Removing access must show target identity and consequences.
6. Last-owner removal is blocked.
7. Success states must distinguish pending invitation from active membership.

## 11. Acceptance Criteria

- Invite flow validates tenant context, inviter permission, recipient, role, and duplicate membership.
- Remove flow validates tenant context, remover permission, target member, and last-owner rule.
- Both flows display tenant and target identity before mutation.
- Both flows are audit-ready.
- Failed invites/removals leave access state unchanged and reload source-of-truth data when needed.
