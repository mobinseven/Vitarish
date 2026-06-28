# Platform Administration — Users & Owners Journey

## 1. Context

- App: Platform-wide
- Module: Platform
- Capability: User management, owner management, tenant administration
- Related apps: All apps
- Related modules: Parties (owner/tenant entities), Users (user lookup), Platform (theme, search)
- Product / service: Vitarish platform administration
- Journey type: Current state with gaps
- Scenario: Platform admin manages users, owners, and tenants across the platform.
- User goal: Maintain platform-wide user and tenant records; support operators and tenants.
- Business goal: Ensure platform health, support tenant operations, manage user access at scale.
- Scope start: Platform admin navigates to the admin section.
- Scope end: User/owner/tenant records are viewed, edited, or managed.
- Primary persona: Platform administrator
- Evidence sources: Static code review of platform admin pages, Parties module, host configuration
- Confidence level: Medium

## 2. Persona Snapshot

**Name / segment:** Platform administrator

**Situation:** Managing the entire Vitarish platform across multiple tenants and owners.

**Motivation:** Maintain platform health, support tenant issues, manage user access.

**Key needs:** User listing, owner management, tenant overview, theme configuration, search index management.

**Constraints:** No bulk user operations; no impersonation; no audit trail viewing from admin pages.

**Success means:** Platform issues are resolved quickly; tenants are configured correctly; users have appropriate access.

## 3. Journey Summary

**Narrative:** The platform admin navigates to the admin section, lists owners, selects an owner to view their tenants, edits tenant details including indicators and module configuration, manages themes, and monitors search and commerce provisioning.

**Emotional arc:** Start: systematic (navigating admin structure) → Middle: attentive (editing tenant details) → End: confident (changes applied)

**Biggest friction point:** No bulk operations or cross-tenant search; admin must navigate hierarchically (owners → tenants → details).

**Highest-value opportunity:** Add cross-tenant search, bulk user operations, and impersonation for support.

## 4. Journey Stages

### Stage 1: Platform Dashboard

**User goal:** Get an overview of platform health.

**User actions:** Open platform admin home; view search, communications, connection memory, themes.

**Touchpoints / channels:** `PlatformAdminHomePage`, `PlatformAdminSearchPage`, `PlatformAdminCommunicationsPage`, `PlatformAdminConnectionMemoryPage`, `PlatformAdminThemesPage`.

**User thoughts / questions:** "What needs my attention across the platform?"

**Emotions:** Systematic, 3, because the dashboard provides entry points.

**Pain points:** No aggregate platform health metrics; no alerting or anomaly detection.

**Needs:** Platform health dashboard with key metrics.

**Opportunities:** Add platform health metrics, alert counts, and quick-action cards.

**Success metrics:** Dashboard view frequency; time-to-first-action from dashboard.

**Evidence:** Static code review assumption.

### Stage 2: Owner Management

**User goal:** View and manage platform-wide owners.

**User actions:** List all owners; select an owner; view owner details and their tenants.

**Touchpoints / channels:** `PlatformAdminOwnersPage`, `PlatformAdminOwnerPage`, `OwnerEditor`.

**User thoughts / questions:** "Who owns what? Which tenants belong to this owner?"

**Emotions:** Focused, 3, because the list is straightforward.

**Pain points:** No search/filter on owner list; no owner activity metrics; no way to see owner's users across tenants.

**Needs:** Owner search, activity summary, cross-tenant user view.

**Opportunities:** Add owner search, activity metrics, and cross-tenant user listing.

**Success metrics:** Time to locate specific owner; owner management task completion rate.

**Evidence:** Static code review assumption.

### Stage 3: Tenant Administration

**User goal:** Edit tenant details, indicators, and module configuration.

**User actions:** Select tenant from owner's list; edit tenant fields; manage indicators; select active modules and apps; save changes.

**Touchpoints / channels:** `PlatformAdminTenantPage`, `TenantEditor`, `TenantIndicatorsEditor`, `TenantModulesSelector`, `TenantAppsSelector`.

**User thoughts / questions:** "Is this tenant configured correctly? Are the right modules active?"

