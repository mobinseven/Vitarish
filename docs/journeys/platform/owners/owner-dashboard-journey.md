# Owner Dashboard Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Current-state owner command center with future-state capability placeholders
- Entry point: Authenticated user opens `/owner/dashboard` from the owner header, global navigation, or a tenant-management link
- Scenario: A user who owns one or more tenants needs a calm daily command center to understand tenant health, switch tenants, create tenants, manage access, review sensitive activity, and find billing or settings tasks without jumping into module-specific dashboards first.
- User goal: Know what needs attention across owned tenants and start the right owner/admin action quickly.
- Business goal: Make self-service tenant ownership trustworthy enough that support does not need to perform routine tenant administration.
- Scope start: User has at least one tenant created by the onboarding journey and signs into Vitarish.
- Scope end: Owner either resolves their daily action from the dashboard or intentionally enters a deeper tenant-management journey.
- Primary persona: Organization owner / first administrator
- Secondary personas: Tenant admin delegated by the owner, platform support/admin observing ownership state
- Evidence sources: Static code review of owner routes, host pages, Parties.Interface owner components, tenant metadata, setup, and authorization surfaces
- Confidence level: Medium for current-state evidence; low-to-medium for future dashboard details because most requested owner features are placeholders.

## 2. Current Implementation Snapshot

The owner dashboard has moved from a stub to a simple host-composed command center. It is intentionally lightweight: implemented tenant discovery and navigation are visible, while data sources that do not exist yet remain honest placeholders.

| Area | Current behavior | Dashboard implication |
| --- | --- | --- |
| Route and host ownership | The dashboard is routable at `/owner/dashboard`; owner routes live in `AppRoutes.Owners`, and the legacy `/owner` constant is documented as a redirect target. | Journey maps should reference `/owner/dashboard` and tenant-scoped `/owner/tenants/{tenantId:guid}` routes instead of an unspecified owner route. |
| Owner dashboard page | `OwnerDashboardPage` resolves the owner id from request context, loads tenants with `GetTenantsByOwnerAsync`, renders up to three `OwnerTenantCard` entries, links to the full tenant list when there are more than three tenants, and shows **Create Workspace** when the initialized list is empty. | The dashboard journey is now a real first-stop owner surface for tenant discovery and creation, not only a future design scaffold. |
| Dashboard capability cards | Action inbox, billing, audit log, and danger zone render reusable Parties.Interface owner components with pending/coming-soon copy; danger-zone archive/delete buttons are disabled. | Keep these cards in the journey as discoverable placeholders and do not describe them as live data, counts, or mutations. |
| Owner interface layer | Reusable owner components now live under `Identity/Parties/Interface/Components/Owners`; host pages only compose those components and provide route URLs/callbacks. | Journey maps should keep route decisions in the host and component behavior in the Parties interface layer. |
| Shared owner page base | Owner pages inherit `OwnerPageBase`, which injects `IPartiesService`/`ISnackbar`, parses `RequestContext.OwnerId`, and provides `LoadTenantOrRedirectAsync`. | Tenant loading/failure behavior should be described as a shared owner-page pattern, not duplicated per page. |
| Tenant context header | The app shell renders `TenantContextHeader`, which loads the current owner and links to home and the owner dashboard. | The current navigation aid is an owner-context header, not a full tenant switcher with role labels. |
| Tenant settings | Tenant settings can edit safe display fields and save active modules through tenant metadata. | Dashboard settings links point to a partially implemented journey rather than a pure placeholder. |

## 3. Persona Snapshot

**Name / segment:** Everyday Owner

**Situation:** Owns one or more tenants after onboarding, visits Vitarish to check whether anything needs attention, and occasionally performs tenant administration.

**Motivation:** Keep workspaces healthy, paid, staffed, secure, and easy to enter.

**Key needs:** Fast tenant switching, concise cross-tenant status, safe administration, clear next actions, recoverable errors, and confidence before destructive changes.

**Constraints:** The owner may understand their organization but not internal platform terms such as tenant, indicator, active modules, metadata, or fully qualified tenant roles.

