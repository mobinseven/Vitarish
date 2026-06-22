# Owner Create Tenant Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Current-state direct create-tenant flow with future wizard enhancements
- Entry point: Owner dashboard create-tenant card, global tenant list empty state, or tenant switcher empty state
- Scenario: An authenticated owner who already has an owner/account relationship needs to create an additional tenant without repeating full first-time onboarding.
- User goal: Add a new workspace with required identity and a primary indicator, then configure modules from tenant settings.
- Business goal: Support owner growth while reusing setup guardrails and avoiding duplicate owner records.
- Scope start: Existing owner selects **Create tenant** from an owner surface.
- Scope end: New tenant and primary indicator are created, and the user is directed to the new tenant overview.
- Primary persona: Organization owner expanding to another workspace
- Secondary personas: Tenant admin if delegated creation is approved, platform support/admin
- Evidence sources: Owner onboarding journey and dashboard journey map
- Confidence level: Medium-high for the current route/form behavior; low-to-medium for duplicate validation, partial-failure recovery, role assignment, modules, and delegated creation rules.

## 2. Current Implementation Snapshot

| Area | Current behavior | Create-tenant implication |
| --- | --- | --- |
| Route and host page | Create tenant is implemented at `/owner/tenants/create` through `OwnerCreateTenantPage`. | This journey should describe the current direct form as the first release and keep the multi-step wizard as a future enhancement. |
| Form component | `OwnerCreateTenantForm` captures tenant name, app id, indicator method, and indicator address. | The first release requires only identity plus one primary indicator; it does not collect modules or review-step data. |
| Owner resolution | The form reads `RequestContext.OwnerId`, parses it as a Guid, and blocks creation with an error snackbar when it is missing/invalid. | Existing-owner creation depends on the current owner claim and does not yet solve multi-owner selection. |
| Tenant creation | The form creates a tenant with a new id, the current owner id, tenant name, and app id. | Owner-facing creation now avoids creating a duplicate owner record, but it also bypasses the richer onboarding orchestration. |
| Indicator creation | After tenant creation succeeds, the form creates one active primary tenant indicator with the selected method/address. | Tenant and indicator creation are sequential; partial failure after tenant creation currently surfaces an error but needs recovery guidance. |
| Post-create navigation | The host page receives `OnTenantCreated` and navigates to the new tenant overview. | The scope end is now the tenant overview route, not automatic active-context switching. |
| Active modules | The current create form does not choose or persist active modules. | Module activation happens later on tenant settings via `activeModules` metadata. |

## 3. Persona Snapshot

**Name / segment:** Expanding owner

**Situation:** Already uses Vitarish and needs a second workspace for another organization, branch, practice, or business unit.

**Motivation:** Create the new workspace quickly without contacting support or repeating first-time owner setup.

**Key needs:** Clear ownership, required identity fields, primary indicator capture, duplicate-safe validation, safe post-create navigation, and a clear path to module configuration.

**Constraints:** The user may not understand indicators, app id uniqueness, active modules, partial setup failure, or owner records.

**Success means:** The owner creates one new tenant under the current owner relationship, gets a primary active indicator, and lands on the tenant overview for follow-up configuration.

## 4. Journey Summary

**Narrative:** The existing owner starts create-tenant from the dashboard or tenant list, enters tenant name/app id, chooses an indicator method, enters an indicator address, and submits once. The system creates the tenant under the current owner id, creates one active primary indicator, and opens the new tenant overview. Module activation, advanced validation, role repair, and multi-owner selection remain follow-up work.

**Emotional arc:** Ambitious -> guided -> reassured -> ready to configure

**Biggest current friction point:** The current form is intentionally small and sequential; if indicator creation fails after tenant creation, the journey needs clearer recovery or transactional behavior.

**Highest-value opportunity:** Keep repeat tenant creation simple while reusing the same tenant/indicator concepts and routing to settings for module activation.

## 5. Current Form Model

