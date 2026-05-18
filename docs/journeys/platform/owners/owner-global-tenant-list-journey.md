# Owner Global Tenant List Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state owned/accessed tenant directory journey
- Entry point: Owner dashboard tenant list card, tenant switcher, or global owner navigation
- Scenario: An owner needs to see all tenants they own or can administer, compare basic identity, and open the right tenant overview.
- User goal: Find and open a tenant without relying on memory or browser bookmarks.
- Business goal: Make multi-tenant ownership self-service and reduce wrong-workspace support requests.
- Scope start: Authenticated user opens the global tenant list from the owner dashboard.
- Scope end: User opens a tenant overview, starts creating a tenant, or exits without changing context.
- Primary persona: Organization owner
- Secondary personas: Tenant admin, platform support/admin
- Evidence sources: Owner dashboard journey map and current owner/tenant service capabilities
- Confidence level: Medium for list need; low for exact filters because the owner-facing list is not implemented.

## 2. Current Implementation Snapshot

| Area | Current behavior | Tenant-list implication |
| --- | --- | --- |
| Tenant listing by owner | Services expose tenant listing by owner for admin/owner data. | A future owner list should wrap this behind owner-scoped authorization and not expose platform-admin pages directly. |
| Tenant DTO | Tenant records include name, app id, logo, description, owner id, metadata, and status-like entity fields. | The list can show tenant identity and active modules without fake metrics. |
| Admin owner pages | Platform admin pages can list tenants for an owner. | Owner-facing list needs safer copy, scoped permissions, and fewer platform/internal details. |
| Active modules | Active modules are stored in tenant metadata. | Tenant rows can show module badges derived from existing metadata. |
| Route ownership | Host app owns routable pages and `AppRoutes`. | The tenant list should become a host platform page when implemented. |

## 3. Persona Snapshot

**Name / segment:** Portfolio owner

**Situation:** Owns several workspaces and needs a directory-like view before choosing one.

**Motivation:** Know what tenants exist, which are usable, and where to go next.

**Key needs:** Accurate list, search, role/status clarity, module badges, and safe open actions.

**Constraints:** The user should not see platform-admin internals or tenants they no longer have access to.

**Success means:** The user can locate a tenant, understand their access level, and open its overview or switch into it.

## 4. Journey Summary

**Narrative:** The owner opens a global tenant list from the dashboard. The page loads a read-only directory of tenants the user owns or can access. The owner searches by tenant name or app id, scans role and module badges, and opens the selected tenant overview. Create-tenant is visible only if permitted and routes to the dedicated create-tenant journey.

**Emotional arc:** Scattered -> organized -> confident

**Biggest current friction point:** Current tenant visibility is fragmented between context resolution and admin pages, with no owner-facing directory.

**Highest-value opportunity:** Provide a simple, read-only list that becomes the safe launch point for tenant overview, switching, and creation.

## 5. Journey Stages

### Stage 1: Load directory

**User goal:** See every tenant they can manage or access.

**User actions:** Opens tenant list; scans total accessible tenants if a reliable count is available.

**Touchpoints / channels:** Owner tenant list page.

**Pain points:** A missing list forces users to rely on active context or support.

**Needs:** Empty state, loading state, and access-scoped query.

**Opportunities:** Use one future read model instead of stitching service calls in the page.

**Success metrics:** Directory load success; empty-state exits; support requests for “missing tenant.”

### Stage 2: Find tenant

**User goal:** Locate the intended tenant quickly.

**User actions:** Searches by name/app id; reviews role and module badges; optionally filters by role.

**Touchpoints / channels:** Search box, tenant cards/table.

**Pain points:** Internal ids or metadata can overwhelm a nontechnical owner.

**Needs:** Human-readable identity and minimal filters.

**Opportunities:** Start with simple search before adding complex admin filters.

**Success metrics:** Search-to-open completion; zero-result searches; filter usage.

### Stage 3: Open tenant

**User goal:** Enter the right tenant overview or context.

**User actions:** Selects View overview or Switch tenant.

**Touchpoints / channels:** Row/card action buttons.

**Pain points:** Opening admin actions from the wrong row can be risky.

**Needs:** Explicit active marker, selected tenant name in CTA, and permission check.

**Opportunities:** Make tenant overview the default action and context switch an explicit secondary action.

**Success metrics:** Tenant overview opens; wrong-tenant reversal rate; action errors.

## 6. Happy Path

1. User opens the global tenant list from the owner dashboard.
2. System verifies authentication and owner/admin/member access scope.
3. System loads accessible tenants through one list query.
4. Page renders tenant name, app id, logo/description when present, effective role, active marker, and active module badges.
5. User searches by tenant name or app id.
6. System filters the read-only list without changing tenant state.
7. User selects **View overview** on the intended tenant.
8. System validates access and opens that tenant’s overview.
9. User can then switch active context or choose a dedicated management journey.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| User has no tenants | List looks broken. | Show a clear empty state and create-tenant CTA only when allowed. |
| Tenant query fails | User cannot trust the list. | Show retry and do not render stale management actions. |
| User has member-only tenant access | Owner actions may be exposed incorrectly. | Show tenant row with Member role and read-appropriate action only. |
| Tenant is archived or disabled | User may attempt normal actions on inactive workspace. | Mark status clearly and limit actions to approved recovery/support paths. |
| Search has no results | User may think access is missing. | Show zero-state copy and reset-search action. |
| Access changes during list use | User may open a revoked tenant. | Revalidate on row action and reload list on failure. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Tenant identity | `TenantReadDto` | Name, app id, logo, description, owner id. |
| Effective role | Request context and future access query | Display user-facing role labels. |
| Active context marker | Existing active tenant context | Read-only marker in the list. |
| Active modules | `TenantMetadata.GetActiveModules` | Show badges only for known module names. |
| Tenant status | Existing entity status if available; future tenant lifecycle model | Do not infer billing or health status. |

## 9. UX and Guardrail Rules

1. The list is read-only; mutations route to dedicated journeys.
2. Do not expose platform-admin internals to owners.
3. Use tenant name/app id over raw ids unless a support/debug mode is approved.
4. Role labels must be legible and access-scoped.
5. Empty, error, and revoked-access states must be explicit.
6. Create-tenant is a separate journey and should not be embedded in the list form.
7. The active tenant marker must come from the same active context as the dashboard.

## 10. Acceptance Criteria

- Owner can open a directory of accessible tenants from the dashboard.
- The directory shows tenant identity, effective role, active marker, and active module badges when available.
- Search does not mutate active tenant context.
- Row actions revalidate access before navigation.
- Users with no tenants get a safe empty state.
- Owner/admin/member permissions affect available actions.