**Emotions:** Careful, 4, because tenant configuration affects all users.

**Pain points:** No confirmation before saving tenant changes; no change history; indicator DNS verification is manual.

**Needs:** Save confirmation, change audit trail, automated DNS verification.

**Opportunities:** Add save confirmation dialog, change history panel, and DNS verification status.

**Success metrics:** Tenant configuration accuracy; change rollback frequency.

**Evidence:** Static code review assumption.

### Stage 4: User Management

**User goal:** Find and manage platform-wide users.

**User actions:** Search for users; view user details; manage user roles.

**Touchpoints / channels:** `PlatformAdminUsersPage`, `PlatformAdminUserPage`, `UserFindForm`, `RoleEditor`.

**User thoughts / questions:** "Where is this user? What roles do they have across tenants?"

**Emotions:** Neutral, 3, because user search is functional.

**Pain points:** No cross-tenant user search; no bulk role assignment; no user activity history.

**Needs:** Cross-tenant search, bulk operations, activity history.

**Opportunities:** Add cross-tenant user search with tenant context display.

**Success metrics:** User search success rate; role change accuracy.

**Evidence:** Static code review assumption.

### Stage 5: Theme Management

**User goal:** Configure platform-wide themes and tenant-specific themes.

**User actions:** View theme list; edit theme colors; preview theme; save changes.

**Touchpoints / channels:** `PlatformAdminThemesPage`, `PlatformAdminThemeEditPage`, `PlatformThemeForm`.

**User thoughts / questions:** "Does this theme look right? Will it work for tenants?"

**Emotions:** Creative, 3, because theme editing is visual.

**Pain points:** No theme preview in context; no theme versioning; no rollback.

**Needs:** Theme preview, versioning, rollback.

**Opportunities:** Add live theme preview with tenant sample data.

**Success metrics:** Theme edit frequency; tenant satisfaction with themes.

**Evidence:** Static code review assumption.

### Stage 6: Commerce & Search Monitoring

**User goal:** Monitor commerce provisioning and search index health.

**User actions:** View commerce provisioning status; trigger sync; view search index status.

**Touchpoints / channels:** `PlatformAdminCommercePage`, `PlatformAdminCommerceProvisionPage`, `PlatformAdminCommerceSyncPage`, `PlatformAdminSearchPage`.

**User thoughts / questions:** "Is commerce synced? Is the search index healthy?"

**Emotions:** Alert, 3, because these are operational concerns.

**Pain points:** Sync status is manual; no automated health checks; no alerting on sync failures.

**Needs:** Automated sync health checks; failure alerting.

**Opportunities:** Add automated health monitoring and alerting.

**Success metrics:** Sync success rate; search index freshness.

**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points

1. Hierarchical navigation (owners → tenants → details) is slow for cross-cutting tasks — occurs at every admin operation.
2. No bulk operations for users, roles, or tenants — occurs at scale.
3. No change audit trail for tenant configuration — occurs at save.

### Moments that matter

- **Moment of truth:** Saving tenant module configuration — changes affect all tenant users immediately.
- **Moment of friction:** Searching for a user across multiple tenants requires navigating each owner's tenant list.
- **Moment of delight:** Theme preview shows exactly how the tenant will look.

### Design principles implied by the journey

- Provide cross-cutting search, not just hierarchical navigation.
- Confirm destructive or wide-impact changes before applying.
- Keep audit trails for configuration changes.

## 6. Implementation Derivation

### Required artifacts

| Artifact | Why it is needed | Journey source | Priority |
|---|---|---|---|
| Cross-tenant user search | Speeds up user lookup across the platform | Stage 4 | High |
| Tenant change audit trail | Records who changed what and when | Stage 3 | High |
| Save confirmation dialog | Prevents accidental tenant config changes | Stage 3 | Medium |
| Platform health metrics | Provides at-a-glance platform status | Stage 1 | Medium |

### Feature slices

