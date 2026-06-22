# Owner Onboarding Journey Map

## 1. Context

- Product / service: Vitarish platform onboarding
- Journey type: Current-state self-service tenant setup flow, delivered behind a feature flag
- Entry point: Authenticated user selects **Create Workspace** from the home page or navigates to `/setup`
- Scenario: A signed-in user without the desired tenant context creates an owner record, tenant, primary tenant indicator, tenant role assignments, and active module list through a short wizard.
- User goal: Create a usable workspace and start from the correct tenant context quickly.
- Business goal: Let an authenticated customer bootstrap their own tenant without support intervention while keeping the feature safely gated.
- Scope start: Authenticated user reaches the home page with `Vitarish:Features:TenantOnboarding` enabled.
- Scope end: Tenant setup is submitted successfully and the user is returned to the home page, where tenant module dashboard shortcuts can appear after tenant context resolves.
- Primary persona: Organization owner / first administrator
- Friction principle: Ask only for the fields required to create a tenant, one primary indicator, and the initial active-module list.

## 2. Feature Gate and Access Rules

The onboarding journey is controlled by the `TenantOnboarding` feature flag in the `Vitarish:Features` configuration section. The template default is `false`, so operators must explicitly enable the flow before users can access it.

| Condition | System behavior | User impact |
| --- | --- | --- |
| Feature flag disabled | `/setup` redirects to home and the home page setup button is hidden. | No unfinished self-service path is exposed. |
| Feature flag enabled and user authenticated | Home page can show **Create Workspace** when no tenant dashboard is available. | The user has a visible self-service entry point. |
| Feature flag enabled and user unauthenticated | `/setup` redirects to login. | Tenant creation remains tied to an authenticated user. |
| Tenant context resolved with active modules | Home page shows dashboard shortcuts for active modules that have known dashboard routes. | Returning users can jump directly into enabled work areas. |

## 3. Persona Snapshot

**Name / segment:** First-time organization owner

**Situation:** Wants to evaluate or start using Vitarish for an organization, practice, team, or business unit.

**Motivation:** Create a secure workspace and activate only the modules needed now.

**Key needs:** Clear ownership, trustworthy tenant identity, obvious module selection, and a safe retry path if setup fails.

**Constraints:** The user may not understand internal platform terms such as owner, tenant, app id, hostname, subdomain, or module.

**Success means:** The user creates a tenant without support, manual role assignment, or separate admin screens for the initial indicator and module activation.

## 4. Journey Summary

**Narrative:** The authenticated user starts the setup wizard, completes workspace details, defines one primary hostname or subdomain indicator, selects modules from the registered module list, and submits once. The system creates the owner, tenant, primary indicator, active-module metadata, and tenant role assignments on behalf of the user.

**Emotional arc:** Motivated -> guided -> in control -> ready to enter the workspace

**Biggest friction point addressed:** The previous target journey required tenant creation, indicator setup, module activation, and role assignment to feel like one action instead of multiple unrelated admin tasks.

**Current implementation shape:** Three wizard steps share one `TenantSetupContext` until final submission.

**Important current limitation:** The wizard does not yet generate suggested slugs, preselect recommended modules, resolve module dependencies, or automatically enter the newly created tenant context after submit. Those remain future iteration opportunities.

## 5. Wizard Screen Model

| Step | Required inputs | Defaults / automation | Primary CTA | Validation / guardrail |
| --- | --- | --- | --- | --- |
| Details | Owner name, tenant name, app id | Empty draft values held in the wizard context | Next | Required-field check before moving forward. |
| Indicator | Method, address | Method defaults to `Subdomain`; submitted status is `Active`; indicator is primary | Next | Indicator address is required before module selection. |
| Modules | Selected module names | Module list comes from the registered `IReadOnlyList<IModule>` collection | Finish | Submit button is disabled while the request is in progress. |

## 6. Happy Path

1. User signs in.
2. System loads the home page.
3. If `TenantOnboarding` is enabled and no tenant dashboard is available, the user selects **Create Workspace**.
4. System opens `/setup` and verifies that the feature flag is still enabled.
5. System verifies that the request has an authenticated user id; otherwise it redirects to login.
6. User enters owner name, tenant name, and app id.
7. User selects the indicator method: hostname or subdomain.
8. User enters the indicator address.
9. User selects the modules that should be active for the tenant.
10. User submits setup.
11. System sends one setup request containing the collected details, indicator data, selected module names, and authenticated user id.
12. System creates the owner record.
13. System creates the tenant and stores `activeModules` in tenant metadata.
14. System creates the primary active tenant indicator.
15. System assigns the user to the tenant owner, tenant admin, and tenant user roles using fully qualified tenant role names.
16. System redirects back to home with a full page reload.
17. Once tenant context resolves, the home page reads `activeModules` and shows dashboard shortcuts for active modules that have known routes.

## 7. Service Orchestration

The setup submit action maps directly to a single application-service operation: `SetupTenantAsync`.

