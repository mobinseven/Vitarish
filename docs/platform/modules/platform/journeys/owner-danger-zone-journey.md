# Owner Danger Zone Archive and Delete Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state high-risk tenant lifecycle journey
- Entry point: Tenant overview danger-zone card or tenant settings danger-zone link
- Scenario: An owner needs to archive, deactivate, restore, or request deletion of a tenant under strict safeguards.
- User goal: Understand consequences and complete only the approved lifecycle action intentionally.
- Business goal: Protect customer data and platform integrity while providing a controlled path for tenant closure.
- Scope start: Authorized owner opens the danger zone for a tenant.
- Scope end: Tenant lifecycle action is cancelled, archived/deactivated, restored, or deletion is requested/completed per policy.
- Primary persona: Organization owner with legal authority
- Secondary personas: Platform support/admin, tenant admin if allowed only for non-destructive actions
- Evidence sources: Owner dashboard journey map
- Confidence level: Low-to-medium because archive/delete semantics, recovery, and hard-delete policy are open questions.

## 2. Current Implementation Snapshot

| Area | Current behavior | Danger-zone implication |
| --- | --- | --- |
| Route and host page | Danger zone is implemented as a tenant-scoped route at `/owner/tenants/{tenantId:guid}/danger`. | The journey has a stable destination route from tenant overview. |
| Component | `OwnerDangerZoneCard` renders pending copy plus disabled **Archive Tenant** and **Delete Tenant** buttons. | Destructive actions are intentionally visible but unavailable; the journey should not describe archive/delete as implemented. |
| Dashboard reuse | The dashboard also renders the same danger-zone card. | Dashboard exposure remains safe because controls are disabled and no tenant lifecycle command is wired. |
| Lifecycle service | No owner-facing archive/delete command, recovery model, or audit integration is connected in this surface. | Archive/delete semantics, confirmation, retention, and support workflows remain future work. |
| Tenant context | The route carries `TenantId`, but the current component does not load tenant lifecycle state. | Future implementation must validate tenant id/access before enabling any destructive control. |

## 3. Persona Snapshot

**Name / segment:** Closing owner

**Situation:** Needs to stop using a workspace, remove access, or comply with data retention/deletion needs.

**Motivation:** Avoid ongoing cost/risk while handling tenant data responsibly.

**Key needs:** Consequence clarity, export/backup guidance if applicable, role validation, cooldown/recovery policy, and strong confirmation.

**Constraints:** Delete may be legally, operationally, or technically irreversible and may affect module data, billing, indicators, and users.

**Success means:** The owner either safely cancels or completes an approved lifecycle action with no ambiguity about consequences.

## 4. Journey Summary

**Narrative:** The owner opens danger zone and sees disabled or policy-gated controls until archive/delete semantics are approved. For an approved archive action, the page explains consequences, asks for confirmation, revalidates permission, writes audit, and transitions tenant status through a lifecycle service. Hard delete is either unavailable, platform-admin only, or handled as a request with review and retention policy.

**Emotional arc:** Serious -> cautious -> certain or relieved after cancelling

**Biggest current friction point:** The dashboard must show the capability exists while preventing accidental data loss before policy and implementation are ready.

**Highest-value opportunity:** Treat tenant lifecycle as a separate orchestrated use case, not a button in settings.

## 5. Lifecycle Action Matrix

| Action | Owner release stance | Required safeguards |
| --- | --- | --- |
| Archive/deactivate tenant | Candidate first owner action after policy approval | Owner permission, consequence copy, typed confirmation, audit, recovery policy. |
| Restore archived tenant | Candidate if archive is reversible | Permission, status validation, billing/indicator checks, audit. |
| Request deletion | Candidate if hard delete needs review | Owner permission, retention copy, support/admin workflow, audit. |
| Hard delete | Not owner-facing until explicitly approved | Platform-admin only, legal/retention checks, backups/export policy, audit. |

## 6. Happy Path: Archive

1. Owner opens danger zone for a tenant.
2. System validates tenant context and owner-level permission.
3. Page shows tenant identity, current status, and lifecycle action availability.
4. Owner selects **Archive tenant**.
5. System shows consequences, affected capabilities, recovery policy, and support path.
6. Owner completes required confirmation.
7. System revalidates permission and tenant status.
8. System calls a tenant lifecycle command to archive/deactivate the tenant.
9. System writes audit event and disables normal tenant-management actions.
10. User is returned to global tenant list or dashboard safe state.

## 7. Happy Path: Deletion Request

1. Owner opens danger zone and selects **Request deletion** if policy allows.
2. System validates permission and tenant status.
3. Page explains retention, irreversible consequences, billing/access impact, and review process.
4. Owner confirms the request.
5. System records a deletion request and writes audit event.
6. Platform support/admin review handles final deletion according to policy.
7. Tenant overview shows pending deletion state if the user remains authorized to see it.

## 8. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| Lifecycle policy not approved | Destructive action could be premature. | Show disabled placeholders and design-pending copy. |
| User is not owner | Unauthorized destructive action. | Hide controls or show permission state. |
| Tenant is already archived | Duplicate action/confusion. | Show archived state and allowed restore/request options only. |
| Tenant has active billing/legal holds | Data or billing inconsistency. | Block or route to billing/support resolution. |
| Confirmation mismatch | Accidental action. | Do not submit; keep confirmation state. |
| Archive command fails | Tenant state uncertain. | Show failure, reload status, and do not assume archived. |
| Audit write fails | High-risk action lacks trace. | Block or roll back per approved policy. |

## 9. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Tenant identity/status | Tenant lifecycle read model | Existing tenant DTO may need explicit lifecycle status. |
| Lifecycle command | Future tenant lifecycle application service | Orchestrates modules, indicators, roles, billing, and audit as needed. |
| Permission | Owner-level policy | Admin/member should not be enough for destructive actions unless approved. |
| Confirmation token | UI command model | Do not rely on a simple accidental click. |
| Audit event | Audit service | Required for attempts and completions. |
| Recovery/retention metadata | Future lifecycle policy | Needed before delete is enabled. |

## 10. UX and Guardrail Rules

1. Danger zone must be isolated from normal settings.
2. Controls remain disabled until archive/delete semantics are approved.
3. Prefer archive/deactivate before owner-facing hard delete.
4. Tenant identity and consequences must be visible through confirmation.
5. Require typed or equivalent high-friction confirmation for destructive actions.
6. Revalidate permission and tenant status immediately before mutation.
7. Lifecycle actions must be audit-ready and recoverable according to policy.
8. Never perform destructive action from the dashboard placeholder.

## 11. Acceptance Criteria

- Danger zone validates tenant context and owner-level permission.
- Until policy is approved, controls are disabled with clear design-pending copy.
- Archive flow shows consequences, requires confirmation, revalidates, and writes audit.
- Delete is either hidden, disabled, platform-admin only, or request-based according to policy.
- Failed lifecycle actions reload source-of-truth tenant status.
- Normal tenant actions are unavailable after successful archive unless restore policy allows them.