| Slice | User value | Includes | Excludes | Acceptance signal |
|---|---|---|---|---|
| Cross-tenant user search | Find users across all tenants quickly | Search input, user results with tenant context, role display | Bulk role assignment | User search time decreases |
| Tenant change audit | Know who changed tenant config and when | Audit log panel on tenant page, change diff display | Real-time alerting | Configuration disputes resolve faster |
| Save confirmation | Prevent accidental tenant config changes | Confirmation dialog before save, change summary | Undo/rollback | Accidental saves decrease |

### Domain / data implications

- Add audit log entity for tenant configuration changes.
- Add cross-tenant user query across all PartyScope boundaries.
- Add platform health metrics aggregation.

### UI implications

- Add search bar to platform admin pages.
- Add audit log panel to tenant admin page.
- Add confirmation dialog before tenant save.

### API / service implications

- Add cross-tenant user search endpoint.
- Add tenant change audit write on save.
- Add platform health metrics query.

### Validation, permissions, and safety

- Only platform admins can access admin pages.
- Tenant changes should require confirmation.
- Audit logs should be immutable.

### Tests and acceptance criteria

- Cross-tenant search returns users from all tenants.
- Tenant config changes are recorded in audit log.
- Save confirmation prevents accidental saves.

### Metrics

- User search time.
- Tenant config change accuracy.
- Platform health score.

## 7. Technical Detail Addendum

### Current implementation touchpoints

- `src/Hosts/Main/Pages/Platform/PlatformAdminHomePage.razor` — admin dashboard with links to sub-sections.
- `src/Hosts/Main/Pages/Platform/PlatformAdminUsersPage.razor` — lists all users as buttons with "Add User" button.
- `src/Hosts/Main/Pages/Platform/PlatformAdminUserPage.razor` — renders `UserFindForm` for user search.
- `src/Hosts/Main/Pages/Platform/PlatformAdminOwnersPage.razor` — lists all owners as buttons with "Add Owner" button.
- `src/Hosts/Main/Pages/Platform/PlatformAdminOwnerPage.razor` — renders `OwnerEditor` and tenant list for selected owner.
- `src/Hosts/Main/Pages/Platform/PlatformAdminTenantPage.razor` — full tenant editor: `TenantEditor`, `TenantIndicatorsEditor`, `TenantModulesSelector`, `TenantAppsSelector`, Save and Delete buttons.
- `src/Hosts/Main/Pages/Platform/PlatformAdminThemesPage.razor` — theme list.
- `src/Hosts/Main/Pages/Platform/PlatformAdminThemeEditPage.razor` — theme editor using `PlatformThemeForm`.
- `src/Hosts/Main/Pages/Platform/PlatformAdminSearchPage.razor` — search index management.
- `src/Hosts/Main/Pages/Platform/PlatformAdminCommunicationsPage.razor` — communications management.
- `src/Hosts/Main/Pages/Platform/PlatformAdminConnectionMemoryPage.razor` — connection memory management.
- `src/Modules/Parties/Interface/Components/Admins/OwnerEditor.razor` — edit owner fields.
- `src/Modules/Parties/Interface/Components/Admins/TenantEditor.razor` — edit tenant fields.
- `src/Modules/Parties/Interface/Components/Admins/TenantIndicatorsEditor.razor` — manage tenant indicators with add/edit/delete dialog.
- `src/Modules/Users/Interface/Components/UserFindForm.razor` — debounced user search with scope selector.
- `src/Modules/Users/Interface/Components/Admins/RoleEditor.razor` — role assignment table with Read/Write checkboxes.
- `src/Modules/Platform/Interface/Components/PlatformThemeForm.razor` — full theme editor with palette preview.

### Issues to address

- No cross-tenant user search; `UserFindForm` defaults to `TenantUsers` scope and requires `AllPlatformUsers` selection per search.
- `PlatformAdminTenantPage` saves tenant changes without confirmation; add save confirmation dialog.
- No audit trail for tenant configuration changes; who changed modules, indicators, or settings is not recorded.
- `PlatformAdminOwnersPage` and `PlatformAdminUsersPage` are simple button lists without search or filter.
- `TenantIndicatorsEditor` supports DNS verification conceptually but no automated verification flow exists.
- Theme editing (`PlatformThemeForm`) has no live preview in context; changes are applied blindly.
- No platform health metrics or aggregate statistics on the admin dashboard.
