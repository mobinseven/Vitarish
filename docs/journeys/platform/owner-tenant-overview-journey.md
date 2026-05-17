# Owner Tenant Overview Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state tenant overview journey
- Entry point: Owner dashboard, global tenant list, or post-switch confirmation
- Scenario: An owner opens one tenant to review identity, active modules, access readiness, billing placeholders, audit entry points, and available management journeys.
- User goal: Understand one tenant’s administrative state and choose the correct next action.
- Business goal: Provide a safe tenant command page that links owner capabilities without mixing unrelated module dashboards.
- Scope start: User opens a specific tenant overview with an authorized tenant id or active tenant context.
- Scope end: User starts a dedicated tenant-management journey or returns to the dashboard/list.
- Primary persona: Organization owner
- Secondary personas: Tenant admin, member with limited read access, platform support/admin
- Evidence sources: Owner dashboard journey map and current tenant DTO/metadata behavior
- Confidence level: Medium for information architecture; low for final UI because the overview is not implemented.

## 2. Current Implementation Snapshot

| Area | Current behavior | Overview implication |
| --- | --- | --- |
| Tenant identity | Tenant DTO includes name, app id, owner id, description/logo, and metadata. | The overview can show identity from existing data. |
| Active modules | `activeModules` metadata is already used for home dashboard shortcuts. | Module section should use the same metadata rule. |
| Owner dashboard | Current owner page is a stub and dashboard is a future design target. | Tenant overview should be a deeper page linked from dashboard cards. |
| Admin tenant actions | Admin pages can create/list tenants for owners. | Owner overview should not reuse admin pages directly for mutations. |
| Permissions | Tenant role helpers exist. | Read/mutate cards must be gated by Owner/Admin/Member labels. |

## 3. Persona Snapshot

**Name / segment:** Tenant owner

**Situation:** Wants a concise administrative snapshot for one workspace.

**Motivation:** Confirm the workspace identity and see where to manage people, settings, billing, audit, or module entry.

**Key needs:** Identity, role, active modules, management entry points, and safe unavailable states.

**Constraints:** Detailed health, billing, members, and audit data are not all connected yet.

**Success means:** The user knows what is configured, what is pending, and where to go next without seeing fake metrics.

## 4. Journey Summary

**Narrative:** The user opens a tenant overview and sees a read-only identity header, active module badges, role-aware management cards, and clear links to dedicated journeys. Cards with unavailable data display honest placeholder copy. The page does not perform destructive changes directly; it routes state-changing work to validated flows.

**Emotional arc:** Curious -> oriented -> in control

**Biggest current friction point:** Tenant-level administration is spread across onboarding, home shortcuts, and admin-only pages.

**Highest-value opportunity:** Create one tenant-specific command page that remains read-only and delegates mutations to focused journeys.

## 5. Journey Stages

### Stage 1: Verify tenant identity

**User goal:** Confirm the page is for the intended tenant.

**User actions:** Reads name, app id, role, description/logo, and active context marker.

**Touchpoints / channels:** Tenant overview header.

**Pain points:** Wrong-tenant actions are high risk.

**Needs:** Pinned tenant identity and explicit active/specific tenant indicator.

**Opportunities:** Show return-to-list and switcher actions near the header.

**Success metrics:** Wrong-tenant navigation reversals; switcher usage; overview bounce rate.

### Stage 2: Scan configuration

**User goal:** See current module and setup state.

**User actions:** Reviews active module badges and overview cards.

**Touchpoints / channels:** Module cards, setup summary, placeholders.

**Pain points:** Missing data can look like broken UI.

**Needs:** Honest placeholder labels and no fake counts/statuses.

**Opportunities:** Use tenant metadata as the single source of truth for active modules.

**Success metrics:** Module shortcut clicks; placeholder CTA clicks; support questions about unavailable data.

### Stage 3: Choose management journey

**User goal:** Start the right deeper action.

**User actions:** Opens members/roles, settings, billing, audit, action inbox, or danger-zone journey.

**Touchpoints / channels:** Role-aware management cards.

**Pain points:** Too many actions on one page can blur read-only versus mutation.

**Needs:** Dedicated CTAs and permission copy.

**Opportunities:** Keep overview read-only and link to one-purpose journeys.

**Success metrics:** Journey starts; unauthorized action attempts; task completion downstream.

## 6. Happy Path

1. User opens tenant overview from dashboard or tenant list.
2. System resolves tenant id and validates access.
3. System loads tenant identity and metadata from the existing tenant DTO/read model.
4. Page shows tenant name, app id, role label, description/logo if available, and active context state.
5. Page displays active module shortcuts from `activeModules` metadata.
6. Page displays management cards for members/roles, invites/removals, settings, billing, audit, action inbox, and danger zone.
7. Cards with unavailable data show explicit placeholder copy.
8. User selects a dedicated journey CTA.
9. System validates role permissions again before opening the journey.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| Tenant id missing or malformed | Data could load for the wrong tenant. | Return to safe dashboard/home state; do not render management cards. |
| Tenant not found | Stale link can confuse users. | Show not-found state and return-to-list action. |
| Access denied | Owner-only data could leak. | Show permission state and hide tenant details beyond safe context copy. |
| Metadata malformed | Module section may fail. | Show no active modules and keep the rest of the page stable. |
| User is member only | Management cards may imply permissions. | Show read-appropriate cards and disabled owner/admin actions with copy. |
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
- Missing/unimplemented data appears as honest placeholders.
- Member-only users cannot access owner/admin actions.
- Malformed tenant metadata does not break the page.
