# Failure and recovery matrix

This matrix is the repository-owned minimum for the selected failure and
recovery rows. It covers the software and hardware checks in PDF 2-6.1, the
secure-link check in 2-6.2, the valid-time check in 2-6.4, the manual-update
check in 2-6.5, and the core-function failure check in 2-7.1.

It does not add a witness, emergency journal, outbox, external alerting
service, NTP service, automatic update, update signature/hash verification,
redundancy, or HA claim. A product health state is not a delivered operator
alert; release evidence must prove the configured operator channel separately.

| Failure or event | Mandatory product result | Repository owner and proof | Release-evidence handoff |
| --- | --- | --- | --- |
| Main process crash or restart | In-flight work cannot claim success. The evaluated profile applies bounded `on-failure:5` supervision; the public ingress admits a restarted Main only after dependency readiness is healthy. | Main/Hosting composition, private readiness endpoint, Compose profile validator, Main dependency-health tests, and safe exception mapping. | Operator records termination, restart, readiness restoration, and smoke result for the exact release. |
| PostgreSQL unavailable or migration not ready | Readiness is unhealthy. Authentication, authorization, sessions, integrity state, and audit persistence do not fall back to memory or an alternate store. | Main dependency health check, owning persistence boundaries, forward-only manual-update runbook, and focused dependency-failure tests. | Operator records dependency outage, migration state, recovery, and any stopped-traffic decision. |
| Redis unavailable | Readiness is unhealthy. No in-memory substitute is used for sessions, data-protection state, or other mandatory Redis-backed security state. | Main dependency health check, Hosting/Authentication ownership, and focused dependency-failure tests. | Operator records Redis restoration and post-recovery smoke checks. |
| Object/file storage or external provider unavailable | The owning operation returns its declared dependency-unavailable result or safe failure. Cancellation, timeout, and owner-controlled bounded retry remain effective; a mutation is not reported as confirmed. | Owning module plus Hosting transport policy; provider/storage failure tests and the existing transport inventory. | Operator records outage scope, operation result, retry/recovery outcome, and any quarantined work. |
| Audit store unavailable or at capacity | Preserve the selected AU-8 behavior: continue only where the owning operation permits it, emit one redacted structured host error, and return dependency-unavailable for failed audit reads. No emergency journal is introduced. | Audit owner and existing capacity/failure contract; focused audit tests. | Operator records capacity/failure state and verifies the authorized review/evidence channel. |
| Integrity mismatch or corrupted data | The owning Files/module boundary blocks use, warns through its existing remediation state, and does not confirm corrupted data. No new witness or completeness root is introduced. | Files and owning module integrity controls; focused mismatch/blocked-use tests. | Operator records the affected release object/state and the authorized remediation decision. |
| UTC clock unavailable or wall-clock adjustment | Persisted security/audit timestamps use the selected OS UTC wall clock. Elapsed durations use the owned monotonic clock and cannot become negative because of a wall-clock jump. No NTP product feature is added. | Hosting clock owner, `TimeProvider` composition, transaction-duration test, and transport/configuration inventory. | Operator records trusted host time and the UTC time of the recovery probe. |
| Disk pressure or host/hardware loss | The affected service stops or restarts safely; no false success or uninterrupted-availability claim is made. Backup, restore, hardware replacement, and live placement remain operator responsibilities. | Evaluated Compose supervision, failure matrix, and release runbook; no HA implementation. | Operator supplies capacity, hardware, backup/restore, placement, and recovery records for the deployed release. |
| Authorized manual product update | An operator records version/source/image identity, applies compatible forward-only migrations, verifies readiness and smoke checks, and retains the previous known-good deployment for the approved rollback limit. | `manual-update-runbook.md`, profile integrity validator, and release provenance checks. | Operator supplies the approved release record and deployment evidence; the product performs no automatic update. |

## Implementation proof

Repository proof consists of the private Main readiness contract, the bounded
evaluated Compose restart policy, the Caddy upstream readiness probe, the
clock-duration test, the focused dependency-health tests, the profile-integrity
script, and the existing transport and manual-update records. These checks
prove repository behavior and configuration only; they do not prove a live
deployment.

## Deployment-evidence handoff

The release operator must exercise the exact image/configuration/topology and
retain sanitized records for dependency failure, restart, readiness recovery,
operator update, hardware or disk recovery, backup/restore where applicable,
and live secure-link placement. No repository health response is treated as
proof that an external alert was delivered.
