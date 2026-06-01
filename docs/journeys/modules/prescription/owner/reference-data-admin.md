# Journey: Reference Data Administration

## 1. Context
- **Persona:** Clinic Admin / Platform Admin
- **Trigger:** Scheduled reference data sync runs, manual refresh requested, or staleness alert triggered.
- **Goal:** Keep prescription code references current per provider without disrupting active workflows.

## Current Implementation Alignment
- **Status:** `partial`.
- **Implemented now:** `ReferenceDataAdminDashboard` shows provider/code-system freshness, sync history, breaking changes, and crosswalk grid; admins can trigger sync and deactivate crosswalks; prescription item search uses synced reference codes.
- **Current artifacts:** `ReferenceDataAdminDashboard`, `AddPrescriptionItemDialog`, `SyncReferenceDataCommand`, `GetReferenceFreshnessQuery`, `GetReferenceSyncJobHistoryQuery`, `GetReferenceChangeLogsQuery`, `GetReferenceCodeCrosswalkGridQuery`, `DeactivateReferenceCodeCrosswalkCommand`, `SearchReferenceCodesQuery`, `ReferenceDataSyncProcessor`.
- **Authoritative state:** `ReferenceCode`, `ReferenceCodeSyncJob`, `ReferenceCodeChangeLog`, and `ReferenceCodeCrosswalk` own reference data, sync state, change history, and mappings respectively.
- **Known gaps:** admin route/navigation is not confirmed, crosswalk create/import UI is incomplete, and sync scheduling controls are not exposed from the dashboard.

## 2. Entry Conditions
1. Admin has reference data management permission.
2. At least one provider is configured with reference data sync endpoints.

## 3. Journey Stages

### Stage 1: Freshness Monitoring
| Step | User Action | System Response |
|------|-------------|-----------------|
| 1.1 | Admin navigates to reference data dashboard | Dashboard shows per-provider freshness: last sync time, record count, version, sync status (success/failed/stale). |
| 1.2 | — | Stale providers highlighted (last sync exceeds configured threshold). |
| 1.3 | — | Failed sync history available with error details (masked sensitive data). |

### Stage 2: Scheduled Sync Execution
| Step | User Action | System Response |
|------|-------------|-----------------|
| 2.1 | — (automated) | Background job triggers sync per provider on configured schedule. |
| 2.2 | — | Sync fetches latest reference data from provider (drug lists, service lists, code systems). |
| 2.3a | — | **On success:** New version imported. Old codes marked inactive (never deleted). Freshness timestamp updated. |
| 2.3b | — | **On failure:** Failure logged. Retry scheduled with backoff. Alert triggered after threshold failures. |
| 2.4 | — | Doctors immediately see updated active codes for new drafts. Historical prescriptions remain readable with prior code versions. |

### Stage 3: Manual Sync
| Step | User Action | System Response |
|------|-------------|-----------------|
| 3.1 | Admin clicks "Sync Now" for a specific provider | Confirmation dialog shows last sync info. |
| 3.2 | Admin confirms | Sync job triggers immediately. Progress indicator shown. |
| 3.3 | — | Results displayed: records added, records deactivated, version info, any warnings. |

### Stage 4: Breaking Change Detection
| Step | User Action | System Response |
|------|-------------|-----------------|
| 4.1 | — | Sync detects significant changes: mass deactivation, code system structure change, checksum mismatch beyond threshold. |
| 4.2 | — | Alert raised to admin with details. |
| 4.3 | Admin reviews change impact | System shows affected active prescriptions (if any use deactivated codes). |
| 4.4 | Admin acknowledges or escalates | Acknowledgment recorded in audit. |

## 4. Exit Conditions
1. Freshness timestamp updated (success) or failure escalated with evidence.
2. Historical prescriptions remain readable regardless of reference changes.
3. Sync history and version chain retained for compliance.
4. Active doctors see only current valid codes for new work.

## 5. Error and Edge States

| Error | UI Manifestation | System Behavior |
|-------|-----------------|-----------------|
| Provider API unavailable during sync | Sync status: "Failed — provider unavailable" with timestamp | Retry scheduled; previous valid data remains usable |
| Sync returns empty or corrupt dataset | "Sync rejected — data integrity check failed" | Existing data preserved; alert raised |
| One provider sync fails, others succeed | Failed provider highlighted; others show fresh status | Provider isolation: one failure doesn't affect others |
| Checksum mismatch (data tampering concern) | "Data integrity warning" alert | Admin review required before activation |

## 6. Acceptance Criteria
1. Reference data is versioned by provider and code system.
2. Sync failure for one provider never breaks other providers.
3. Old codes are deactivated, never deleted — historical records remain valid.
4. Freshness is visible per provider on admin dashboard.
5. Manual sync is available without waiting for next scheduled run.
6. Breaking changes trigger explicit admin acknowledgment.
7. All sync operations are logged with: provider_key, version, record counts, duration, result.
