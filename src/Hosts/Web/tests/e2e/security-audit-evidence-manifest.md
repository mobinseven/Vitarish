# Security-audit E2E evidence manifest

This manifest is the control-to-procedure index and completion report for the
real two-host evidence suite. A procedure is valid only when it performs the
public action, verifies the durable record, opens the platform audit surface,
and retains the matching unblurred audit-grid screenshot.

The table follows `security-audit-evidence.spec.ts` and the authoritative
`SecurityAuditEventCatalog`. It distinguishes screenshot evidence, query-only
checks, deliberate product selections, and missing procedures. Several controls
share an event because the catalog assigns that event to more than one control.

| Control | Procedure / action | Evidence | Status |
| --- | --- | --- | --- |
| 1 Security-function lifecycle | `passwordLogin`, `passwordChange`, `tenantMemberProvisioning`, `tenantMemberRemoval` | `password-login-succeeded.png`, `password-change.png`, `tenant-member-provisioned.png`, `tenant-member-removed.png` | Covered |
| 2 Failed security-record read | A signed-in clinic owner attempts a platform audit read; the API returns 403 and the durable denied-review count increases by one | Query assertion naming the attempted operation and resource; no dedicated denied-review screenshot | Query-covered for signed-in denial. Anonymous requests are rejected by authorization middleware before the denial filter and have no denied-review record |
| 3 Security-record read | Authorized audit review leaves the successful-review count unchanged; access-history acknowledgement remains an explicit command | Query assertion for side-effect-free review; `access-history.png` for acknowledgement | Authorized audit reads are deliberately not recorded under the owner selection in `src/Libraries/Audit/AGENTS.md`; acknowledgement is not an audit-read event |
| 4 Security-configuration change | `sessionPolicy` security session-policy PUT | `session-policy.png` | Covered by `authentication.security-access-policy` |
| 5 Audit-storage threshold action | The E2E host runs the real Main capacity monitor against an E2E storage-size adapter that reports the configured threshold crossed | `audit-storage-threshold.png` | Covered by the E2E-only threshold composition; production operator-channel delivery is not proved |
| 6 Audit-storage failure action | The E2E host makes every audit-store save fail, a sign-in attempt still gets its normal answer, and once saves succeed again the real Main outage reporter records one `security.failure` summary (start, last failure, failed writes) | `audit-storage-outage.png`, `audit-storage-outage-detail.png` | Covered by the E2E-only store-outage composition |
| 7 User-data integrity verification | Upload a valid PNG through the public upload endpoint, run the real Files verification, tamper the E2E-stored object, and verify again | `integrity-verification.png`, `integrity-verification-failed.png` | Covered by the E2E-only integrity triggers; production storage verification is not proved |
| 8 Authentication-mechanism use | Password login, real email-OTP completion, and an authentication-category review | `password-login-succeeded.png`, `password-login-denied.png`, `authentication-catalog-page-1.png` | Password records are individually asserted. The catalog and attempt auditor support phone, email, and passkey events; this procedure does not assert and capture every mechanism separately |
| 9 Authentication outcome | Successful and denied password login, and a client address blocked after repeated failures (threshold set in `source-threshold-setting.png`) | `password-login-succeeded.png`, `password-login-denied.png`, `source-address-blocked.png`, `source-threshold-setting.png` | Covered for implemented password path |
| 10 Password verification | Successful password login and password change | `password-login-succeeded.png`, `password-change.png` | Covered for implemented password paths |
| 11 Security-attribute assignment | Tenant-member provisioning | `tenant-member-provisioned.png` | Covered for tenant membership |
| 12 Security-attribute change | Password change | `password-change.png` | Covered for password; other attributes pending |
| 13 Inactive/disabled-entity operation | Attempt mutation against an inactive entity | None | No deterministic public fixture/trigger identified |
| 14 User-data import | Multipart file upload with computed SHA-256 checksum; verify the recorded cleaned filename, keyed filename digest, extension, and size | `data-import.png` and transfer-metadata assertions | Digest, extension, and size covered; the filename assertion is pending the next collective run |
| 15 Information export | File download with matching cleaned filename, keyed filename digest, extension, and size; audit CSV export | Transfer-metadata assertions and `audit-export.png` | Covered for file download and audit export except the filename assertion, which is pending the next collective run |
| 16 Security-behavior change | `sessionPolicy` security session-policy PUT | `session-policy.png` | Covered by `authentication.security-access-policy` |
| 17 Security administration | Access-history acknowledgement, membership administration, and role replacement | `access-history.png`, `tenant-member-provisioned.png`, `tenant-member-removed.png`, `tenant-role-replaced.png` | Covered for these commands; authorized audit review emits no event |
| 18 Group/role/membership change | Tenant-member provisioning/removal and tenant-role replacement, with the previous and new role on the record | `tenant-member-provisioned.png`, `tenant-member-removed.png`, `tenant-role-replaced.png`, `tenant-role-change-detail.png` | Covered |
| 19 Security-function failure | Invalid `sessionPolicy` validation path with durable security event | `security-function-failure.png` | Covered by `security.failure` |
| 20 Security-capability unavailable | Audit-store outage: saves fail, then one `security.failure` summary of the outage is recorded after recovery | `audit-storage-outage.png`, `audit-storage-outage-detail.png` | Covered by `security.failure` through the E2E-only store-outage composition |
| 21 Session establishment | Password login session establishment | `session-established.png` | Covered |
| 22 Concurrent-session limit | Establish a third administrator session after setting maximum sessions to two | `session-concurrent-limit-denied.png` | Covered |
| 23 Idle-session termination | Advance the E2E clock past inactivity timeout | None | Requires clock-control adapter or deterministic timeout procedure |
| 24 Administrator idle termination | Administrator terminates another user’s idle session | None | No dedicated procedure captures this idle-session case; catalog membership alone does not prove it |