**Success means:** Within one dashboard view, the owner can answer: “Which tenant am I in?”, “What needs attention?”, “Who has access?”, “Is billing OK?”, “What changed recently?”, and “Where do I go next?”

## 4. Journey Summary

**Narrative:** The owner opens `/owner/dashboard` and sees a lightweight command center. The page lists up to three owned tenants, links to the full tenant list when needed, offers **Create Workspace** when the owner has no tenants, and shows honest placeholder cards for action inbox, billing, audit, and danger-zone work. The owner opens a tenant overview from the list, then follows tenant-scoped links into settings, members, billing, audit, or danger-zone routes.

**Emotional arc:** Uncertain after login -> oriented by owner navigation -> focused on a tenant -> clear about what is implemented versus pending

**Biggest current friction point:** The dashboard now exists, but several high-value owner capabilities are intentionally placeholder-only and the current header is owner-oriented rather than a full tenant switcher.

**Highest-value opportunity:** Keep the current simple command center and progressively replace placeholders with tenant-scoped read models and dedicated journeys without changing route ownership or inventing fake data.

## 5. Current Dashboard Information Architecture

The current dashboard is a working scaffold. It should communicate what is available now while avoiding fake functionality.

| Region | Primary question answered | Content shown now | Related journey |
| --- | --- | --- | --- |
| Owner context header | “Where can I return for owner work?” | Owner name when resolved, plus links to home and owner dashboard. | Tenant switcher |
| Tenant preview | “What tenants do I own?” | Up to three `OwnerTenantCard` entries with tenant name/app id; **View All** when more than three tenants exist. | Global tenant list; Tenant overview |
| Empty owner state | “How do I start?” | **Create Workspace** link after the owner-scoped tenant list initializes empty. | Create tenant |
| Action inbox | “What needs my attention?” | `ActionInboxPending` placeholder only. | Basic action inbox |
| Billing | “Where will billing live?” | Coming-soon billing card only. | Billing page |
| Audit log | “Where will sensitive history live?” | `AuditLogPending` placeholder only. | Audit log |
| Danger zone | “Where will destructive actions live?” | Pending copy with disabled archive/delete buttons. | Danger zone |
| Tenant management routes | “Where do I manage this tenant?” | Tenant overview links to settings, members, billing, audit, and danger-zone routes. | Tenant overview; Settings; Members; Billing; Audit; Danger zone |

## 6. Dashboard Stage Map

### Stage 1: Arrive and establish context

**User goal:** Confirm they are in the right owner/tenant context.

**User actions:** Sign in, land on home or owner dashboard, scan current tenant name, role, and available tenants.

**Touchpoints / channels:** Owner dashboard, owner context header, tenant list preview, tenant overview routes, and home/module shortcuts.

**User thoughts / questions:** “Am I managing the right workspace?” “Do I own more than one tenant?” “Can I switch without losing my place?”

**Emotions:** Cautious, 2/5, because wrong-tenant actions are high risk.

**Pain points:** Tenant switcher is not implemented; dashboard tenant selection is route-scoped rather than a global active-context switch; role labels are not yet shown.

**Needs:** One active tenant source of truth, clear role badge, graceful no-tenant and multi-tenant states.

**Opportunities:** Keep the owner header visible, make tenant route selection explicit, and add role labels before enabling sensitive tenant switching.

**Success metrics:** Time to identify active tenant; switcher use rate; wrong-tenant support incidents; bounce rate from owner dashboard.

**Evidence:** Static code review.

### Stage 2: Scan the daily command center

**User goal:** Understand whether anything needs owner attention today.

**User actions:** Review status cards, action inbox, tenant overview, module shortcut availability, and any warning/empty states.

**Touchpoints / channels:** Tenant preview cards, action inbox placeholder, billing placeholder, audit placeholder, danger-zone placeholder, and tenant overview links.

**User thoughts / questions:** “Is anything broken, unpaid, insecure, or waiting for me?” “Can my team keep working?”

**Emotions:** Reassured, 4/5, if the dashboard is quiet and clear; anxious if unknown placeholders look like errors.

