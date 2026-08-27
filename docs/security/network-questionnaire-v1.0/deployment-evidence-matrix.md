# Network questionnaire v1.0 deployment-evidence matrix

This matrix binds release-instance proof to stable IDs without duplicating row
definitions. Every row in [requirements-matrix.md](requirements-matrix.md) is
covered by the corresponding family below; the deployment-evidence phase owns
execution, immutable custody, signing, and independent verification.

**Stable-ID coverage:** AU-1, AU-2, AU-3, AU-4, AU-5, AU-6, AU-7, AU-8,
CR-1, CR-2, CR-3, CR-4, IA-1, IA-2, IA-3, IA-4, IA-5, IA-6, IA-7, IA-8,
IA-9, UD-1, UD-2, UD-3, UD-4, UD-5, UD-6, UD-7, UD-8, UD-9, UD-10, UD-11,
SM-1, SM-2, SM-3, SM-4, SM-5, SM-6, TSF-1, TSF-2, TSF-3, TSF-4, TSF-5,
TSF-6, RA-1, AC-1, AC-2, AC-3, AC-4, AC-5, AC-6, AC-7, TC-1, TC-2, TC-3,
VULN-1, VULN-2, VULN-3, VULN-4, VULN-5, VULN-6, HTTPS-1, HTTPS-2, HTTPS-3,
TLSS-1, TLSS-2, TLSS-3, TLSC-1, TLSC-2, TLSC-3, TLSC-4, CERT-1, CERT-2,
CERT-3.

| ID family | Release proof | Required artifact | Owner | Status |
| --- | --- | --- | --- | --- |
| AU-1–AU-8 | audit execution, capacity, journal, witness, retention and reconciliation | sanitized audit/fault reports | release operator + Audit | pending |
| CR-1–CR-4 / TSF-6 | resolved crypto, key, certificate and signing inventory | release crypto report | release operator | pending |
| IA-1–IA-9 / AC-1–AC-7 | remote authentication, lockout, sessions, history, CIDR and revocation | probe report | release operator + Authentication | pending |
| UD-1–UD-11 / SM-1–SM-6 | tenant/object denial, transfer, integrity and management probes | authorization/transfer report | release operator + owners | pending |
| TSF-1–TSF-5 / RA-1 | dependency faults, restart, restore, update and time records | recovery/provenance report | release operator | pending |
| TC-1–TC-3 / HTTPS-1–3 / TLSS-1–3 / TLSC-1–4 / CERT-1–3 | listener, TLS, certificate and outbound identity scans | transport report | release operator | pending |
| VULN-1–VULN-6 | component, headers, cookies, ports, methods and CSRF probes | vulnerability/install report | release operator | pending |
| TSF-3 | shared-data compatibility proof if triggered | interoperability report | owning integration | pending |

No screenshot, E2E run, scan, backup record, alert-delivery record, signing
record, or live topology is required to close repository implementation, but
each is required here when its row is triggered.
