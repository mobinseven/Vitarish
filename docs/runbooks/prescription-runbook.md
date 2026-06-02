# Prescription Module — Operational Runbook

## Overview

This runbook documents incident modes, resolution paths, and support procedures for the Prescription module's integration with insurance provider APIs (Tamin, DITAS, Salamat).

---

## 1. Incident Modes

### 1.1 Provider Outage (Complete)

**Symptoms:**
- Dashboard shows >80% failure rate for a provider in the last hour
- `ProviderOutage` alert fires
- Users see "Provider Unavailable" errors

**Diagnosis:**
1. Check Operational Dashboard → Alerts tab for `ProviderOutage` alerts
2. Check Transactions tab for the affected provider's success rate
3. Verify provider status via their external status page (if available)

**Resolution:**
1. **Immediate:** The system automatically transitions prescriptions to `ProviderUnavailable` state
2. **Short-term:** Advise doctors to use Paper Fallback workflow if prescriptions are urgent
3. **Recovery:** Once provider is back, the retry processor will automatically resolve `SyncPending` prescriptions
4. **Post-recovery:** Use Reconciliation Desk to resolve any `DuplicateRisk` or `SyncFailed` prescriptions

---

### 1.2 Provider Outage (Degraded)

**Symptoms:**
- Elevated timeout count but not full outage
- Latency significantly above normal
- Intermittent failures

**Diagnosis:**
1. Check Transactions tab for timeout counts and latency metrics
2. Check Retry Queue tab for growing queue depth

**Resolution:**
1. Monitor closely — the resilience layer handles retries with exponential backoff
2. If queue depth exceeds 50 entries, consider pausing submissions for that provider
3. Alert provider support team if degradation persists >30 minutes

---

### 1.3 Duplicate Risk Escalation

**Symptoms:**
- `DuplicateRiskPending` alert fires
- Prescriptions stuck in `DuplicateRisk` state
- Retry processor escalated stale `SyncPending` records

**Diagnosis:**
1. Check Duplicate Risk tab in Operational Dashboard for pending incidents
2. Review specific prescriptions in Reconciliation Desk
3. Check if provider has a record of the prescription (using Refresh Status action)

**Resolution:**
1. **Verify with provider:** Use "Refresh Status" on each affected prescription
2. **If provider has accepted:** Resolve as "Accepted" — system transitions to `AcceptedByProvider`
3. **If provider has no record:** Resolve as "Not Found" — safe to resubmit
4. **If inconclusive:** Use "Manual Reconcile" with documentation of investigation

---

### 1.4 Credential Expiration

**Symptoms:**
- `CredentialExpiration` or `CredentialExpired` alerts fire
- Authentication failures for specific doctors/providers
- `Authentication` error category in rejection distribution

**Diagnosis:**
1. Check Credentials tab for expiring/expired credentials
2. Identify affected doctors and clinics

**Resolution:**
1. **Proactive:** Notify clinic admins 7 days before expiration (alert threshold)
2. **Reactive:** Guide clinic admin through credential renewal process
3. **Emergency:** If a provider session can be refreshed, use the provider admin tools
4. **Verification:** After renewal, test with an eligibility check before prescribing


---

### 1.5 Retry Queue Depth Critical

**Symptoms:**
- `RetryQueueDepth` alert fires (Warning at 10, Critical at 50)
- Growing backlog of pending outbox entries
- Possible cascading failure

**Diagnosis:**
1. Check Retry Queue tab for entry details and error patterns
2. Determine if failures are for one provider or all
3. Check if retry processor is running (Hangfire dashboard)

**Resolution:**
1. **Provider-specific:** If all failures are one provider, likely an outage (see 1.1)
2. **System-wide:** Check Hangfire job status, ensure `prescription:sync-pending:retry` job is scheduled
3. **Manual intervention:** If needed, trigger retry job manually from Hangfire dashboard
4. **Cleanup:** After root cause is resolved, monitor queue drain rate

---

## 2. Support Procedures

### 2.1 Reconciliation Desk Operations

**Available Actions:**
- **Refresh Status:** Queries provider for current prescription status (safe, read-only)
- **Resolve Duplicate Risk:** Marks a `DuplicateRisk` prescription as accepted or to retry
- **Manual Reconcile:** Marks a prescription as manually resolved (requires notes)
- **Reconcile Paper Fallback:** Links a paper prescription back to the electronic system

**Guidelines:**
- Always use "Refresh Status" first before any manual action
- Document all manual reconciliations with clear notes
- Escalate if more than 5 prescriptions for same provider are in error state

### 2.2 Paper Fallback Procedures

**When to use:**
- Provider confirmed offline with no ETA
- Critical prescription that cannot wait
- Doctor explicitly requests paper fallback

**Process:**
1. Doctor initiates Paper Fallback from prescription detail page
2. System records reason and generates paper tracking number
3. After provider recovery, support reconciles via Reconciliation Desk
4. All paper fallbacks are audited

### 2.3 Credential Management

**Setup flow:**
1. Clinic admin maps doctor to provider credentials
2. System validates credentials with a test authentication call
3. Credential status transitions: Pending → Active
4. Monitor for expiration via Credentials tab

**Renewal flow:**
1. Identify expiring credentials from alerts or Credentials tab
2. Clinic admin provides new credentials
3. System validates and updates credential reference
4. Old token/session is invalidated

---

## 3. Monitoring Checklist

### Daily Checks
- [ ] Review active alerts in Operational Dashboard
- [ ] Check retry queue depth (should be < 10 in steady state)
- [ ] Verify no credentials expiring within 3 days

### Weekly Checks
- [ ] Review rejection distribution by provider for trends
- [ ] Check reference data freshness for all providers
- [ ] Review and resolve any long-standing `DuplicateRisk` incidents
- [ ] Verify all scheduled jobs are running (Hangfire dashboard)

### Monthly Checks
- [ ] Review transaction metrics for capacity planning
- [ ] Audit all manual reconciliations performed
- [ ] Review credential lifecycle — upcoming expirations
- [ ] Verify reference data crosswalk integrity

---

## 4. Background Jobs

| Job ID | Schedule | Purpose |
|--------|----------|---------|
| `prescription:sync-pending:retry` | Every 5 minutes | Retries SyncPending prescriptions with fetch-before-retry pattern |

### Job Failure Recovery

If a recurring job stops firing:
1. Check Hangfire dashboard for job status
2. Verify the job exists in recurring jobs list
3. If missing, restart the application (jobs are re-registered on startup)
4. If consistently failing, check application logs for the specific processor

---

## 5. Escalation Matrix

| Severity | Response Time | Escalation Path |
|----------|--------------|-----------------|
| Critical (outage, expired creds blocking all) | 15 minutes | Support → On-call engineer → Team lead |
| Warning (degraded, queue growing) | 1 hour | Support → On-call engineer |
| Info (single failures, minor issues) | Next business day | Support ticket |

---

## 6. Contact Points

- **Provider Support (Tamin):** Coordinate via official channels for API issues
- **Provider Support (DITAS):** API-key-based; check clinic-specific keys
- **Provider Support (Salamat):** Ministry of Health channels (when active)
- **Internal:** Development team for adapter/infrastructure issues
