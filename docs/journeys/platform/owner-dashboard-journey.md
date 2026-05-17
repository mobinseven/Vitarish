# Owner Dashboard Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state everyday dashboard experience, grounded in current static code review
- Entry point: Authenticated user completes tenant onboarding, resolves a tenant context, and opens the platform owner dashboard from home or a tenant shortcut
- Scenario: A user who owns one or more tenants needs a calm daily command center to understand tenant health, switch tenants, create tenants, manage access, review sensitive activity, and find billing or settings tasks without jumping into module-specific dashboards first.
- User goal: Know what needs attention across owned tenants and start the right owner/admin action quickly.
- Business goal: Make self-service tenant ownership trustworthy enough that support does not need to perform routine tenant administration.
- Scope start: User has at least one tenant created by the onboarding journey and signs into Vitarish.
- Scope end: Owner either resolves their daily action from the dashboard or intentionally enters a deeper tenant-management journey.
- Primary persona: Organization owner / first administrator
- Secondary personas: Tenant admin delegated by the owner, platform support/admin observing ownership state
- Evidence sources: Static code review of owner, tenant, routing, home, setup, and authorization surfaces
- Confidence level: Medium for current-state evidence; low-to-medium for future dashboard details because most requested owner features are placeholders.

## 2. Current Implementation Snapshot

The dashboard journey is not functionally implemented yet. The important current facts are:

| Area | Current code behavior | Dashboard implication |
| --- | --- | --- |
| Owner dashboard page | `OwnerDashboardPage.razor` only renders `OwnerIndex` and has no visible owner command center. | Treat the owner dashboard as a placeholder design target, not an existing surface to refine. |
| Home continuation | Home can show module dashboard shortcuts when a tenant context exists and `activeModules` metadata maps to a known module route. | The platform owner dashboard should become the cross-tenant command center that sits above module dashboards. |
| Onboarding result | `SetupTenantAsync` creates an owner, creates a tenant, stores selected modules in metadata, creates a primary tenant indicator, and assigns the onboarding user to owner/admin/user tenant roles. | The dashboard can assume the onboarding creator starts with full tenant access, but still needs safe empty/error states if role or tenant context resolution fails. |
| Tenant APIs | `IPartiesService` exposes owner CRUD, tenant CRUD, tenant listing by owner, tenant indicators, and setup. | Early dashboard placeholders can be backed by a small owner-dashboard read model later, instead of binding every card directly to separate entity services. |
| Tenant metadata | `TenantReadDto` includes tenant identity, logo/description, app id, owner id, and metadata; `TenantMetadata` defines `activeModules`. | Dashboard overview cards can show tenant identity and module activation without inventing a second metadata source. |
| Role model | Tenant roles are currently `TenantOwner`, `TenantAdmin`, and `TenantUser`; request helpers check tenant-scoped role names. | Dashboard permissions should stay simple: owner sees everything, admin can manage operational settings, member sees read/action surfaces only where explicitly allowed. |
| Admin owner pages | Platform admin owner pages can list tenants for an owner and create a tenant with defaults. | Owner-facing tenant management should not reuse admin pages directly; it needs safer copy, explicit validation, and owner-scoped permissions. |

## 3. Persona Snapshot

**Name / segment:** Everyday Owner

**Situation:** Owns one or more tenants after onboarding, visits Vitarish to check whether anything needs attention, and occasionally performs tenant administration.

**Motivation:** Keep workspaces healthy, paid, staffed, secure, and easy to enter.

**Key needs:** Fast tenant switching, concise cross-tenant status, safe administration, clear next actions, recoverable errors, and confidence before destructive changes.

**Constraints:** The owner may understand their organization but not internal platform terms such as tenant, indicator, active modules, metadata, or fully qualified tenant roles.

**Success means:** Within one dashboard view, the owner can answer: “Which tenant am I in?”, “What needs attention?”, “Who has access?”, “Is billing OK?”, “What changed recently?”, and “Where do I go next?”

## 4. Journey Summary

**Narrative:** The owner signs in and lands on a dashboard that recognizes their active tenant, shows a switcher for all owned tenants, summarizes tenant health, and presents placeholder sections for the owner capabilities that will later become dedicated journeys. The owner scans the action inbox, confirms no urgent issues, optionally switches tenant context, and follows one clear entry point into tenant overview, members, settings, billing, audit, or danger-zone flows.

**Emotional arc:** Uncertain after login -> oriented by tenant context -> reassured by status -> in control of next action

