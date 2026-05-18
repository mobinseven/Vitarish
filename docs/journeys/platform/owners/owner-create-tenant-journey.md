# Owner Create Tenant Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state create-tenant flow for existing owners
- Entry point: Owner dashboard create-tenant card, global tenant list empty state, or tenant switcher empty state
- Scenario: An authenticated owner who already has an owner/account relationship needs to create an additional tenant without repeating full first-time onboarding.
- User goal: Add a new workspace with required identity, indicator, and active modules.
- Business goal: Support owner growth while reusing setup guardrails and avoiding duplicate owner records.
- Scope start: Existing owner selects **Create tenant** from an owner surface.
- Scope end: New tenant is created, role assignments are established, and the user is directed to the new tenant overview or dashboard.
- Primary persona: Organization owner expanding to another workspace
- Secondary personas: Tenant admin if delegated creation is approved, platform support/admin
- Evidence sources: Owner onboarding journey and dashboard journey map
- Confidence level: Medium for flow shape because onboarding exists; low for owner identity mapping and delegated creation rules.

## 2. Current Implementation Snapshot

| Area | Current behavior | Create-tenant implication |
| --- | --- | --- |
| Setup onboarding | `SetupTenantAsync` creates owner, tenant, primary indicator, active-module metadata, and role assignments. | Existing-owner flow should reuse tenant/indicator/module concepts but not create a duplicate owner. |
| Owner identity | Tenant records carry `OwnerId`, but the canonical authenticated user-to-owner relationship needs clarification. | Create-tenant must know which owner receives the new tenant. |
| Feature flag | Tenant onboarding is feature-gated. | Existing-owner creation may need its own feature flag or use the same gate until approved. |
| Active modules | Setup stores selected modules in tenant metadata. | Create-tenant should use the same `activeModules` metadata source. |
| Admin tenant creation | Admin pages can create tenants with defaults. | Owner-facing flow needs explicit validation, copy, and scoped permissions. |

## 3. Persona Snapshot

**Name / segment:** Expanding owner

**Situation:** Already uses Vitarish and needs a second workspace for another organization, branch, practice, or business unit.

**Motivation:** Create the new workspace quickly without contacting support or repeating first-time owner setup.

**Key needs:** Clear ownership, duplicate-safe tenant identity, indicator validation, module selection, and safe post-create navigation.

**Constraints:** The user may not understand indicators, app id uniqueness, active modules, or owner records.

**Success means:** The owner creates one new tenant under the correct owner relationship and becomes able to manage it immediately.

## 4. Journey Summary

**Narrative:** The existing owner starts create-tenant, confirms the owner/account context, enters tenant identity, configures the primary indicator, selects active modules, reviews the summary, and submits once. The system creates only tenant-scoped records and assigns the user appropriate roles, then opens the new tenant overview.

**Emotional arc:** Ambitious -> guided -> reassured -> ready to configure

**Biggest current friction point:** The current onboarding flow creates both owner and tenant; existing-owner tenant creation needs a smaller flow that reuses setup rules without duplicating ownership.

**Highest-value opportunity:** Split first-time onboarding from repeat tenant creation while keeping one source of truth for tenant setup validation.

## 5. Wizard Screen Model

| Step | Required inputs | Defaults / automation | Primary CTA | Validation / guardrail |
| --- | --- | --- | --- | --- |
| Owner context | Existing owner/account selection or confirmation | Preselect canonical owner when unambiguous | Continue | Block if no owner relationship can be resolved. |
| Tenant details | Tenant name, app id | Suggest slug/app id only when approved | Continue | Required fields and uniqueness checks. |
| Indicator | Method and address | Default to same method used by onboarding if appropriate | Continue | Required address and duplicate/conflict validation. |
| Modules | Selected module names | Registered module list | Review | Validate module names and dependency rules when they exist. |
| Review | Summary of owner, tenant, indicator, modules | No mutation until submit | Create tenant | Disable while submitting; submit once. |

## 6. Happy Path

1. Owner opens **Create tenant** from the dashboard or tenant list.
2. System verifies authentication and create-tenant permission.
3. System resolves the canonical owner relationship for the signed-in user.
4. User confirms owner context if more than one owner relationship exists.
5. User enters tenant name and app id.
6. User enters primary tenant indicator method and address.
7. User selects active modules from the registered module list.
8. User reviews the summary.
9. System validates owner relationship, uniqueness, indicator availability, module names, and current permission.
10. System creates the tenant under the existing owner.
11. System stores active modules in tenant metadata.
12. System creates the primary active tenant indicator.
13. System assigns the creating user to appropriate tenant roles.
14. System opens the new tenant overview or owner dashboard with the new tenant context.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| No owner relationship resolved | Tenant may be orphaned or attached incorrectly. | Block creation and show support/setup guidance. |
| Multiple owner relationships | Tenant could be created under wrong owner. | Require explicit owner selection before details. |
| App id conflict | New tenant cannot be uniquely addressed. | Show inline error and keep draft. |
| Indicator conflict | Tenant routing may be ambiguous. | Show conflict message and require another address. |
| Module list unavailable | Tenant may be created without clear capabilities. | Block module step or allow no modules only if approved. |
| Submit partially fails | Inconsistent tenant setup. | Use transaction or compensating cleanup; show retry/support state. |
| User loses permission mid-flow | Unauthorized tenant creation. | Revalidate on submit and block mutation. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Owner id | Future canonical user-to-owner lookup | Must not rely on ambiguous tenant owner inference. |
| Tenant identity | Tenant create command | Name/app id required. |
| Primary indicator | Tenant indicator create command | Same method/address concepts as onboarding. |
| Active modules | Registered modules and `TenantMetadata` | Store as `activeModules` metadata. |
| Creator roles | Tenant role assignment service | Apply only after tenant creation succeeds. |

## 9. UX and Guardrail Rules

1. Do not create a new owner record in the existing-owner flow.
2. Keep create flow separate from the read-only tenant list and overview.
3. Validate owner relationship before accepting tenant details.
4. Validate uniqueness before final submit and again server-side on submit.
5. Store module activation in the same metadata shape used by onboarding.
6. Redirect to the new tenant overview when context switching is available.
7. Keep the flow short; do not add billing, members, or settings before tenant exists.

## 10. Acceptance Criteria

- Existing owner can start a create-tenant journey from owner surfaces.
- Flow confirms or resolves the owner relationship before tenant creation.
- Tenant details, primary indicator, and active modules are collected with validation.
- Submit creates tenant-scoped records without duplicating the owner.
- Creating user receives appropriate tenant roles.
- Partial failures are handled safely without exposing broken tenant actions.
