# Owner Tenant Switcher Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state tenant context selection journey
- Entry point: Owner dashboard header, home safe state, or any owner-management page that detects more than one accessible tenant
- Scenario: An owner or delegated admin needs to confirm or change the active tenant before viewing or changing tenant-scoped information.
- User goal: Switch to the correct workspace with confidence before taking action.
- Business goal: Prevent wrong-tenant administration, reduce support corrections, and make multi-tenant ownership understandable.
- Scope start: Authenticated user has access to one or more tenants and opens a tenant-aware owner surface.
- Scope end: A single active tenant context is confirmed, switched, or safely left unchanged.
- Primary persona: Organization owner with multiple tenants
- Secondary personas: Tenant admin, member with read-only tenant access, platform support/admin observing context
- Evidence sources: Owner dashboard journey map and static code review of current tenant context, tenant DTO, metadata, role helper, and home shortcut behavior
- Confidence level: Medium for need and guardrails; low for final UI because the switcher is not implemented.

## 2. Current Implementation Snapshot

| Area | Current behavior | Switcher implication |
| --- | --- | --- |
| Tenant context | The app can resolve an active tenant context for tenant-aware pages. | The switcher should reuse the existing context rather than create a second active-state model. |
| Tenant identity | `TenantReadDto` carries tenant name, app id, owner id, logo, description, and metadata. | Switcher rows can show identity from the existing tenant DTO. |
| Tenant list by owner | Owner/tenant services expose tenants by owner. | A future switcher can query owned tenants after the authenticated user-to-owner relationship is clarified. |
| Role names | Tenant roles exist as `TenantOwner`, `TenantAdmin`, and `TenantUser`. | The switcher should show user-facing labels such as Owner/Admin/Member without changing internal roles. |
| Home shortcuts | Home already reacts to the resolved tenant and `activeModules`. | Switching tenant should refresh downstream shortcuts from the newly active tenant metadata. |

## 3. Persona Snapshot

**Name / segment:** Multi-workspace owner

**Situation:** Owns or administers more than one workspace and needs to move between them during daily work.

**Motivation:** Avoid changing the wrong tenant and quickly enter the intended workspace.

**Key needs:** Visible active tenant, searchable/sortable list, role clarity, explicit confirmation, and safe handling when no tenant is available.

**Constraints:** The user may not know the internal difference between owner, tenant, app id, indicator, and role.

**Success means:** The user can identify the current workspace, switch intentionally, and see all subsequent owner actions scoped to that workspace.

## 4. Journey Summary

**Narrative:** The user sees the active tenant pinned in the owner dashboard header. If they need another workspace, they open the switcher, review accessible tenants with role labels, choose one, and confirm the context change. The app reloads tenant-scoped overview data from the selected tenant and hides state-changing actions if context resolution fails.

**Emotional arc:** Cautious -> oriented -> certain -> ready to act

**Biggest current friction point:** Multi-tenant context is not exposed as a first-class owner task, so users can lose confidence about where actions apply.

**Highest-value opportunity:** Make the active tenant impossible to miss and require an explicit switch before any sensitive owner action.

## 5. Journey Stages

### Stage 1: Recognize current tenant

**User goal:** Know which workspace is active.

**User actions:** Opens dashboard; scans header for tenant name, app id, and role.

**Touchpoints / channels:** Owner dashboard header, safe home state, tenant-aware page chrome.

**User thoughts / questions:** “Which workspace am I managing right now?”

**Pain points:** A generic dashboard can make tenant scope ambiguous.

**Needs:** Active tenant name, app id, role label, and clear no-context state.

**Opportunities:** Pin active tenant summary above all management cards.

**Success metrics:** Wrong-tenant action attempts; switcher opens; context-related support tickets.

### Stage 2: Choose another tenant

**User goal:** Find the intended tenant quickly.

**User actions:** Opens switcher; scans owned/accessed tenants; filters by name/app id if needed; selects a tenant.

**Touchpoints / channels:** Tenant switcher panel or dialog.

