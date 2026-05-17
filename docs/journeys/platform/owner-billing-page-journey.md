# Owner Billing Page Journey Map

## 1. Context

- Product / service: Vitarish platform owner experience
- Journey type: Future-state billing review and management journey
- Entry point: Owner dashboard billing card, tenant overview billing card, or action inbox billing item
- Scenario: An owner needs to view billing status, plan, payment/invoice information, and start approved billing actions for a tenant.
- User goal: Understand whether the tenant is paid/current and access billing tasks safely.
- Business goal: Reduce billing support load while keeping payment and invoice data accurate and secure.
- Scope start: Authorized owner opens billing for a tenant.
- Scope end: User reviews billing information, opens an external/internal billing action, or sees unavailable state.
- Primary persona: Organization owner responsible for payment
- Secondary personas: Billing admin, tenant admin if allowed, platform support/admin
- Evidence sources: Owner dashboard journey map
- Confidence level: Low because the billing system, plan model, and invoice source are open questions.

## 2. Current Implementation Snapshot

| Area | Current behavior | Billing implication |
| --- | --- | --- |
| Dashboard | Billing is listed as a planned placeholder. | Do not display fake plan, invoice, payment, or status data. |
| Open questions | Dashboard asks what billing system, plan model, and invoice source should connect. | Billing journey must remain source-agnostic until a provider/contract is chosen. |
| Tenant context | Dashboard is tenant-scoped. | Billing should be scoped to tenant or owner account according to future billing model. |
| Permissions | Owner/Admin/Member labels are planned. | Billing visibility/actions need explicit role policy. |
| Audit | Billing changes are candidate sensitive audit events. | Payment/plan changes should be audit-ready. |

## 3. Persona Snapshot

**Name / segment:** Billing owner

**Situation:** Needs confidence that the workspace subscription, invoices, or payment status is correct.

**Motivation:** Avoid service interruption and keep finance records accessible.

**Key needs:** Clear current status, invoices/receipts, plan details, payment method action, and support path.

**Constraints:** Billing data may come from an external provider and may be owner-level rather than tenant-level.

**Success means:** The owner can view trustworthy billing state and complete or start a billing action without exposing payment data incorrectly.

## 4. Journey Summary

**Narrative:** The owner opens billing and sees an honest source-backed page. Before integration, the page shows a pending placeholder. Once connected, it displays plan/status, invoice list, payment method summary, and permitted actions such as manage payment method or change plan. Sensitive mutations route through provider-hosted or dedicated flows and write audit records where required.

**Emotional arc:** Anxious -> informed -> secure

**Biggest current friction point:** Billing is requested by the dashboard but no billing model/source is defined.

**Highest-value opportunity:** Define a billing application contract before showing any billing facts.

## 5. Billing Information Architecture

| Region | Content | Source/stance |
| --- | --- | --- |
| Billing scope header | Tenant/account being billed and viewer role | Required before showing data. |
| Status summary | Current billing status, plan, renewal/due info | Source-backed only. |
| Invoices/receipts | Invoice rows with date, amount, status, download/open action | Future billing provider/query. |
| Payment method | Brand/last4 or safe summary if applicable | Never collect raw card data in this page unless approved. |
| Billing actions | Manage payment method, change plan, contact support | Permission-gated and provider-aware. |
| Placeholder state | Billing not connected yet | Honest fallback; no fake data. |

## 6. Happy Path

1. Owner opens billing for a tenant.
2. System validates tenant/account context and billing-view permission.
3. System queries the approved billing application contract.
4. Page shows tenant/account scope, status, plan, payment summary, and invoice list from the billing source.
5. Owner selects an available billing action.
6. System validates billing-action permission.
7. System opens the approved provider-hosted or internal billing flow.
8. On return, page refreshes billing state from the source.
9. Sensitive billing changes emit audit events if policy requires.

## 7. Alternate and Failure Paths

| Situation | User risk | System behavior |
| --- | --- | --- |
| Billing source not configured | Fake billing status could mislead. | Show pending/unavailable state and support contact. |
| User lacks billing permission | Payment data exposure. | Hide details or show permission state. |
| Billing query fails | User cannot know status. | Show retry; do not show cached all-clear unless freshness is explicit. |
| Provider action fails/cancelled | User may assume payment updated. | Show cancelled/failed state and refresh source. |
| Tenant/account billing mismatch | Wrong account could be charged. | Show billing scope and require provider-side/customer id validation. |
| Invoice unavailable | User needs records. | Show row-level unavailable state and support path. |

## 8. Data and State Model Recommendation

| Field | Source today or future source | Notes |
| --- | --- | --- |
| Billing account/customer id | Future billing contract | Decide tenant-level vs owner-level billing. |
| Plan/status | Future billing provider/query | Source-backed only. |
| Invoices | Future billing provider/query | Avoid storing payment-sensitive data unnecessarily. |
| Payment method summary | Future billing provider/query | Safe summary only; no raw payment details. |
| Permissions | Billing application policy | Separate view/manage permissions. |
| Audit events | Future audit service | For plan/payment changes if required. |

## 9. UX and Guardrail Rules

1. Never show fake billing status, plans, invoices, amounts, or payment methods.
2. Billing scope must be clear before displaying data.
3. Separate view billing from manage billing permissions.
4. Prefer provider-hosted payment flows for sensitive payment data.
5. Refresh billing source after external actions.
6. Billing errors must not imply paid/current status.
7. Audit sensitive billing changes when policy requires.

## 10. Acceptance Criteria

- Billing page validates tenant/account scope and billing permission.
- Before integration, billing shows a clear unavailable/pending state only.
- Connected billing data comes from a defined billing application contract.
- View and manage actions are permission-gated separately.
- External billing actions refresh the page from the source on return.
- No raw payment details are stored or displayed outside approved provider flows.
