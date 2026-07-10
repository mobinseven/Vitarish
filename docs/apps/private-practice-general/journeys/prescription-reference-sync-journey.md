# Reference Data Sync Journey (Admin)

## 1. Context
- Persona: Clinic admin
- Trigger: Scheduled sync run or manual refresh request.
- Goal: Keep prescription code references current without disrupting active workflows.

## 2. Journey Flow
1. Admin views reference-data freshness dashboard.
2. Scheduled sync executes and records results.
3. On repeated failures, system triggers alert.
4. Admin may run manual sync.
5. New version activates; old codes marked inactive (not deleted).
6. Doctors use updated active codes for new drafts while historical prescriptions remain readable.

## 3. Exit Conditions
1. Freshness timestamp updated (or failure escalated).
2. Sync history retained for compliance and support.

## 4. Guardrails
- Sync failure must not break reads from previously valid cached data.
- Historical records must remain interpretable after code version changes.