**User thoughts / questions:** “Is this the tenant I own/administer?”

**Pain points:** Owned tenants, delegated tenants, and member-only tenants can look equivalent.

**Needs:** Role labels, active marker, empty state, and unavailable-state explanation.

**Opportunities:** Group or badge tenants by effective role without inventing fake health data.

**Success metrics:** Time to switch; abandoned switch attempts; repeated switches within a session.

### Stage 3: Confirm context change

**User goal:** Ensure the selected tenant becomes the source of truth.

**User actions:** Confirms switch or cancels.

**Touchpoints / channels:** Confirmation CTA, route refresh, dashboard header.

**User thoughts / questions:** “Did everything update to the selected tenant?”

**Pain points:** Silent switches can leave users unsure whether state changed.

**Needs:** Clear success feedback and reload of tenant-scoped data.

**Opportunities:** Show a short confirmation message and refresh module shortcuts from `activeModules`.

**Success metrics:** Successful context switches; failed context resolution rate; post-switch dashboard load time.

## 6. Happy Path

1. User opens the owner dashboard.
2. System resolves the active tenant context.
3. Dashboard header shows active tenant name, app id, and effective role.
4. User opens the tenant switcher.
5. System loads accessible/owned tenants through a single owner/tenant read model.
6. System marks the active tenant and maps internal roles to Owner/Admin/Member labels.
7. User selects another tenant.
8. System validates the user still has access to that tenant.
9. System updates the active tenant context through the approved context mechanism.
10. Dashboard reloads tenant identity, module shortcuts, and owner placeholder cards from the new context.
11. User continues to the intended owner or module action.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| No accessible tenants | User cannot switch anywhere meaningful. | Show create-tenant guidance if allowed; otherwise show a safe empty state. |
| Tenant access was revoked | User might enter stale context. | Block switch, show access changed message, and reload available tenant list. |
| Selected tenant no longer exists | Stale tenant id could leak into actions. | Do not update context; return to safe home/dashboard state. |
| User has member-only access | User may expect owner controls. | Allow switch only for read/member surfaces and hide or disable owner/admin actions. |
| Context update fails | User may act in the previous tenant by mistake. | Keep previous context, show explicit failure, and require retry. |
| Tenant metadata malformed | Module shortcuts can fail. | Switch tenant but omit module shortcuts and show no fake data. |

## 8. Data and State Model Recommendation

Use one future tenant-switcher read model that is fed by the same tenant context and owner/tenant service sources used by the dashboard.

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Active tenant id | Existing tenant context | Do not duplicate active context in component-local state beyond display state. |
| Tenant name/app id/logo | `TenantReadDto` | Use current tenant identity fields. |
| Effective role | Request context role helpers | Map internal role names to Owner/Admin/Member labels. |
| Accessible tenants | Future user-to-owner/tenant query or existing owner tenant listing once identity mapping is settled | Needs canonical authenticated user to owner relationship. |
| Active modules | `TenantMetadata.GetActiveModules` after switch | Recalculate from selected tenant metadata. |

## 9. UX and Guardrail Rules

1. Active tenant must remain visible before state-changing owner actions.
2. Switching must validate current access before updating context.
3. The switcher must not invent health, billing, or member counts.
4. Member-only tenants must not expose owner-only cards after switch.
5. Failed switches must preserve the previous known-good context.
6. The tenant switcher is a context action, not a tenant-edit form.
7. Keep labels user-facing: Owner, Admin, Member.
8. Reuse one route/context mechanism so pages do not disagree about the active tenant.

## 10. Acceptance Criteria

- Dashboard header clearly displays the active tenant and role.
- Switcher lists accessible tenants with an active marker and role labels.
- Selecting a tenant validates access before context changes.
- Dashboard content refreshes from the selected tenant after a successful switch.
- State-changing actions are hidden or disabled when no safe tenant context exists.
- Failure states never render destructive controls for a malformed, missing, or revoked tenant.
