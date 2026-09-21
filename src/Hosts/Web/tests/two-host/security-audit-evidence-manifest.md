# Security-audit E2E evidence manifest

This manifest is the control-to-procedure index and completion report for the
real two-host evidence suite. A procedure is valid only when it performs the
public action, verifies the durable record, opens the platform audit surface,
and retains the matching unblurred audit-grid screenshot.

Current report: 16 controls have executable evidence procedures; 8 controls are
explicitly excluded with a product-backed reason. Several controls are
represented by one event because the authoritative catalog assigns that event
to more than one control.

| Control | Procedure / action | Evidence | Status |
| --- | --- | --- | --- |
| 1 Security-function lifecycle | `passwordLogin`, `passwordChange`, `tenantMemberProvisioning`, `tenantMemberRemoval` | `password-login-succeeded.png`, `password-change.png`, `tenant-member-provisioned.png`, `tenant-member-removed.png` | Covered |
| 2 Failed security-record read | Anonymous platform audit read is rejected by authorization middleware | Existing boundary denial assertion; no durable event screenshot | Excluded: authorization short-circuits before `SecurityAuditQueryService`, so no denied audit-read event is emitted |
| 3 Security-record read | `auditReview` and access-history acknowledgement | `audit-review.png`, `access-history.png` | Covered for audit/access-history paths |
| 4 Security-configuration change | `sessionPolicy` security session-policy PUT | `session-policy.png` | Covered by `authentication.security-access-policy` |
| 5 Audit-storage threshold action | Trigger configured capacity threshold | None | No deterministic public trigger |
| 6 Audit-storage failure action | Inject audit-store failure through an E2E adapter | None | Requires named failure-injection adapter |
| 7 User-data integrity verification | Public file upload/download invokes integrity verification internally | None | Excluded: no public durable integrity-verification event; only import/export events are emitted |
| 8 Authentication-mechanism use | Password login and real email-OTP action | `password-login-succeeded.png`, `password-login-denied.png` | Covered for password; email-OTP action is exercised but has no catalogued durable event, and passkey has no dedicated durable event |
| 9 Authentication outcome | Successful and denied password login | `password-login-succeeded.png`, `password-login-denied.png` | Covered for implemented password path |
| 10 Password verification | Successful password login and password change | `password-login-succeeded.png`, `password-change.png` | Covered for implemented password paths |
| 11 Security-attribute assignment | Tenant-member provisioning | `tenant-member-provisioned.png` | Covered for tenant membership |
| 12 Security-attribute change | Password change | `password-change.png` | Covered for password; other attributes pending |
| 13 Inactive/disabled-entity operation | Attempt mutation against an inactive entity | None | No deterministic public fixture/trigger identified |
| 14 User-data import | Multipart file upload with computed SHA-256 checksum | `data-import.png` | Covered |
| 15 Information export | Audit CSV export | `audit-export.png` | Covered for audit export |
| 16 Security-behavior change | `sessionPolicy` security session-policy PUT | `session-policy.png` | Covered by `authentication.security-access-policy` |
| 17 Security administration | Audit review, access-history acknowledgement, membership administration, and role replacement | `audit-review.png`, `access-history.png`, `tenant-member-provisioned.png`, `tenant-member-removed.png`, `tenant-role-replaced.png` | Covered for implemented operations |
| 18 Group/role/membership change | Tenant-member provisioning/removal and tenant-role replacement | `tenant-member-provisioned.png`, `tenant-member-removed.png`, `tenant-role-replaced.png` | Covered |
| 19 Security-function failure | Invalid `sessionPolicy` validation path with durable security event | `security-function-failure.png` | Covered by `security.failure` |
| 20 Security-capability unavailable | Audit/dependency unavailable path | None | No deterministic public trigger |
| 21 Session establishment | Password login session establishment | `session-established.png` | Covered |
| 22 Concurrent-session limit | Establish a third administrator session after setting maximum sessions to two | `session-concurrent-limit-denied.png` | Covered |
| 23 Idle-session termination | Advance the E2E clock past inactivity timeout | None | Requires clock-control adapter or deterministic timeout procedure |
| 24 Administrator idle termination | Administrator terminates another user’s idle session | None | Excluded: catalog marks feature not implemented |

The eight excluded controls are 2, 5, 6, 7, 13, 20, 23, and 24. They remain
visible here so omission cannot be mistaken for coverage. The suite is complete
only when the focused run produces every listed screenshot in one fresh
disposable run and the canonical two-host suite is reviewed for unrelated
failures.

## Validation report

Focused evidence run:

```powershell
$env:VITARISH_E2E_GREP = 'real actions create visible platform security-audit evidence'
npm run test:e2e:two-host
Remove-Item Env:VITARISH_E2E_GREP -ErrorAction SilentlyContinue
```

Result: 1 test passed against fresh two-host hosts and produced 16 screenshots
in the fresh evidence directory. The newly added `security-function-failure.png`
was inspected and shows the full, unblurred Platform Admin audit grid filtered
through the normal catalog event-type, category, operation, and outcome filters,
with the expected event name, operation, outcome, actor, and scope fields visible
in the row. No event-details dialog is opened for evidence capture.

Relevant unit tests: `dotnet test tests/Libraries/Audit.Tests/Audit.Tests.csproj
--no-restore` — 37 passed.

Canonical two-host review: 31 passed, 7 skipped, and 2 failed. The failures
were the pre-existing authenticated-boundary scenario and shared `e2e-owner`
lockout state caused by suite ordering; the evidence test itself passed in the
fresh focused run. This result is recorded rather than presented as a green
canonical suite.