**Pain points:** Billing, action inbox, audit rows, member counts, and tenant health summaries are not connected; placeholder copy must remain explicit.

**Needs:** Honest labels such as “Coming soon,” “No data connected yet,” or “Design pending”; no fake counts; clear distinction between implemented links and disabled future controls.

**Opportunities:** Use neutral placeholder components that show planned capability, owner value, and expected next release decision without implying live data.

**Success metrics:** Action inbox click-through; dashboard task completion; confusion reports; support contacts about unavailable owner features.

**Evidence:** Static code review and requested feature list.

### Stage 3: Choose an owner action

**User goal:** Move from summary into the correct management task.

**User actions:** Click tenant overview, members, invite users, settings, billing, audit log, danger zone, create tenant, or module dashboard shortcut.

**Touchpoints / channels:** Dashboard cards, tenant overview links, route constants, current owner-management pages, and disabled placeholders.

**User thoughts / questions:** “Where do I manage this?” “Is this safe?” “Do I have permission?”

**Emotions:** Focused, 3/5, because owner actions can affect access, money, or data.

**Pain points:** Most target actions now have owner-facing routes, but several routes still render placeholder components rather than connected read models or commands.

**Needs:** One primary CTA per card, stable future route plan, permission-aware disabled states, safe fallback to home when tenant context is missing.

**Opportunities:** Keep host route composition stable and replace placeholder cards with focused Parties.Interface components as each journey becomes real.

**Success metrics:** Card CTR by task, failed authorization rate, route-not-found incidents, completion rate after leaving dashboard.

**Evidence:** Static code review.

### Stage 4: Complete or defer the task

**User goal:** Finish the chosen action or understand why it is not available yet.

**User actions:** Complete the linked journey when implemented; for placeholders, read what is coming and return to dashboard or continue to a module dashboard.

**Touchpoints / channels:** Dedicated future journey pages, snackbar/error messaging, dashboard return links, audit trail surfaces.

**User thoughts / questions:** “Did the change work?” “Can I undo it?” “Was it recorded?”

**Emotions:** Confident, 4/5, if state-changing actions are confirmed and auditable.

**Pain points:** Sensitive operations such as user removal, role changes, billing, and delete/archive controls need stronger validation and audit than simple CRUD buttons.

**Needs:** Separate read-only overview queries from state-changing commands, confirmation patterns for sensitive actions, audit entries for destructive/security-sensitive changes.

**Opportunities:** Use the dashboard as the common return point after every owner-management journey; surface the latest sensitive changes in the trust/safety region.

**Success metrics:** Successful action completion; undo/support requests; audit-log coverage; destructive-action cancellation rate.

**Evidence:** Static code review and architecture rules.

## 7. Required Owner Capability Journey Backlog

Each owner capability needs its own dedicated journey map before implementation. The dashboard should link to or reserve space for these maps without implementing their details prematurely.

| Capability | Dashboard placeholder promise | Dedicated journey map focus | Implementation status |
| --- | --- | --- | --- |
| Tenant switcher | Confirm or change active tenant context. | Switching context safely, preserving return path, and making active tenant unmistakable. | Not implemented; owner header and route-scoped tenant opening exist. |
| Create tenant | Start a new tenant setup from the dashboard/list. | Current direct form plus future module selection, duplicate checks, and recovery. | Implemented as a simple tenant + primary indicator form. |
| Global tenant list | Show all owned tenants. | Cross-tenant list, future search/filter, ownership boundaries, empty states. | Implemented as a simple owner-scoped list without search/filter. |
| Tenant overview | Summarize one tenant and route to management tasks. | Current identity/navigation panel plus future health, modules, primary indicator, and recent changes. | Implemented as identity + management links. |
| Members & roles | Preview member count and role distribution. | Member table, role assignment, permission guardrails. | Placeholder route implemented; data/model not connected. |
| Invite/remove users | Show pending invites and removal entry point. | Invitation flow, revocation/removal, failure recovery, email/status handling. | Invite placeholder route implemented; remove not implemented. |
| Basic Owner/Admin/Member roles | Explain current role model simply. | Role definitions, upgrade/downgrade rules, self-demotion prevention. | TenantOwner/TenantAdmin/TenantUser exist in code; UX naming needs Owner/Admin/Member. |
| Tenant settings | Link to tenant identity/configuration. | Tenant name, logo, description, active modules, and future indicators. | Implemented for display fields and active modules; indicators/audit/conflict handling pending. |
| Billing page | Show billing status placeholder. | Plan, payment method, invoices, usage, failed payment recovery. | Placeholder route/card implemented. |
| Basic action inbox | Surface urgent owner tasks. | Prioritization, dismissal, ownership, and read/state separation. | Dashboard placeholder card implemented. |
| Audit log for sensitive actions | Show recent sensitive events. | Immutable events for access, billing, settings, and danger-zone actions. | Placeholder route/card implemented; event source not connected. |
| Danger zone with delete/archive controls | Show disabled destructive controls until journey is approved. | Archive vs delete, confirmations, dependencies, retention, audit, recovery. | Admin delete exists; owner-safe archive/delete not implemented. |