Controls 6 and 20 share the recovered audit-storage-outage procedure. Controls
13, 23, and 24 still lack dedicated evidence procedures. Control 2 has a signed-in
denial assertion but lacks its own screenshot and anonymous-denial recording;
control 8 lacks separate evidence for every mechanism. Control 3 records the
explicit selection not to audit authorized reads. These limits must remain
visible when assessing completion. A new evidence run must produce every listed
screenshot in one fresh disposable directory, and its canonical two-host result
must be reviewed separately from this source-coverage inventory.

## Validation report

Focused evidence run:

```powershell
$env:VITARISH_E2E_GREP = 'real actions create visible platform security-audit evidence'
npm run test:e2e:two-host
Remove-Item Env:VITARISH_E2E_GREP -ErrorAction SilentlyContinue
```

Latest recorded integration validation: 2026-10-26, PR #972, tested head
`cfe78f670cbad3eb64d891d1187e37b8e7d1d167`, merged as
`b4fa60487c41eba58a5e7dbf5718ff896f02fc2f`.

- Command: `npm run test:e2e:two-host` from `src/Hosts/Web`.
- Result: 56 passed, 13 existing skips, exit 0. One sign-in navigation failed
  initially and passed on the configured retry; this was not a first-attempt
  clean pass.
- The authenticated `security-audit-controls-merge-proof.png` and changed-screen
  captures were inspected, including source-threshold/block evidence, account
  history, role-change detail, and outage-summary detail. The role and outage
  detail screenshots intentionally open their record-detail sheets.
- Production deployment, release-host TLS/certificate scans, real operator-channel
  delivery, and controlled NSMAIL cases were not run by that integration proof.

These are historical results for the identified head, not validation of every
later checkout. They supersede the earlier 16-screenshot focused baseline and
the earlier canonical result of 31 passed, 7 skipped, and 2 failed. A passing
local two-host run does not prove the deployed ingress TLS policy.

Validation of the current edits is deferred to the final combined run at the
owner's request (2026-10-28). The interrupted two-host startup produced no new
test result or authenticated screenshot.
