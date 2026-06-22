# Owner Global Tenant List Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Current-state owner-scoped tenant directory with future search/filter enhancements
- Entry point: Owner dashboard tenant list card, tenant switcher, or global owner navigation
- Scenario: An owner needs to see the tenants currently associated with their owner id and open the right tenant overview.
- User goal: Find and open a tenant without relying on memory or browser bookmarks.
- Business goal: Make multi-tenant ownership self-service and reduce wrong-workspace support requests.
- Scope start: Authenticated user opens the global tenant list from the owner dashboard.
- Scope end: User opens a tenant overview, starts creating a tenant, or exits without changing context.
- Primary persona: Organization owner
- Secondary personas: Tenant admin, platform support/admin
- Evidence sources: Owner dashboard journey map and current owner/tenant service capabilities
- Confidence level: Medium-high for the basic list route; low-to-medium for future filters, role labels, and delegated-access behavior.

## 2. Current Implementation Snapshot

| Area | Current behavior | Tenant-list implication |
| --- | --- | --- |
| Route and host page | The global owner tenant list is implemented at `/owner/tenants` through `OwnerTenantsPage`. | This journey now documents an initial shipped directory, not only a future list. |
| Query source | The page parses the current owner id from request context and calls `GetTenantsByOwnerAsync`. | The list is owner-scoped to the current owner claim; broader delegated/admin access remains future work. |
| List component | `OwnerTenantList` renders a simple empty state or one button per tenant, using a host-provided `TenantHref` builder. | The current list is intentionally simple: no search, filters, role labels, counts, or health metrics yet. |
| Tenant cards | Dashboard shows the first three tenants with `OwnerTenantCard` and links to the full list when more tenants exist. | The dashboard and full list share tenant identity as the entry point to tenant overview. |
| Create path | The list always offers **Create Workspace** linking to `/owner/tenants/create`. | Empty-state and expansion journeys should route to the current simple create-tenant form. |
| Component boundary | Host pages own routes; Parties.Interface components render reusable owner UI. | Future search/filter behavior should extend the list component without moving route ownership into the component. |

## 3. Persona Snapshot

**Name / segment:** Portfolio owner

**Situation:** Owns several workspaces and needs a directory-like view before choosing one.

**Motivation:** Know what tenants exist, which are usable, and where to go next.

**Key needs:** Accurate owner-scoped list, clear empty state, create-workspace entry, and safe open actions; search, role/status clarity, and module badges are future enhancements.

**Constraints:** The user should not see platform-admin internals or tenants they no longer have access to.

**Success means:** The user can locate a tenant by name, open its overview, or start creating a new workspace.

## 4. Journey Summary

**Narrative:** The owner opens `/owner/tenants` from the dashboard. The page loads tenants for the current owner id and renders a simple read-only list of tenant buttons. The owner selects a tenant to open its overview or uses **Create Workspace** to start the create-tenant flow. Search, filters, role labels, active markers, module badges, and delegated-access behavior are future enhancements.

**Emotional arc:** Scattered -> organized -> confident

**Biggest current friction point:** The current directory is intentionally minimal and owner-claim based; it does not yet solve delegated tenant access or rich portfolio filtering.

**Highest-value opportunity:** Preserve the simple owner-scoped list as the safe launch point and add search/filter badges only when backed by a read model.

## 5. Journey Stages

### Stage 1: Load directory

**User goal:** See every tenant they can manage or access.

**User actions:** Opens tenant list and scans tenant-name buttons.

**Touchpoints / channels:** Owner tenant list page.

**Pain points:** A missing list forces users to rely on active context or support.

**Needs:** Empty state and owner-scoped query.

**Opportunities:** Keep the current query simple and introduce a future directory read model when search/delegation is needed.

**Success metrics:** Directory load success; empty-state exits; support requests for “missing tenant.”

### Stage 2: Find tenant

**User goal:** Locate the intended tenant quickly.

**User actions:** Scans tenant names; future versions may search by name/app id and filter by role.

**Touchpoints / channels:** Tenant list buttons today; search box and richer cards/table later.

**Pain points:** Internal ids or metadata can overwhelm a nontechnical owner.

**Needs:** Human-readable tenant names now; app id, role, and filter context later.

**Opportunities:** Keep scan/open simple now; add search before complex admin filters when the list grows.

**Success metrics:** List-to-overview completion today; future zero-result searches and filter usage.

### Stage 3: Open tenant

**User goal:** Enter the right tenant overview or context.

**User actions:** Selects a tenant button to open overview.

**Touchpoints / channels:** Row/card action buttons.

**Pain points:** Opening admin actions from the wrong row can be risky.

**Needs:** Selected tenant name in CTA and route/access validation; active marker is future work.

**Opportunities:** Make tenant overview the default action and context switch an explicit secondary action.

**Success metrics:** Tenant overview opens; wrong-tenant reversal rate; action errors.

## 6. Happy Path

1. User opens the global tenant list from the owner dashboard.
2. System verifies authentication and owner/admin/member access scope.
3. System loads tenants for the current owner id through one list query.
4. Page renders tenant-name buttons and an empty state when no tenants are returned.
5. User selects the intended tenant.
6. System opens that tenant’s overview route.
7. User can then choose a dedicated management journey from the overview.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| User has no tenants | List looks broken. | Show a clear empty state and create-tenant CTA only when allowed. |
| Tenant query fails | User cannot trust the list. | Show retry and do not render stale management actions. |
| User has delegated/member-only tenant access | Owner actions may be exposed incorrectly. | Future read model should show role-appropriate rows/actions; current list is owner-id scoped. |
| Tenant is archived or disabled | User may attempt normal actions on inactive workspace. | Mark status clearly and limit actions to approved recovery/support paths. |
| Access changes during list use | User may open a revoked tenant. | Revalidate on row action and reload list on failure. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Tenant identity | `TenantReadDto` | Name, app id, logo, description, owner id. |
| Effective role | Future access query | Display user-facing role labels when delegated access is supported. |
| Active context marker | Existing active tenant context | Future read-only marker in the list. |
| Active modules | `TenantMetadata.GetActiveModules` | Future badges only for known module names. |
| Tenant status | Existing entity status if available; future tenant lifecycle model | Do not infer billing or health status. |

## 9. UX and Guardrail Rules

1. The list is read-only; mutations route to dedicated journeys.
2. Do not expose platform-admin internals to owners.
3. Use tenant name/app id over raw ids unless a support/debug mode is approved.
4. Future role labels must be legible and access-scoped.
5. Empty, error, and revoked-access states must be explicit.
6. Create-tenant is a separate journey and should not be embedded in the list form.
7. Future active tenant marker must come from the same active context as the dashboard.

## 10. Acceptance Criteria

- Owner can open a directory of accessible tenants from the dashboard.
- The directory shows tenant entries from the current owner-scoped source and keeps richer role/active/module badges as future enhancements.
- Opening a tenant from the list does not mutate active tenant context; it navigates to the route-scoped overview.
- Row actions revalidate access before navigation.
- Users with no tenants get a safe empty state.
- Owner/admin/member permissions are documented as future enhancements for delegated-access rows/actions.
