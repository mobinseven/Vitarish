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
- Confidence level: Medium for identity fields; low for final editable field set because settings are not implemented.

## 2. Current Implementation Snapshot

| Area | Current behavior | Settings implication |
| --- | --- | --- |
| Tenant DTO | Tenant has name, app id, logo, description, owner id, and metadata. | Settings can start with display identity, but app id/routing changes may need stricter policy. |
| Active modules | Module activation is stored in `activeModules` metadata. | Module settings should use the same metadata rule and validate module names. |
| Indicators | Onboarding creates a primary tenant indicator. | Indicator changes should be a dedicated routing/indicator flow unless simple edits are approved. |
| Admin pages | Admin tenant creation/listing exists. | Owner settings needs scoped permissions and safer copy. |
| Dashboard placeholders | Settings is a planned owner card. | Dashboard should link here only when journey is approved. |

## 3. Persona Snapshot

**Name / segment:** Workspace administrator

**Situation:** Needs to update tenant display details or operational settings after initial setup.

**Motivation:** Keep the tenant recognizable and correctly configured.

**Key needs:** Clear editable fields, validation, preview/review before risky changes, and separation from destructive actions.

**Constraints:** Some settings affect routing, module behavior, billing, or permissions and should not be changed casually.

**Success means:** The owner can update safe settings while high-risk changes are routed to dedicated guarded flows.

## 4. Journey Summary

**Narrative:** The user opens settings, sees tenant identity and editable safe fields, changes one or more values, and saves after validation. Risky configuration such as app id, indicators, billing, archive/delete, or broad module activation is either disabled, routed to dedicated journeys, or gated by explicit policy.

**Emotional arc:** Practical -> careful -> reassured

**Biggest current friction point:** Tenant setup data exists, but there is no owner-facing place to view or maintain it.

**Highest-value opportunity:** Start with simple display settings and avoid turning settings into an unsafe catch-all admin console.

## 5. Settings Areas

| Area | Read/write stance | Notes |
| --- | --- | --- |
| Display identity | Editable when authorized | Tenant name, description, logo if supported. |
| App id | Read-only initially | Changing app id can affect routing and should require separate design. |
| Active modules | Read-only or dedicated module-settings flow | Use `activeModules` metadata and validate dependencies before mutation. |
| Indicators/domains | Read-only initially | Route to future indicator management journey. |
| People/access | Link only | Managed by members/roles and invite/remove journeys. |
| Billing | Link only | Managed by billing journey. |
| Archive/delete | Link only or disabled | Managed by danger-zone journey. |

## 6. Happy Path

1. Owner opens tenant settings.
2. System validates tenant context and owner/admin permission.
3. System loads tenant identity from the source-of-truth read model.
4. Page displays read-only app id, owner context, active modules, and indicator summary if available.
5. User edits approved safe fields such as tenant display name or description.
6. System validates required fields and field-specific constraints.
7. User saves.
8. System revalidates permission, applies the update through a settings command, and writes an audit event if required.
9. Page reloads tenant identity and shows success feedback.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| User has member-only access | Unauthorized configuration changes. | Show read-only or permission state; hide save controls. |
| App id change attempted | Routing/identity instability. | Keep read-only until a dedicated policy exists. |
| Module metadata malformed | Settings could overwrite valid unknown data. | Show module unavailable state and avoid saving module changes. |
| Validation fails | Bad identity data. | Show inline errors and keep draft. |
| Save conflict | User may overwrite another update. | Reload latest data and ask user to review. |
| Audit write fails for sensitive setting | Change may be untraceable. | Block or roll back per approved audit policy. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Tenant identity | `TenantReadDto` | Display and safe-edit fields. |
| Editable settings draft | Future tenant settings command model | Separate from read model to avoid accidental full DTO updates. |
| Active modules | `TenantMetadata.GetActiveModules` | Prefer read-only in initial settings release. |
| Indicators | Tenant indicator service | Read-only summary until dedicated flow exists. |
| Audit metadata | Future audit service | Needed for sensitive setting changes. |

## 9. UX and Guardrail Rules

1. Separate safe display edits from routing, billing, access, and destructive actions.
2. Do not use a full tenant DTO as an unrestricted edit form.
3. Keep app id and indicators read-only until dedicated change rules exist.
4. Validate before save and revalidate on the server.
5. Show tenant identity and role before editable fields.
6. Route people, billing, audit, and danger-zone tasks to dedicated journeys.
7. Keep settings simple and focused.

## 10. Acceptance Criteria

- Tenant settings validates context and owner/admin permission.
- Initial editable fields are limited to approved safe display settings.
- App id, indicators, billing, access, and destructive actions are not edited inline.
- Save uses a dedicated command model and handles validation/conflict failures.
- Settings reloads from the source of truth after save.
- Sensitive setting changes are audit-ready.
