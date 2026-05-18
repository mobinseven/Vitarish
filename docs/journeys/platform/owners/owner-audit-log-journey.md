# Owner Audit Log Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state tenant audit review journey
- Entry point: Owner dashboard audit card, tenant overview audit card, or post-action confirmation links
- Scenario: An owner/admin needs to review sensitive tenant activity such as access changes, settings changes, billing changes, and archive/delete attempts.
- User goal: Understand what changed, who changed it, when it happened, and whether follow-up is needed.
- Business goal: Increase trust, support compliance needs, and make sensitive self-service actions accountable.
- Scope start: Authorized user opens audit log for a tenant.
- Scope end: User finds activity, exports/reviews details if supported, or starts a follow-up journey.
- Primary persona: Organization owner
- Secondary personas: Tenant admin, platform support/admin, auditor/compliance viewer if later approved
- Evidence sources: Owner dashboard journey and current audit-library presence in repository structure
- Confidence level: Medium for audit need; low for concrete event schema because owner audit query is not implemented.

## 2. Current Implementation Snapshot

| Area | Current behavior | Audit implication |
| --- | --- | --- |
| Dashboard placeholders | Audit summary is identified as future data. | Do not show fake audit rows or counts before an audit query exists. |
| Sensitive actions | Dashboard calls out invites/removals, role changes, settings, billing, and archive/delete as audit candidates. | These actions should emit audit events before audit log becomes useful. |
| Existing libraries | The repository includes reusable audit-related library structure. | Owner audit should use existing audit abstractions where applicable instead of inventing a parallel log. |
| Role model | Owner/admin/member labels derive from tenant roles. | Audit visibility should be permission-gated. |
| State-changing journeys | Access, settings, billing, and danger zone are dedicated future flows. | Audit log should be read-only and link back to relevant records/actions where safe. |

## 3. Persona Snapshot

**Name / segment:** Accountable owner

**Situation:** Wants to verify recent tenant changes or investigate something unexpected.

**Motivation:** Maintain trust, security, and accountability.

**Key needs:** Reliable event list, filtering, actor/action/target details, timestamps, and clear unavailable states.

**Constraints:** Audit data may be incomplete until all sensitive actions emit events.

**Success means:** The owner can answer “what happened, who did it, and when?” for tenant-sensitive activity.

## 4. Journey Summary

**Narrative:** The owner opens audit log, sees read-only activity scoped to the tenant, filters by action type or date, opens event details, and optionally follows a link to the affected member/settings/billing record. If audit data is not connected, the page honestly states that no audit source is available rather than displaying placeholders as facts.

**Emotional arc:** Suspicious/curious -> informed -> reassured or ready to follow up

**Biggest current friction point:** Sensitive owner actions are planned, but no owner-facing audit read model is defined.

**Highest-value opportunity:** Define one audit query surface before enabling high-risk self-service mutations.

## 5. Journey Stages

### Stage 1: Open tenant-scoped audit

**User goal:** Confirm audit is scoped to the right tenant.

**User actions:** Opens audit; checks tenant header and role.

**Touchpoints / channels:** Audit page header.

**Pain points:** Cross-tenant audit leakage would be severe.

**Needs:** Tenant identity, access validation, and clear scope label.

**Opportunities:** Reuse owner dashboard tenant header pattern.

**Success metrics:** Audit load success; access denied attempts; tenant-scope errors.

### Stage 2: Find relevant event

**User goal:** Locate the event or time range of interest.

**User actions:** Filters by action type, actor, target, or date.

**Touchpoints / channels:** Audit filter controls and event table.

**Pain points:** Raw logs can be noisy or technical.

**Needs:** Human-readable event categories and stable filtering.

**Opportunities:** Start with a small approved event taxonomy for owner actions.

**Success metrics:** Filter usage; zero-result states; detail opens.

### Stage 3: Review details

**User goal:** Understand the change and follow up.

**User actions:** Opens event details; reads before/after if available; follows safe links.

**Touchpoints / channels:** Audit detail drawer/page.

**Pain points:** Missing actor/target data can reduce trust.

**Needs:** Actor, timestamp, action, target, outcome, and correlation id when available.

**Opportunities:** Link to access/settings/billing journeys only when authorized.

**Success metrics:** Follow-up actions; event export/download if later supported; audit support tickets.

## 6. Happy Path

1. Owner opens audit log for a tenant.
2. System validates tenant context and audit-view permission.
3. System loads audit events from the approved audit query.
4. Page displays tenant identity, event list, and filters.
5. User filters by date range or event category.
6. System returns tenant-scoped matching events.
7. User opens an event detail.
8. Page shows actor, action, target, timestamp, outcome, and safe metadata.
9. User returns to list or opens a related journey if permitted.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| Audit source unavailable | Fake data could mislead. | Show unavailable state and no invented rows. |
| No events in range | User may think audit is broken. | Show empty state with filter reset and data-source explanation. |
| User lacks permission | Sensitive metadata exposure. | Deny or show limited state per policy. |
| Event references deleted target | Detail link may fail. | Show event facts and mark target unavailable. |
| Query times out | User cannot investigate. | Show retry and preserve filters. |
| Audit records incomplete | User may overtrust logs. | Show event fields as unknown when unavailable, not guessed. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Tenant id/name | Tenant context / `TenantReadDto` | Scope all audit queries. |
| Event list | Existing/future audit query abstraction | Avoid a separate owner-only audit store. |
| Event category | Approved owner action taxonomy | Examples: access, settings, billing, tenant lifecycle. |
| Actor/target labels | Future audit enrichment or user/tenant lookup | Handle missing/deleted entities safely. |
| Correlation id/outcome | Audit infrastructure | Useful for support/debug. |

## 9. UX and Guardrail Rules

1. Audit log is read-only.
2. Never show fake audit events.
3. Scope every query by tenant and permission.
4. Use human-readable event categories, not only internal class names.
5. Unknown actor/target fields must be displayed honestly.
6. Sensitive actions should emit audit events before production release.
7. Audit details may link to records only when the viewer is authorized.

## 10. Acceptance Criteria

- Audit page validates tenant and audit-view permission before querying events.
- Event list is tenant-scoped and read-only.
- Filters include at least date range and event category once events exist.
- Empty/unavailable states do not imply fake activity.
- Event detail displays actor, action, target, timestamp, outcome, and unknown fields safely.
- Sensitive owner journeys identify the audit event they will write before implementation.
