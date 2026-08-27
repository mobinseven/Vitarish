# Tenant Indicator Management Journey

## 1. Context

- App: Platform-wide
- Module: Platform, Parties
- Capability: Tenant indicator CRUD, DNS verification, hostname resolution
- Related apps: All apps (tenant resolution depends on indicators)
- Related modules: Parties (indicator entities), Platform (admin UI)
- Product / service: Vitarish tenant resolution system
- Journey type: Current state with gaps
- Scenario: Platform admin creates, edits, and manages tenant indicators that route traffic to the correct tenant.
- User goal: Ensure each tenant has valid, active indicators for reliable tenant resolution.
- Business goal: Maintain accurate tenant routing; prevent misrouting; support multi-tenant hostname/subdomain resolution.
- Scope start: Platform admin opens tenant admin page.
- Scope end: Indicators are created, verified, and active for the target tenant.
- Primary persona: Platform administrator
- Evidence sources: Static code review of TenantIndicatorsEditor, TenantIndicatorEditDialog, tenant resolution logic
- Confidence level: Medium

## 2. Persona Snapshot

**Name / segment:** Platform administrator

**Situation:** Managing tenant routing infrastructure across multiple tenants.

**Motivation:** Ensure tenants are reliably reachable via their configured hostnames or subdomains.

**Key needs:** Create indicators, verify DNS, activate/deactivate indicators, manage primary indicator selection.

**Constraints:** DNS verification is manual; no propagation status; no bulk indicator operations.

**Success means:** All tenants are reliably reachable via their configured indicators with no misrouting.

## 3. Journey Summary

**Narrative:** The platform admin opens a tenant's admin page, navigates to the indicators section, creates or edits indicators with hostname or subdomain values, verifies DNS configuration, and activates indicators. The runtime `TenantResolver` uses these indicators to route incoming requests to the correct tenant.

**Emotional arc:** Start: methodical (navigating to indicators) → Middle: careful (verifying DNS) → End: confident (indicator active)

**Biggest friction point:** DNS verification is manual and there's no propagation status feedback.

**Highest-value opportunity:** Automated DNS verification with propagation status and health monitoring.

## 4. Journey Stages

### Stage 1: Access Tenant Indicators

**User goal:** Open the indicator management interface for a specific tenant.

**User actions:** Navigate to platform admin → select owner → select tenant → view indicators section.

**Touchpoints / channels:** `PlatformAdminTenantPage`, `TenantIndicatorsEditor`.

**User thoughts / questions:** "Where do I manage indicators for this tenant?"

**Emotions:** Methodical, 3, because the navigation path is known.

**Pain points:** Must navigate the full owner → tenant hierarchy to reach indicators; no direct tenant search from indicator management.

**Needs:** Direct tenant search; indicator-centric view across tenants.

**Opportunities:** Add indicator-centric admin view; direct tenant lookup by indicator value.

**Success metrics:** Time to reach indicator management; navigation steps required.

**Evidence:** Static code review assumption.

### Stage 2: Create Indicator

**User goal:** Add a new hostname or subdomain indicator for the tenant.

**User actions:** Click "Add Indicator"; enter indicator method (Hostname/Subdomain); enter address value; set primary flag; save.

**Touchpoints / channels:** `TenantIndicatorEditDialog`, `TenantIndicatorsEditor`.

**User thoughts / questions:** "Is this address valid? Is it already used by another tenant? Is the DNS configured?"

**Emotions:** Careful, 4, because routing errors affect all tenant users.

**Pain points:** No address format validation; no uniqueness check before save; no DNS configuration guidance; no preview of how the indicator will be used in routing.

**Needs:** Format validation, uniqueness check, DNS guidance, routing preview.

**Opportunities:** Add address format validation, cross-tenant uniqueness check, DNS setup instructions.

**Success metrics:** Indicator creation success rate; misrouting incidents after creation.

**Evidence:** Static code review assumption.

### Stage 3: Edit Indicator

**User goal:** Modify an existing indicator's address, method, or primary status.

**User actions:** Click edit on an indicator; modify fields; save changes.

**Touchpoints / channels:** `TenantIndicatorEditDialog`.

**User thoughts / questions:** "Will changing this address break existing access? Is DNS propagated for the new address?"

**Emotions:** Anxious, 2, because changes affect live routing.

**Pain points:** No warning about live impact of address changes; no DNS propagation check; no rollback mechanism.

**Needs:** Impact warning, propagation check, rollback capability.

**Opportunities:** Add change-impact preview, DNS propagation status, and rollback.

**Success metrics:** Routing incidents after indicator changes; rollback frequency.

**Evidence:** Static code review assumption.

### Stage 4: Activate/Deactivate Indicator

**User goal:** Enable or disable an indicator without deleting it.

**User actions:** Toggle indicator active status; observe status change.

**Touchpoints / channels:** `TenantIndicatorsEditor`.

**User thoughts / questions:** "Is this indicator now live? Will traffic route through it immediately?"

**Emotions:** Confident, 4, if the toggle is clear.

**Pain points:** No confirmation before activation/deactivation; no propagation delay awareness; no status indicator showing current routing state.

**Needs:** Confirmation before status change, propagation status, routing state visibility.

**Opportunities:** Add activation confirmation, propagation delay indicator, routing state display.

**Success metrics:** Accidental activation/deactivation rate; time for routing to update after status change.

**Evidence:** Static code review assumption.

### Stage 5: Verify DNS Configuration

**User goal:** Confirm that DNS records point to the Vitarish platform for the indicator address.

**User actions:** Manually check DNS records; configure DNS if needed; return to verify.

