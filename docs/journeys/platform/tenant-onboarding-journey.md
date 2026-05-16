# Tenant Onboarding Journey Map

## 1. Context

- Product / service: Vitarish platform onboarding
- Journey type: Future-state, low-friction target flow
- Scenario: A new user registers, logs in, creates a tenant, becomes tenant owner automatically, defines the tenant's domain/sub-domain indicators, and activates modules.
- User goal: Start using a configured tenant as quickly as possible.
- Business goal: Convert a registered user into an active tenant owner with the right modules enabled.
- Scope start: Anonymous user opens registration.
- Scope end: User lands in the tenant workspace with selected modules active.
- Primary persona: Organization owner / first administrator
- Friction principle: Minimize clicks by combining setup into one guided path, using defaults, and asking only for data required to create a usable tenant.

## 2. Persona Snapshot

**Name / segment:** First-time organization owner

**Situation:** Wants to evaluate or start using Vitarish for an organization, practice, team, or business unit.

**Motivation:** Create a secure workspace and activate only the modules needed now.

**Key needs:** Fast registration, clear ownership, trustworthy tenant identity, and obvious module selection.

**Constraints:** The user may not understand internal platform terms such as tenant, owner, domain, or sub-domain.

**Success means:** The user reaches an initialized tenant without support, duplicate setup, or unnecessary decisions.

## 3. Journey Summary

**Narrative:** The user registers once, is signed in automatically when possible, completes a short tenant setup form, reviews recommended modules, and enters the tenant dashboard as owner.

**Emotional arc:** Curious -> reassured -> in control -> ready to work

**Biggest friction point:** Splitting account creation, login, tenant creation, indicator setup, and module activation across separate screens.

**Highest-value opportunity:** Use a single post-registration setup wizard that creates the tenant, owner membership, indicators, and module activations in one submit action.

**Target click budget:**

| Flow segment | Target user clicks | Friction control |
| --- | ---: | --- |
| Register | 1 primary submit | Auto-login after successful email/password or identity-provider registration. |
| Create tenant + indicators | 1 primary continue | One form with sensible defaults and inline validation. |
| Select modules | 1 primary finish | Preselect recommended modules and allow later changes. |
| Enter workspace | 0 extra clicks | Redirect automatically to the new tenant dashboard. |

## 4. Recommended Low-Friction Flow

### Happy path

1. User opens **Register**.
2. User enters only required account fields, accepts required terms, and submits.
3. System creates the account and starts an authenticated session immediately.
4. System detects that the user has no active tenant and redirects to **Create your workspace**.
5. User enters tenant name.
6. System generates a tenant slug/sub-domain suggestion from the tenant name.
7. User selects or confirms the primary domain indicator and optional sub-domain indicator from searchable lists or suggested chips.
8. User continues to module activation.
9. System shows module cards grouped by business outcome, with recommended modules preselected from the indicators.
10. User confirms selected modules.
11. System creates the tenant, assigns the user as owner, stores indicators, activates modules, and initializes required module data.
12. User is redirected to the tenant dashboard with an owner onboarding checklist.

### Minimum viable screen model

| Screen | Required inputs | Defaults / automation | Primary CTA | Notes |
| --- | --- | --- | --- | --- |
| Register | Name, email, password or identity provider | Auto-login after success | Create account | Do not ask for tenant data here unless registration is explicitly organization-first. |
| Create your workspace | Tenant name, domain indicator, optional sub-domain indicator | Suggested slug/sub-domain from tenant name; suggested indicators from email/domain or previous choice | Continue | Inline availability checks prevent late failures. |
| Activate modules | Module selection | Recommended modules preselected by indicators; dependencies auto-selected | Finish setup | Allow skip only if a usable default module set exists. |
| Tenant dashboard | None | Owner role, active tenant context, active modules | None | Show next-best actions, not another required setup step. |

## 5. Journey Stages

### Stage 1: Register

**User goal:** Create an account quickly and safely.

**User actions:** Open registration, enter required credentials or choose identity provider, submit.

**Touchpoints / channels:** Registration page, identity provider, email verification if required by policy.

**User thoughts / questions:** “Can I get in without a long setup process?”

**Emotions:** Hopeful, 4, because the task feels familiar when the form is short.

**Pain points:** Extra profile fields, separate login after registration, or requiring email verification before the user sees setup context.

**Needs:** Short form, clear password rules, secure validation, and immediate continuation.

**Opportunities:** Auto-login after registration; defer nonessential profile data; support identity-provider registration.

**Success metrics:** Registration completion rate, time to authenticated session, registration validation error rate.

### Stage 2: Login / authenticated continuation

**User goal:** Continue without repeating work.

**User actions:** If auto-login is unavailable, enter credentials and submit.

**Touchpoints / channels:** Login page, session redirect, tenant resolver.

**User thoughts / questions:** “Why am I logging in again?”

**Emotions:** Neutral, 3, if login is required; reassured, 4, if auto-login works.

**Pain points:** Losing context after registration or landing on an empty page with no tenant.

**Needs:** Preserve intent, return path, and setup state.

**Opportunities:** Treat login as an exception path; redirect authenticated users without tenants directly to tenant setup.

**Success metrics:** Login success rate after registration, setup redirect success rate, abandoned no-tenant sessions.

### Stage 3: Create tenant and ownership

**User goal:** Create a workspace and know they control it.

**User actions:** Enter tenant name, confirm slug/sub-domain, continue.

**Touchpoints / channels:** Create workspace form, slug availability check, owner-role confirmation copy.

**User thoughts / questions:** “What will this workspace be called, and who can administer it?”

**Emotions:** In control, 4, when ownership is explicit and automatic.

**Pain points:** Asking the user to manually assign themselves as owner, choose technical identifiers, or understand tenant internals.

