# Owner Tenant Overview Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Current-state tenant identity/navigation overview with future summary enhancements
- Entry point: Owner dashboard tenant preview, global tenant list, or post-create navigation
- Scenario: An owner opens one tenant to review identity, active modules, access readiness, billing placeholders, audit entry points, and available management journeys.
- User goal: Understand one tenant’s administrative state and choose the correct next action.
- Business goal: Provide a safe tenant command page that links owner capabilities without mixing unrelated module dashboards.
- Scope start: User opens a specific tenant overview with an authorized tenant id or active tenant context.
- Scope end: User starts a dedicated tenant-management journey or returns to the dashboard/list.
- Primary persona: Organization owner
- Secondary personas: Tenant admin, member with limited read access, platform support/admin
- Evidence sources: Owner dashboard journey map and current tenant DTO/metadata behavior
- Confidence level: Medium-high for the current identity/navigation overview; low-to-medium for future health, module badges, member, billing, and audit summaries.

## 2. Current Implementation Snapshot

| Area | Current behavior | Tenant-overview implication |
| --- | --- | --- |
| Route and host page | Tenant overview is implemented at `/owner/tenants/{tenantId:guid}` through `OwnerTenantPage`. | This journey now describes a live owner tenant overview route. |
| Tenant loading | The page uses the shared `LoadTenantOrRedirectAsync` helper and falls back to `/owner/tenants` when the id is missing or cannot be loaded. | Missing/stale tenant states already have a safe redirect pattern. |
| Page title | When a tenant loads, the host overrides the page title with the tenant name. | The selected tenant is surfaced in page chrome, reducing wrong-tenant ambiguity. |
| Overview panel | `OwnerTenantOverviewPanel` displays tenant name, app id, optional description, and links to settings, members, billing, audit, and danger-zone journeys. | The overview is a navigation hub; it does not yet show health, member counts, billing status, or audit summaries. |
| Active modules | The overview component reads active modules from tenant metadata but does not render module badges yet. | Module visibility remains available for future UI without inventing a second source of truth. |
| Sensitive actions | Destructive work is linked to the danger-zone route and remains disabled in the current danger-zone component. | The overview should continue routing sensitive actions away from the summary panel. |

## 3. Persona Snapshot

**Name / segment:** Tenant owner

**Situation:** Wants a concise administrative snapshot for one workspace.

**Motivation:** Confirm the workspace identity and see where to manage people, settings, billing, audit, or module entry.

**Key needs:** Identity, app id, optional description, management entry points, and safe unavailable states.

**Constraints:** Detailed health, billing, members, and audit data are not all connected yet.

**Success means:** The user knows what is configured, what is pending, and where to go next without seeing fake metrics.

## 4. Journey Summary

**Narrative:** The user opens a tenant overview route and sees a read-only identity panel with tenant name, app id, optional description, and links to dedicated management journeys. The page updates the title to the tenant name and redirects safely when the tenant id is missing or stale. Health, role labels, active-module badges, member counts, billing status, and audit summaries remain future enhancements.

**Emotional arc:** Curious -> oriented -> in control

**Biggest current friction point:** The overview is now a useful navigation hub, but it does not yet expose deeper tenant health or access context.

**Highest-value opportunity:** Keep the overview read-only and add summary fields only when they come from approved tenant-scoped read models.

## 5. Journey Stages

### Stage 1: Verify tenant identity

**User goal:** Confirm the page is for the intended tenant.

**User actions:** Reads name, app id, optional description, and page title.

**Touchpoints / channels:** Tenant overview header.

**Pain points:** Wrong-tenant actions are high risk.

**Needs:** Pinned tenant identity, explicit route-scoped tenant id, and safe redirect on stale ids.

**Opportunities:** Show return-to-list and switcher actions near the header.

**Success metrics:** Wrong-tenant navigation reversals; switcher usage; overview bounce rate.

### Stage 2: Scan configuration

**User goal:** See current module and setup state.

**User actions:** Reviews available management links and notes placeholder destinations.

**Touchpoints / channels:** Module cards, setup summary, placeholders.

**Pain points:** Missing data can look like broken UI.

**Needs:** Honest placeholder labels and no fake counts/statuses.

**Opportunities:** Use tenant metadata as the single source of truth for future active-module display.

**Success metrics:** Module shortcut clicks; placeholder CTA clicks; support questions about unavailable data.

### Stage 3: Choose management journey

**User goal:** Start the right deeper action.

**User actions:** Opens settings, members/roles, billing, audit, or danger-zone journey.

**Touchpoints / channels:** Management link buttons in the overview panel.

**Pain points:** Too many actions on one page can blur read-only versus mutation.

**Needs:** Dedicated CTAs and permission copy.

**Opportunities:** Keep overview read-only and link to one-purpose journeys.

**Success metrics:** Journey starts; unauthorized action attempts; task completion downstream.

## 6. Happy Path

1. User opens tenant overview from dashboard or tenant list.
2. System resolves the route tenant id through the shared load helper.
3. System redirects to the tenant list when the id is missing or cannot be loaded.
4. System loads tenant identity and metadata from the existing tenant DTO/read model.
5. Page shows tenant name, app id, optional description, and management links for settings, members, billing, audit, and danger zone.
6. User selects a dedicated journey link.
7. The linked route or component handles any placeholder, validation, or mutation-specific behavior.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| Tenant id missing or malformed | Data could load for the wrong tenant. | Current shared helper redirects to the tenant list; do not render management links for a null tenant. |
| Tenant not found | Stale link can confuse users. | Current shared helper redirects to the tenant list. |
| Access denied | Owner-only data could leak. | Show permission state and hide tenant details beyond safe context copy. |
| Metadata malformed | Future module section may fail. | Keep the identity/navigation panel stable and show no fake module badges. |
| User is member only | Management links may imply permissions. | Future role-aware UI should show read-appropriate links and disabled owner/admin actions with copy. |
| Sensitive card clicked | User could start destructive change too easily. | Route to dedicated journey with validation and confirmation; never mutate on overview. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Tenant identity | `TenantReadDto` | Primary source for name, app id, logo, description, owner id. |
| Active modules | `TenantMetadata.GetActiveModules` | Keep module activation source consistent with home/dashboard. |
| Effective role | Request context role helpers | Map to Owner/Admin/Member. |
| Management card availability | Future owner capability registry or simple static page model | Avoid scattering permission logic across card markup. |
| Billing/audit/member/action summaries | Future dedicated queries | Placeholder only until real sources exist. |

## 9. UX and Guardrail Rules

1. Tenant overview is read-only; changes happen in dedicated journeys.
2. The tenant identity header must precede management cards.
3. Do not display fake member totals, billing status, or audit rows.
4. Module shortcuts are secondary to owner administration.
5. Permission copy must explain why actions are disabled.
6. Destructive actions appear only as disabled/design-pending placeholders until the danger-zone journey is approved.
7. Use one active-module rule across home, dashboard, and overview.

## 10. Acceptance Criteria

- Tenant overview validates tenant access before rendering details.
- Identity, app id, role, and active modules are visible when available.
- Overview cards link to dedicated journeys rather than mutating state directly.
- Missing or unimplemented summary data is omitted or appears as honest placeholders.
- Member-only users cannot access owner/admin actions.
- Malformed tenant metadata does not break the page.