**Biggest current friction point:** There is no owner command center; current home navigation favors module dashboards once a tenant context exists, while `OwnerDashboardPage.razor` is only a stub.

**Highest-value opportunity:** Design one owner-dashboard placeholder model that makes future owner features discoverable without prematurely implementing each feature.

## 5. Dashboard Placeholder Information Architecture

The first approved dashboard should be a design scaffold. It should communicate the intended experience while avoiding fake functionality.

| Placeholder region | Primary question answered | Content to show now | Later owning journey |
| --- | --- | --- | --- |
| Tenant context header | “Which tenant am I acting on?” | Active tenant name/app id, role badge, switcher placeholder, create-tenant placeholder CTA | Tenant switcher; Create tenant |
| Global tenant list preview | “What tenants do I own?” | Cards/table skeleton for tenant name, modules, status, and last activity; safe empty state | Global tenant list |
| Tenant overview card | “Is this tenant healthy?” | Tenant identity, active modules, primary indicator placeholder, member count placeholder, billing status placeholder | Tenant overview |
| Action inbox | “What needs my attention today?” | Placeholder count groups: invites pending, billing attention, security review, failed setup/context tasks | Basic action inbox |
| People and access | “Who can access this tenant?” | Members & roles placeholder, invite/remove user placeholder, role-model summary for Owner/Admin/Member | Members & roles; Invite/remove users; Basic roles |
| Operations | “How do I configure the tenant?” | Settings and billing entry cards marked “Coming soon” or “Design pending” | Tenant settings; Billing page |
| Trust and safety | “What changed, and what can damage the tenant?” | Audit-log placeholder and danger-zone placeholder with disabled destructive controls | Audit log; Danger zone |
| Module shortcuts | “Where do daily module workspaces live?” | Existing active module dashboard links, kept secondary to owner administration | Module-specific dashboards |

## 6. Dashboard Stage Map

### Stage 1: Arrive and establish context

**User goal:** Confirm they are in the right owner/tenant context.

**User actions:** Sign in, land on home or owner dashboard, scan current tenant name, role, and available tenants.

**Touchpoints / channels:** Home page tenant/module shortcut area; future owner dashboard header; tenant context resolver.

**User thoughts / questions:** “Am I managing the right workspace?” “Do I own more than one tenant?” “Can I switch without losing my place?”

**Emotions:** Cautious, 2/5, because wrong-tenant actions are high risk.

**Pain points:** Tenant switcher is not implemented; `NavigateToTenant` is currently a no-op in home; the owner dashboard stub gives no context.

**Needs:** One active tenant source of truth, clear role badge, graceful no-tenant and multi-tenant states.

**Opportunities:** Put the active tenant and role at the top of the dashboard before any actions; make switch/create actions visible but safely marked as placeholders until implemented.

**Success metrics:** Time to identify active tenant; switcher use rate; wrong-tenant support incidents; bounce rate from owner dashboard.

**Evidence:** Static code review.

### Stage 2: Scan the daily command center

**User goal:** Understand whether anything needs owner attention today.

**User actions:** Review status cards, action inbox, tenant overview, module shortcut availability, and any warning/empty states.

**Touchpoints / channels:** Dashboard summary cards, action inbox placeholder, active modules list, tenant overview placeholder.

**User thoughts / questions:** “Is anything broken, unpaid, insecure, or waiting for me?” “Can my team keep working?”

**Emotions:** Reassured, 4/5, if the dashboard is quiet and clear; anxious if unknown placeholders look like errors.

**Pain points:** Billing, action inbox, audit, member counts, and tenant health are not implemented; empty placeholder copy could be mistaken for missing data.

**Needs:** Honest labels such as “Coming soon,” “No data connected yet,” or “Design pending”; no fake counts; clear distinction between implemented links and disabled future controls.

**Opportunities:** Use neutral placeholder components that show planned capability, owner value, and expected next release decision without implying live data.

**Success metrics:** Action inbox click-through; dashboard task completion; confusion reports; support contacts about unavailable owner features.

**Evidence:** Static code review and requested feature list.

### Stage 3: Choose an owner action

**User goal:** Move from summary into the correct management task.

**User actions:** Click tenant overview, members, invite users, settings, billing, audit log, danger zone, create tenant, or module dashboard shortcut.

**Touchpoints / channels:** Dashboard cards, CTAs, disabled placeholders, route constants, future owner-management pages.