**Needs:** Plain-language labels, automatic owner assignment, recoverable validation errors.

**Opportunities:** Create tenant and owner membership in the same transaction; explain “You will be the owner” near the submit action.

**Success metrics:** Tenant creation completion rate, slug collision rate, tenant creation rollback rate.

### Stage 4: Define domain/sub-domain indicators

**User goal:** Classify the tenant enough for correct defaults and recommendations.

**User actions:** Choose a domain indicator and optional sub-domain indicator; accept suggestions when accurate.

**Touchpoints / channels:** Indicator chips, searchable selector, helper text, recommendation engine.

**User thoughts / questions:** “Which category best describes my organization?”

**Emotions:** Decisive, 3, if options are clear; uncertain, 2, if taxonomy is too detailed.

**Pain points:** Too many categories, internal naming, or requiring sub-domain decisions too early.

**Needs:** A short list of common domains, search, examples, and a safe “not sure” option when sub-domain is optional.

**Opportunities:** Suggest indicators from email domain, organization name, invitation context, or market defaults; let owners edit indicators later.

**Success metrics:** Indicator selection time, “not sure” rate, later indicator correction rate.

### Stage 5: Activate modules

**User goal:** Enable the capabilities needed now without understanding the full module catalog.

**User actions:** Review recommended modules, optionally add/remove modules, finish setup.

**Touchpoints / channels:** Module selection cards, dependency notices, module recommendations.

**User thoughts / questions:** “Which modules do I need to start?”

**Emotions:** Confident, 4, when recommended modules are preselected.

**Pain points:** Showing every module equally, forcing technical dependency choices, or requiring configuration before the user can enter the workspace.

**Needs:** Recommended bundle, clear module benefits, dependency automation, and ability to change later.

**Opportunities:** Auto-select modules based on indicators; hide advanced modules under “more options”; initialize module defaults in the background.

**Success metrics:** Module activation completion rate, modules changed after first session, setup finish failures.

### Stage 6: Enter tenant workspace

**User goal:** Start working immediately.

**User actions:** Land in dashboard; optionally follow checklist.

**Touchpoints / channels:** Tenant dashboard, owner onboarding checklist, module navigation.

**User thoughts / questions:** “What should I do first?”

**Emotions:** Ready, 4, if the workspace looks complete.

**Pain points:** Another required setup screen, empty navigation, or unclear active tenant context.

**Needs:** Active tenant indicator, owner status, visible modules, and next-best actions.

**Opportunities:** Show a dismissible checklist for optional profile, team, branding, and module-specific setup tasks.

**Success metrics:** First workspace action rate, time to first module use, checklist engagement.

## 6. Service Blueprint

| User-facing step | Frontstage behavior | Backstage action | Failure handling |
| --- | --- | --- | --- |
| Register | Validate account form inline and submit once. | Create user and authenticated session. | Keep entered values, show field-level errors, and avoid restarting setup. |
| Login / continue | Redirect authenticated user to setup if no tenant exists. | Resolve active user and tenant membership. | If session fails, return to login with preserved return URL. |
| Tenant setup | Show one form for name, slug, and indicators. | Validate tenant name, slug uniqueness, and indicator IDs. | Show recoverable inline errors before any module choices are lost. |
| Finish setup | Confirm selected modules once. | In one orchestrated setup operation: create tenant, create owner membership, persist indicators, activate modules, initialize module defaults. | Use an all-or-nothing transaction where possible; otherwise show retry with setup draft retained. |
| Dashboard | Redirect into the new tenant context. | Set active tenant context and load activated module navigation. | If module initialization is delayed, show “setting up” status while core tenant remains available. |

## 7. Friction Reduction Rules

1. **One source of truth:** Keep tenant setup state in one draft model until final submission.
2. **Do not ask twice:** Registration captures user identity; tenant setup captures workspace identity; module setup captures capabilities.
3. **Make ownership automatic:** The creator becomes owner without a separate role assignment screen.
4. **Recommend before asking:** Use indicators to preselect modules and explain recommendations.
5. **Validate early:** Check required fields, slug availability, indicator validity, and module dependencies before final submission.
6. **Defer optional work:** Branding, invitations, billing, advanced module configuration, and profile enrichment happen after dashboard entry.
7. **Keep recovery simple:** On errors, keep the user's setup draft and show one clear retry path.

## 8. Click and Decision Inventory

| Step | User decision | Required? | Can be defaulted? | Recommendation |
| --- | --- | --- | --- | --- |
| Account credentials | Who am I? | Yes | Partly, through identity provider | Keep on registration only. |
| Tenant name | What is my workspace called? | Yes | No | Single text input. |
| Slug/sub-domain | Where is my tenant reached? | Yes, if sub-domain routing is used | Yes | Auto-generate and allow edit. |
| Domain indicator | What type of tenant is this? | Yes | Sometimes | Suggest from context and allow search. |
| Sub-domain indicator | What specialty/category applies? | No unless required by domain | Yes | Make optional or recommended. |
| Modules | What capabilities start active? | Yes | Yes | Preselect recommended bundle. |
| Owner assignment | Who owns this tenant? | Yes | Fully | Automatic; no user decision. |

## 9. Acceptance Criteria

- A newly registered user can complete setup without manually logging in when security policy allows it.
- A user without a tenant is routed directly to tenant setup after authentication.
- Tenant creation automatically assigns the creator as owner.
- Domain and sub-domain indicators are collected in the tenant setup flow, not in separate admin pages.
- Module recommendations are derived from indicators and can be accepted with one primary action.
- Module dependencies are resolved automatically or explained inline before setup completion.
- Setup can complete in three primary clicks after landing on registration: create account, continue workspace setup, finish setup.
- Optional setup tasks are deferred to the tenant dashboard checklist.
