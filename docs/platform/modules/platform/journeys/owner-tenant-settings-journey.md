# Owner Tenant Settings Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state tenant settings management journey
- Entry point: Owner dashboard settings card or tenant overview settings card
- Scenario: An owner/admin needs to view and safely update tenant identity and non-destructive configuration.
- User goal: Keep workspace details accurate without breaking routing, modules, or access.
- Business goal: Enable self-service tenant configuration while protecting core tenant identity and routing rules.
- Scope start: Authorized user opens tenant settings for a validated tenant context.
- Scope end: User saves approved settings, cancels, or is routed to a more specific journey for risky changes.
- Primary persona: Organization owner
- Secondary personas: Tenant admin, platform support/admin
- Evidence sources: Owner dashboard journey, owner onboarding journey, and current tenant DTO/metadata model
- Confidence level: Medium-high for current display-field and active-module editing; low-to-medium for future indicators, audit, conflicts, and permission refinements.

## 2. Current Implementation Snapshot

| Area | Current behavior | Settings implication |
| --- | --- | --- |
| Route and host page | Tenant settings is implemented at `/owner/tenants/{tenantId:guid}/settings`. | The journey now covers a partially implemented settings page rather than only a future card. |
| Tenant loading | The page uses `LoadTenantOrRedirectAsync` and redirects to `/owner/tenants` when the route id is missing or stale. | Settings inherits the shared safe-load behavior used by tenant overview. |
| Display settings form | `OwnerTenantSettingsForm` edits name, logo URL, and description; app id is displayed disabled. | Safe display edits are live, while app id/routing remains read-only as the journey requires. |
| Save behavior | The form converts the tenant to a write DTO, calls `UpdateTenantAsync`, invokes `TenantChanged`, and shows success/error snackbars. | Current saves use the tenant write DTO, not a dedicated settings command model; audit/conflict behavior remains future work. |
| Active modules | The page renders `TenantModulesSelector`, keeps selected modules in a case-insensitive set, persists them with `TenantMetadata.SetActiveModules`, and reloads the local tenant state after save. | Module activation is now editable from settings and uses the existing `activeModules` metadata source. |
| Indicators/billing/access/danger | These are not edited in settings and remain linked/dedicated journeys. | The page keeps settings focused and does not become a catch-all admin console. |

## 3. Persona Snapshot

**Name / segment:** Workspace administrator

**Situation:** Needs to update tenant display details or operational settings after initial setup.

**Motivation:** Keep the tenant recognizable and correctly configured.

**Key needs:** Clear editable fields, validation, preview/review before risky changes, and separation from destructive actions.

**Constraints:** Some settings affect routing, module behavior, billing, or permissions and should not be changed casually.

**Success means:** The owner can update safe settings while high-risk changes are routed to dedicated guarded flows.

## 4. Journey Summary

**Narrative:** The user opens settings, sees tenant identity, editable display fields, and active-module selection. They save display changes through the settings form or save module changes through the module selector. Risky configuration such as app id, indicators, billing, archive/delete, or module dependency policy is either disabled, routed to dedicated journeys, or left as follow-up hardening.

**Emotional arc:** Practical -> careful -> reassured

**Biggest current friction point:** The owner settings page now exists, but it still uses broad tenant update DTOs and lacks conflict handling, audit behavior, and indicator management.

**Highest-value opportunity:** Keep the implemented display/module settings focused and add validation, audit, and indicator-specific flows without turning settings into an unsafe catch-all admin console.

## 5. Settings Areas

| Area | Read/write stance | Notes |
| --- | --- | --- |
| Display identity | Editable today | Tenant name, description, and logo URL are saved through `OwnerTenantSettingsForm`. |
| App id | Read-only today | Displayed disabled because changing app id can affect routing and should require separate design. |
| Active modules | Editable today | Saved through `TenantModulesSelector` and `TenantMetadata.SetActiveModules`. Dependency validation remains future work. |
| Indicators/domains | Read-only/future | Route to a future indicator management journey if simple edits are approved. |
| People/access | Link only | Managed by members/roles and invite/remove journeys. |
| Billing | Link only | Managed by billing journey. |
| Archive/delete | Link only or disabled | Managed by danger-zone journey. |

## 6. Happy Path

1. Owner opens tenant settings.
2. System loads the tenant by route id or redirects to the tenant list if the id is missing/stale.
3. Page initializes selected modules from `TenantMetadata.GetActiveModules`.
4. Page displays editable name, logo URL, description, and a disabled app id.
5. User edits approved display fields and selects **Save** in the settings form.
6. Form sends an updated tenant write DTO through `UpdateTenantAsync`.
7. Page receives `TenantChanged`, refreshes local tenant state, and shows a saved snackbar.
8. User changes active module selection and selects the module **Save** button.
9. Page writes selected module names into tenant metadata, calls `UpdateTenantAsync`, refreshes local state, and shows a saved snackbar.

## 7. Alternate and Failure Paths

| Situation | User risk | Current or recommended behavior |
| --- | --- | --- |
| Tenant id is missing/stale | Settings could edit the wrong tenant or fail silently. | Current shared helper redirects to `/owner/tenants`. |
| User has member-only access | Unauthorized configuration changes. | Recommended: show read-only or permission state and hide save controls. |
| App id change attempted | Routing/identity instability. | Current UI displays app id disabled. |
| Module metadata malformed | Settings could overwrite valid unknown data. | Current load uses tenant metadata helpers; recommended: show repair/unavailable state before save if parsing fails. |
| Update returns null | User may think changes saved. | Current code shows an error snackbar and keeps the page available. |
| Save conflict | User may overwrite another update. | Future behavior should reload latest data and ask user to review. |
| Audit write fails for sensitive setting | Change may be untraceable. | Future audit policy should decide whether to block or roll back sensitive changes. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Tenant identity | `TenantReadDto` | Display and safe-edit fields. |
| Editable settings draft | `TenantWriteDto` created from the current tenant | Current implementation; a narrower command model can be added later if conflicts/audit demand it. |
| Active modules | `TenantMetadata.GetActiveModules` / `TenantMetadata.SetActiveModules` | Current source of truth for module activation. |
| Indicators | Tenant indicator service | Read-only/future until a dedicated flow exists. |
| Audit metadata | Future audit service | Needed for sensitive setting changes. |

## 9. UX and Guardrail Rules

1. Separate safe display edits from routing, billing, access, and destructive actions.
2. Limit the current tenant write DTO usage to approved display fields and active-module metadata.
3. Keep app id and indicators read-only until dedicated change rules exist.
4. Validate before save and revalidate on the server.
5. Show tenant identity and role before editable fields.
6. Route people, billing, audit, and danger-zone tasks to dedicated journeys.
7. Keep settings simple and focused.

## 10. Acceptance Criteria

- Tenant settings validates context and owner/admin permission.
- Initial editable fields are limited to approved safe display settings.
- App id, indicators, billing, access, and destructive actions are not edited inline.
- Save uses the current tenant update service, shows success/error feedback, and documents validation/conflict/audit hardening as follow-up.
- Settings reloads from the source of truth after save.
- Sensitive setting changes are identified as audit-ready follow-up work.