## 8. Happy Path: Everyday Dashboard Visit

1. Owner signs in.
2. System resolves authentication and tenant context.
3. If no tenant context is available, system shows a safe home/dashboard state with create/switch guidance instead of owner-management actions.
4. If tenant context exists, dashboard header shows active tenant name, app id, and the user’s effective tenant role.
5. Dashboard shows the tenant switcher placeholder and create-tenant placeholder.
6. Dashboard lists owned-tenant preview cards or an honest “tenant list coming soon” placeholder.
7. Dashboard loads tenant overview information that is already available: tenant name, app id, description/logo when present, and active modules.
8. Dashboard shows module shortcuts from active modules as secondary navigation.
9. Dashboard shows action inbox, people/access, billing, settings, audit, and danger-zone cards with clear unavailable states where data is not connected.
10. Owner clicks an implemented module shortcut or returns later; if they click a placeholder, the UI explains that the journey is pending approval and offers a return to dashboard.

## 9. Alternate and Failure Paths

| Situation | User risk | Dashboard behavior |
| --- | --- | --- |
| No tenant context resolved | Owner cannot tell where actions would apply. | Hide tenant-specific state-changing actions; show create/switch guidance and a retry path. |
| Tenant id is malformed or tenant no longer exists | Wrong or stale context could leak into owner actions. | Return to home or safe error state; do not render destructive controls. |
| User has tenant user/member access only | User may see owner-only controls they cannot use. | Show read/member-appropriate module shortcuts and hide or disable owner/admin cards with permission copy. |
| Owner has multiple tenants | Wrong-tenant actions become likely. | Keep active tenant name pinned in the header and require explicit tenant switch before actions. |
| `activeModules` metadata is missing or malformed | Module shortcuts may be empty. | Show no module shortcuts and avoid failing the dashboard. |
| Owner feature is not implemented | Placeholder may look broken. | Use explicit placeholder labels, disabled CTAs, and “journey pending” helper text. |
| Sensitive action attempted | Data/access loss risk. | Route only to approved journey; require confirmation, validation, permission check, and audit logging before mutation. |

## 10. Placeholder UX Rules

1. **Be honest about readiness:** Do not show fake counts, fake billing status, fake audit rows, or fake member totals.
2. **Keep owner context visible:** Active tenant and role must remain visible above all dashboard actions.
3. **Separate overview from mutation:** Summary cards read data; state-changing actions live behind explicit CTAs and future dedicated journeys.
4. **Use one source of truth:** Tenant identity and modules come from existing tenant DTO/metadata until a formal owner-dashboard read model is introduced.
5. **Make permissions legible:** Owner/Admin/Member labels should be user-facing names over the existing TenantOwner/TenantAdmin/TenantUser role model.
6. **Prefer disabled placeholders to dead links:** If a destination is not implemented, show why and what approval/design is needed next.
7. **Protect destructive actions:** Danger-zone controls start disabled and are not implemented until archive/delete semantics, audit, and recovery are designed.
8. **Keep module dashboards secondary:** Module shortcuts help daily work, but the owner dashboard is for tenant ownership and administration.
9. **Design for extensibility:** New owner capabilities should add cards/sections without rewriting the dashboard shell.
10. **Keep it simple:** The first dashboard should be a small set of cards and clear states, not a complex admin console.