| Sequence | Responsibility | Notes |
| ---: | --- | --- |
| 1 | Create owner | The audit `CreatedBy` value provides the implicit owner-to-user link. |
| 2 | Create tenant | The selected module names are persisted under tenant metadata key `activeModules`; tenant role creation is triggered by tenant creation. |
| 3 | Create primary indicator | The wizard supplies method and value; setup submits `Active` status and marks it primary. |
| 4 | Assign roles | The authenticated user is added to `{tenantId}:TenantOwner`, `{tenantId}:TenantAdmin`, and `{tenantId}:TenantUser`. |
| 5 | Return tenant | A null result indicates setup failed and the UI shows a recoverable error. |

## 8. Home Page Continuation

After setup, home is responsible for presenting the next useful action:

- If a tenant context exists and tenant metadata contains valid `activeModules`, the page filters those module names through the known dashboard-route map.
- Dashboard buttons are shown only for activated modules with mapped routes: Sea, Medical, Commerce, and Appointments.
- If `activeModules` metadata is malformed, the page degrades safely by showing no dashboard shortcuts instead of failing the home page.
- If no tenant context is available and onboarding is enabled, the existing tenant widget remains available and the **Create Workspace** button gives the user an explicit setup path.

## 9. Failure and Recovery Rules

| Failure point | Current behavior | Recommended next improvement |
| --- | --- | --- |
| Feature disabled | Redirect to home. | Add operator-facing documentation that explains how to enable the flag in non-production environments. |
| User unauthenticated | Redirect to login. | Preserve a return URL to `/setup` after login. |
| Required fields missing | Show a warning snackbar and keep the current wizard state. | Add field-level messages for clearer correction. |
| Setup service returns null | Show a generic error snackbar and keep the page active. | Surface a safer user-readable error reason and add telemetry. |
| Indicator creation or role assignment partial failure | The current service continues through sequential operations without an explicit transaction boundary. | Wrap setup in an all-or-nothing transaction or compensating cleanup path. |
| Malformed active-module metadata | Home page catches JSON parsing errors and shows no dashboard shortcuts. | Add metadata validation at write time and migration/repair tooling. |

## 10. Friction Reduction Rules

1. **One source of setup truth:** Keep all wizard state in `TenantSetupContext` until the final submit.
2. **One orchestration call:** Keep owner creation, tenant creation, indicator creation, module activation metadata, and role assignment behind `SetupTenantAsync`.
3. **No manual owner assignment:** The authenticated setup user becomes tenant owner automatically.
4. **Gate unfinished functionality:** Keep the wizard behind `TenantOnboarding` until setup recovery, tenant-context switching, and recommendations are mature enough for broad rollout.
5. **Validate before transitions:** Do not let users advance past required setup data.
6. **Defer optional work:** Branding, invitations, billing, profile enrichment, and advanced module configuration happen after the tenant exists.
7. **Degrade safely:** Missing or malformed active-module metadata should not block the home page.

## 11. Click and Decision Inventory

| Step | User decision | Required? | Current default | Recommendation |
| --- | --- | --- | --- | --- |
| Owner name | Who owns the workspace record? | Yes | None | Consider deriving an initial value from the authenticated profile. |
| Tenant name | What is the workspace called? | Yes | None | Keep as a single text input. |
| App id | Which app id should the tenant use? | Yes | None | Explain expected format or provide a generated suggestion. |
| Indicator method | How should the tenant be resolved? | Yes | Subdomain | Keep the default, but explain when to choose hostname. |
| Indicator address | What hostname or subdomain identifies this tenant? | Yes | None | Add availability and format checks before final submit. |
| Modules | Which capabilities start active? | No explicit minimum in UI | None selected | Preselect recommended modules once recommendation rules exist. |
| Owner/admin/user roles | What access should the creator receive? | Yes | Fully automatic | Keep hidden from the user. |

## 12. Acceptance Criteria Reflected by the Current PR

- The self-service onboarding route is available at `/setup` only when `Vitarish:Features:TenantOnboarding` is enabled.
- The feature flag defaults to disabled in template configuration.
- Unauthenticated users cannot create a tenant through `/setup`.
- The wizard uses three steps: details, indicator, and modules.
- Required fields are checked before moving from details to indicator and from indicator to modules.
- The final submit sends one setup request with owner, tenant, app id, indicator, selected modules, and authenticated user id.
- Tenant creation stores selected module names in tenant metadata under `activeModules`.
- Tenant setup creates a primary active tenant indicator.
- The setup user is assigned tenant owner, tenant admin, and tenant user roles for the new tenant.
- The home page can show **Create Workspace** when onboarding is enabled.
- The home page can show dashboard shortcuts for active modules with known routes after tenant context resolves.
- Malformed `activeModules` metadata does not crash the home page.

## 13. Future Iteration Opportunities

- Preserve `/setup` as the post-login return URL for users who start setup before authentication.
- Enter or switch to the newly created tenant context immediately after successful setup.
- Generate app id, slug, hostname, or subdomain suggestions from tenant name and user profile context.
- Validate indicator format and uniqueness before final submit.
- Recommend and preselect modules from tenant type, email domain, indicator, or onboarding answers.
- Add module dependency handling so required supporting modules are selected automatically or explained inline.
- Add transactionality or compensating cleanup across owner, tenant, indicator, metadata, and role-assignment writes.
- Add setup telemetry for starts, step transitions, validation failures, successful completions, and service failures.
