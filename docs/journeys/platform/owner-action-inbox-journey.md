# Owner Action Inbox Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state basic owner action inbox journey
- Entry point: Owner dashboard action inbox card or tenant overview action card
- Scenario: An owner wants a short list of tenant-level items that need attention, such as pending invites, billing issues, required setup, or failed sensitive operations.
- User goal: Know what requires action and start the correct follow-up journey.
- Business goal: Reduce missed administrative tasks and create a calm command center without building a complex notification system first.
- Scope start: User opens the owner dashboard or action inbox for a tenant.
- Scope end: User dismisses/reviews an item, starts the related journey, or sees an honest empty/unavailable state.
- Primary persona: Organization owner
- Secondary personas: Tenant admin, platform support/admin
- Evidence sources: Owner dashboard journey map
- Confidence level: Low-to-medium because the action service is explicitly future-state.

## 2. Current Implementation Snapshot

| Area | Current behavior | Action-inbox implication |
| --- | --- | --- |
| Dashboard placeholders | Action inbox is listed as future data. | First release should show placeholder/unavailable state unless a real query exists. |
| Dedicated journeys | Members, billing, settings, audit, and danger zone are planned. | Inbox items should deep-link to these journeys when item types are approved. |
| Tenant context | Owner dashboard depends on active tenant context. | Inbox must be tenant-scoped first; cross-tenant rollup can come later. |
| Billing/access/audit data | Not connected in dashboard. | Do not invent item counts or severity. |
| State changes | Dashboard should separate read-only queries from mutations. | Inbox should read items and route actions; mutations happen in owning journeys. |

## 3. Persona Snapshot

**Name / segment:** Busy owner

**Situation:** Opens Vitarish briefly and wants to know if anything needs attention.

**Motivation:** Resolve administrative tasks before they become support, security, or billing problems.

**Key needs:** Short prioritized list, clear source, due/created date when real, and direct next action.

**Constraints:** A fake inbox is worse than no inbox because it can create false confidence.

**Success means:** The owner can quickly see no real action is needed or open the right journey for a real item.

## 4. Journey Summary

**Narrative:** The dashboard shows a basic action inbox region. If no action service exists, it says the inbox is pending. Once connected, the inbox displays tenant-scoped items from approved sources, grouped by severity or type. The owner opens an item, reads why it matters, and follows a CTA to the owning journey. The inbox itself does not perform sensitive mutations.

**Emotional arc:** Alert -> oriented -> focused -> relieved

**Biggest current friction point:** Owner surfaces will have many future cards; without an inbox, owners may not know what matters first.

**Highest-value opportunity:** Start with a small, trustworthy read-only list of real items.

## 5. Candidate Inbox Item Types

| Item type | Source | Follow-up journey | Release stance |
| --- | --- | --- | --- |
| Pending invite issue | Future invitation service | Invite/remove users | Future, only when invites exist. |
| Role/access warning | Future access rules | Members and roles | Future, no fake warnings. |
| Billing issue | Future billing service | Billing page | Future, only with billing source. |
| Settings incomplete | Tenant settings/read model | Tenant settings | Future if rules are explicit. |
| Audit follow-up | Future audit/action rules | Audit log | Future if actionable criteria exist. |
| Archive/delete recovery | Future lifecycle service | Danger zone or support | Future, sensitive. |

## 6. Happy Path

1. User opens owner dashboard with a resolved tenant context.
2. System queries the action inbox read model for that tenant.
3. If the action service is connected, dashboard shows real items with type, reason, source, created/due date when available, and priority.
4. User opens an item.
5. System shows item details and the approved follow-up CTA.
6. User selects the CTA.
7. System opens the owning journey, where any state-changing action is validated and audited.
8. Inbox refreshes after the owning journey changes the source state.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| No action service exists | Placeholder may look broken. | Show explicit “action inbox pending” copy and no fake items. |
| No items | User may wonder if data loaded. | Show “No actions need attention” only when the source query succeeds. |
| Query fails | User may miss real tasks. | Show error/retry; do not show all-clear. |
| Item source is stale | User may act on resolved problem. | Revalidate item before opening follow-up or mark resolved. |
| User lacks permission for item | Sensitive information could leak. | Hide item or show limited copy according to permission policy. |
| Follow-up journey unavailable | Dead-end frustration. | Show pending journey copy and return to dashboard. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Tenant id | Tenant context | Inbox is tenant-scoped first. |
| Item id/type | Future owner action service | Stable id for refresh and details. |
| Priority/severity | Future action rules | Keep taxonomy small. |
| Source journey/link | Capability registry or item metadata | Route to owning journey. |
| Item state | Future source-specific rules | Avoid a separate mutable inbox state unless dismissal is approved. |

## 9. UX and Guardrail Rules

1. The inbox is read-only except for future explicit dismiss/acknowledge rules.
2. Do not show fake counts, fake overdue items, or fake all-clear states.
3. “No actions” is valid only after a successful query.
4. Every item must have an owning source and follow-up journey.
5. Permission filtering must happen before items are shown.
6. Keep the first release basic; no complex workflow engine.
7. State-changing resolution happens in the owning journey, not in the inbox card.

## 10. Acceptance Criteria

- Dashboard can show a clear pending/unavailable state before an action service exists.
- Connected inbox items are tenant-scoped, permission-filtered, and source-backed.
- Empty all-clear state appears only after successful query.
- Items open details with a follow-up CTA to the owning journey.
- Inbox does not perform sensitive mutations directly.
- Stale or resolved items are revalidated before follow-up.