**User thoughts / questions:** “Where do I manage this?” “Is this safe?” “Do I have permission?”

**Emotions:** Focused, 3/5, because owner actions can affect access, money, or data.

**Pain points:** Most target actions have no owner-facing route or implementation; platform admin pages exist but are not suitable as owner-facing UX.

**Needs:** One primary CTA per card, stable future route plan, permission-aware disabled states, safe fallback to home when tenant context is missing.

**Opportunities:** Define dashboard cards as navigation placeholders first, then implement each card as its own journey without rewriting the dashboard layout.

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
| Tenant switcher | Select another owned tenant from the header. | Switching context safely, preserving return path, and making active tenant unmistakable. | Not implemented. |
| Create tenant | Start a new tenant setup from the dashboard. | Reusing onboarding concepts for an existing owner, module selection, indicators, and role assignment. | Partially covered by onboarding; owner-created additional tenants not implemented. |
| Global tenant list | Show all owned tenants with status and quick filters. | Cross-tenant list, search/filter, ownership boundaries, empty states. | Admin-only owner tenant list exists; owner-facing list not implemented. |
| Tenant overview | Summarize one tenant’s identity, modules, members, billing, and activity. | One read model for tenant health, active modules, primary indicator, and recent changes. | Not implemented. |
| Members & roles | Preview member count and role distribution. | Member table, role assignment, permission guardrails. | Basic roles exist; owner UI not implemented. |
| Invite/remove users | Show pending invites and removal entry point. | Invitation flow, revocation/removal, failure recovery, email/status handling. | Not implemented. |
| Basic Owner/Admin/Member roles | Explain current role model simply. | Role definitions, upgrade/downgrade rules, self-demotion prevention. | TenantOwner/TenantAdmin/TenantUser exist in code; UX naming needs Owner/Admin/Member. |
| Tenant settings | Link to tenant identity/configuration. | Tenant name, logo, description, indicators, active modules, safe validation. | Not implemented owner-facing; tenant metadata update exists through `TenantPageBase`. |
| Billing page | Show billing status placeholder. | Plan, payment method, invoices, usage, failed payment recovery. | Not implemented. |
| Basic action inbox | Surface urgent owner tasks. | Prioritization, dismissal, ownership, and read/state separation. | Not implemented. |
| Audit log for sensitive actions | Show recent sensitive events. | Immutable events for access, billing, settings, and danger-zone actions. | Audit infrastructure exists in some modules; owner audit UI not implemented. |
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

1. Should the owner dashboard have a platform route such as `/owner`, `/owners/dashboard`, or should it be tenant-scoped under a tenant route? Current `OwnerDashboardPage.razor` has no route.
2. After onboarding creates a tenant, should the success redirect go directly to the owner dashboard instead of home once tenant context switching is available?
3. What is the canonical relationship between authenticated users and owners when a user can own multiple owners or tenants? Current onboarding relies on creation/audit context, while tenant records carry `OwnerId`.
4. Should user-facing roles be named **Owner/Admin/Member** while internal roles remain `TenantOwner`, `TenantAdmin`, and `TenantUser`?
5. Should **delete** be allowed for owners, or should the first release only support **archive** with platform-admin hard delete?
6. What billing system, plan model, and invoice source should the billing placeholder eventually connect to?
7. Which sensitive actions must appear in audit first: invites/removals, role changes, tenant settings, billing changes, tenant archive/delete, or all of them?

## 13. Acceptance Criteria for the Placeholder Dashboard Design

- Dashboard clearly communicates it is an owner command center, not a module dashboard.
- Active tenant and effective role are visible before any management card.
- Implemented links are visually distinct from disabled placeholders.
- Every requested owner capability has a visible placeholder or planned entry point.
- The dashboard does not invent unavailable data.
- Tenant identity and active modules use existing tenant DTO/metadata rules.
- State-changing owner actions are not implemented directly in the placeholder dashboard.
- Danger-zone controls are present only as disabled/design-pending placeholders.
- Each capability in the requested list has a named dedicated journey map still to be created before implementation.

## 14. Future Journey Map Creation Order

Recommended order, because each step unlocks safer dashboard functionality:

1. Tenant switcher journey.
2. Global tenant list journey.
3. Tenant overview journey.
4. Create tenant journey for existing owners.
5. Members & roles journey.
6. Invite/remove users journey.
7. Tenant settings journey.
8. Audit log journey.
9. Basic action inbox journey.
10. Billing page journey.
11. Danger zone archive/delete journey.