**Touchpoints / channels:** External DNS management tools; `TenantIndicatorEditDialog` (no built-in verification).

**User thoughts / questions:** "Is my DNS configured correctly? Has it propagated?"

**Emotions:** Frustrated, 2, because verification is manual and external.

**Needs:** Built-in DNS verification; propagation status.

**Opportunities:** Add DNS lookup and verification within the indicator editor.

**Success metrics:** DNS verification time; misrouting incidents due to DNS misconfiguration.

**Evidence:** Static code review assumption.

### Stage 6: Runtime Tenant Resolution

**User goal:** Understand how indicators are used at runtime.

**User actions:** (System-level) incoming request → `TenantResolver` → hostname normalization → indicator lookup → tenant resolution → cache.

**Touchpoints / channels:** `TenantResolver` (infrastructure, not user-facing).

**User thoughts / questions:** "How does the system know which tenant to route to?"

**Emotions:** N/A (system behavior).

**Pain points:** No admin visibility into resolution cache; no cache invalidation controls; no resolution failure logging.

**Needs:** Resolution monitoring, cache management, failure logging.

**Opportunities:** Add resolution monitoring dashboard, cache invalidation, failure alerting.

**Success metrics:** Resolution success rate; cache hit rate; average resolution time.

**Evidence:** Static code review assumption.

## 5. Cross-Journey Insights

### Key pain points

1. DNS verification is completely manual — occurs at indicator creation and edit.
2. No propagation status feedback — occurs after any indicator change.
3. No resolution monitoring — occurs at runtime.

### Moments that matter

- **Moment of truth:** First request routed through a new indicator — confirms DNS and routing work.
- **Moment of friction:** Manually checking DNS records across multiple tools.
- **Moment of delight:** Built-in DNS verification confirms everything is correct.

### Design principles implied by the journey

- Keep DNS verification inside the indicator management flow.
- Show propagation status after every indicator change.
- Monitor resolution health proactively.

## 6. Implementation Derivation

### Required artifacts

| Artifact | Why it is needed | Journey source | Priority |
|---|---|---|---|
| DNS verification | Confirms DNS records are correctly configured | Stage 3, 5 | High |
| Propagation status indicator | Shows whether DNS changes have taken effect | Stage 4 | High |
| Resolution monitoring | Provides visibility into runtime tenant resolution | Stage 6 | Medium |
| Indicator uniqueness check | Prevents two tenants from claiming the same address | Stage 2 | High |

### Feature slices

| Slice | User value | Includes | Excludes | Acceptance signal |
|---|---|---|---|---|
| DNS verification | Know DNS is configured correctly | DNS lookup in indicator editor, pass/fail display | Auto-DNS-configuration | DNS verification time decreases |
| Propagation status | Know when changes are live | TTL-based propagation estimate, status badge | Real-time DNS monitoring | Misrouting incidents decrease |
| Indicator uniqueness | Prevent routing conflicts | Cross-tenant uniqueness check on save | Bulk uniqueness audit | Duplicate indicator incidents decrease |

### Domain / data implications

- Add DNS verification result to indicator entity.
- Add propagation status tracking (last-checked, TTL estimate).
- Add cross-tenant uniqueness constraint on indicator address.

### UI implications

- Add DNS verification button and status to indicator editor.
- Add propagation status badge to indicator list.
- Add uniqueness validation before indicator save.

### API / service implications

- Add DNS lookup service (resolve A/CNAME records).
- Add cross-tenant indicator uniqueness check.
- Add resolution cache monitoring endpoint.

### Validation, permissions, and safety

- Only platform admins can manage indicators.
- Indicator address must be unique across all tenants.
- Deactivation should warn about routing impact.

### Tests and acceptance criteria

- DNS verification correctly identifies configured vs. unconfigured addresses.
- Propagation status reflects actual DNS state.
- Duplicate indicator addresses are rejected.
- Deactivated indicators are excluded from resolution.

### Metrics

- DNS verification success rate.
- Misrouting incidents per month.
- Time to propagate indicator changes.

## 7. Technical Detail Addendum

### Current implementation touchpoints

- `src/Hosts/Web/apps/platform-admin/src/app/tenants/[tenantId]/page.tsx` — tenant administration surface.
- `src/Hosts/Web/apps/platform-admin/src/app/tenants/[tenantId]/indicators/[indicatorId]/edit/page.tsx` — edits a tenant indicator value.
- `src/Hosts/Web/apps/platform-admin/src/app/tenants/[tenantId]/edit/page.tsx` — edits tenant details.
- Runtime tenant resolution uses `TenantResolver` which normalizes hostnames via `HostnameNormalizer.Canonical()`, queries `TenantIndicators` where `Status == Active` and `Value == normalized`, and caches results for 10 minutes.
- `src/Hosts/Main/Pages/Common/Home.razor` — home page reads `activeApps` from tenant metadata after resolution; uses the resolved tenant context.

### Issues to address

- `TenantIndicatorEditDialog` accepts any address value without format validation (no check for valid hostname/subdomain format).
- No cross-tenant uniqueness check; two tenants could claim the same hostname, causing resolution ambiguity.
- DNS verification is entirely manual; the editor provides no built-in DNS lookup or verification.
- No propagation status after indicator changes; the 10-minute resolution cache may serve stale data.
- `TenantIndicatorsEditor` has no bulk operations for creating or deactivating multiple indicators.
- The primary indicator flag has no validation; multiple indicators could be marked primary.
- No resolution failure logging or monitoring; if DNS is misconfigured, the admin has no visibility.
- `TenantResolver` caching (10-minute TTL) means changes may take up to 10 minutes to propagate; this delay is not communicated to admins.