| Step / region | Required inputs | Defaults / automation | Primary CTA | Validation / guardrail |
| --- | --- | --- | --- | --- |
| Tenant details | Tenant name and app id | None | Create | Client blocks empty tenant name/app id. Future work should add uniqueness feedback. |
| Primary indicator | Method and address | Method defaults to `Subdomain`; created indicator is `Active` and primary | Create | Client blocks empty address. Future work should add duplicate/conflict validation before mutation. |
| Owner context | Current `RequestContext.OwnerId` | Parsed as a Guid by the form | Create | Block with an error snackbar when owner id is missing or invalid. |
| Post-create navigation | Newly created tenant id | Host `OnTenantCreated` callback | Automatic redirect | Navigate to `/owner/tenants/{tenantId}` after successful tenant and indicator creation. |
| Future wizard additions | Module selection, review, duplicate checks, role repair | Not present today | Future | Add only when supporting services are ready. |

## 6. Happy Path

1. Owner opens **Create Workspace** from the dashboard empty state or tenant list.
2. System renders the create-tenant form at `/owner/tenants/create`.
3. User enters tenant name and app id.
4. User keeps or changes indicator method.
5. User enters primary indicator address.
6. User selects **Create**.
7. Form verifies required fields and parses the current owner id.
8. System calls tenant creation with a new tenant id, owner id, name, and app id.
9. System creates one primary active tenant indicator for the new tenant.
10. Host receives the created tenant and navigates to the tenant overview route.
11. User configures active modules later from tenant settings if needed.

## 7. Alternate and Failure Paths

| Situation | User risk | Current or recommended behavior |
| --- | --- | --- |
| Required fields are empty | Bad tenant or indicator data. | Current form shows a required-fields warning and does not submit. |
| Owner id is missing/invalid | Tenant could be orphaned or attached incorrectly. | Current form shows an error snackbar and does not submit. |
| Tenant creation returns null/fails | No workspace is created. | Current form shows an error snackbar and re-enables submit. |
| Indicator creation returns null/fails after tenant creation | Tenant may exist without the expected primary indicator. | Current form shows an error snackbar; recommended next step is transaction/compensation or a recovery route. |
| App id or indicator conflict | New tenant may be invalid or ambiguous. | Future validation should show inline conflict feedback before mutation and revalidate server-side. |
| Multiple owner relationships | Tenant could be created under wrong owner. | Future flow should require explicit owner selection before details. |
| User loses permission mid-flow | Unauthorized tenant creation. | Revalidate before mutation and block submit. |
| Module setup is needed | Tenant starts without activated modules from this form. | Route to tenant settings for active-module selection. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Owner id | Current `RequestContext.OwnerId` | Current source; future multi-owner accounts need a canonical selectable owner relationship. |
| Tenant identity | `TenantWriteDto` from the form | Name/app id required. |
| Primary indicator | `TenantIndicatorWriteDto` from the form | Method/address required; status is `Active`; indicator is primary. |
| Active modules | Tenant settings via `TenantMetadata.SetActiveModules` | Not captured during create today. |
| Creator roles | Existing tenant creation/setup policies | Not explicit in the form; verify whether tenant creation service guarantees required roles. |
| Partial-failure state | Future recovery/transaction model | Needed when tenant succeeds but indicator fails. |

## 9. UX and Guardrail Rules

1. Do not create a new owner record in the existing-owner flow.
2. Keep create flow separate from the read-only tenant list and overview.
3. Validate required fields before submitting.
4. Validate owner relationship before creating tenant records.
5. Treat tenant creation plus primary-indicator creation as one user goal; add transaction/compensation or recovery for partial failures.
6. Redirect to the new tenant overview after successful create.
7. Keep module activation in tenant settings until the create form has a designed module step.
8. Keep the flow short; do not add billing, members, or advanced settings before tenant exists.

## 10. Acceptance Criteria

- Existing owner can start create tenant from owner surfaces.
- Form collects tenant name, app id, indicator method, and indicator address.
- Submit blocks empty required fields and missing/invalid owner id.
- Submit creates tenant-scoped records without duplicating the owner.
- Successful tenant and indicator creation redirects to the new tenant overview.
- Partial failures are surfaced safely and documented as a recovery/transaction follow-up.
- Module activation is clearly handled later through tenant settings.