## 11. Data and State Model Recommendation

Create one future read model for the dashboard instead of letting the Razor page query every service independently.

| Read model field | Source today | Notes |
| --- | --- | --- |
| Active tenant id/name/app id/logo/description | `TenantReadDto` | Already available after tenant context resolution. |
| Owner id/name | `OwnerReadDto` via tenant owner id | Needs safe owner lookup and handling when owner cannot be loaded. |
| Active modules | `TenantMetadata.GetActiveModules` | Already used by home; keep as the single active-module metadata rule. |
| Effective role | Request context role helpers | Map internal TenantOwner/TenantAdmin/TenantUser to Owner/Admin/Member labels. |
| Owned tenants | `GetTenantsByOwnerAsync` | Requires reliable way to identify the current owner for the signed-in user, especially for multiple owners/tenants. |
| Member summary | Future users/roles query | Do not infer from `TenantService.Users` unless the dashboard deliberately uses the same tenant-user role source. |
| Billing status | Future billing application contract | Placeholder only. |
| Action inbox | Future owner action service | Placeholder only; should be read-only until individual action handlers exist. |
| Audit summary | Future audit query | Placeholder only; sensitive mutations should write audit records before this becomes useful. |

## 12. Open Clarifying Questions Before UI Implementation

1. Should the legacy `/owner` entry point redirect to `/owner/dashboard`, and should any additional aliases be supported?
2. After onboarding creates a tenant, should the success redirect go directly to the owner dashboard instead of home once tenant context switching is available?
3. What is the canonical relationship between authenticated users and owners when a user can own multiple owners or tenants? Current onboarding relies on creation/audit context, while tenant records carry `OwnerId`.
4. Should user-facing roles be named **Owner/Admin/Member** while internal roles remain `TenantOwner`, `TenantAdmin`, and `TenantUser`?
5. Should **delete** be allowed for owners, or should the first release only support **archive** with platform-admin hard delete?
6. What billing system, plan model, and invoice source should the billing placeholder eventually connect to?
7. Which sensitive actions must appear in audit first: invites/removals, role changes, tenant settings, billing changes, tenant archive/delete, or all of them?

## 13. Acceptance Criteria for the Placeholder Dashboard Design

- Dashboard clearly communicates it is an owner command center, not a module dashboard.
- Owner context is visible before management cards; tenant role labels remain a follow-up for switcher/members work.
- Implemented links such as tenant overview, full tenant list, create workspace, and settings are visually distinct from disabled or pending placeholders.
- Every requested owner capability has a visible placeholder or planned entry point.
- The dashboard does not invent unavailable data.
- Tenant identity and active modules use existing tenant DTO/metadata rules where currently rendered or saved.
- State-changing owner actions are not implemented directly in the placeholder dashboard.
- Danger-zone controls are present only as disabled/design-pending placeholders.
- Each capability in the requested list has a named dedicated journey map that reflects current route/component status before further code work begins.

## 14. Future Journey Map Creation Order

Recommended order, because each step unlocks safer dashboard functionality:

1. [Tenant switcher journey](owner-tenant-switcher-journey.md).
2. [Global tenant list journey](owner-global-tenant-list-journey.md).
3. [Tenant overview journey](owner-tenant-overview-journey.md).
4. [Create tenant journey for existing owners](owner-create-tenant-journey.md).
5. [Members & roles journey](owner-members-roles-journey.md).
6. [Invite/remove users journey](owner-invite-remove-users-journey.md).
7. [Tenant settings journey](owner-tenant-settings-journey.md).
8. [Audit log journey](owner-audit-log-journey.md).
9. [Basic action inbox journey](owner-action-inbox-journey.md).
10. [Billing page journey](owner-billing-page-journey.md).
11. [Danger zone archive/delete journey](owner-danger-zone-journey.md).
