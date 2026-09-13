# Network questionnaire v1.0 requirements matrix

This is the canonical row catalog. The row definitions remain in
[`implementation-plan.md`](implementation-plan.md); this matrix binds each
stable ID to its trigger, owner, planned proof, and release handoff. Optional
questionnaire rows are intentionally absent. `stage-0-open` means the row is
selected and has a proof path, but the Stage 0 evaluator gate is not closed.

For this work item, the PDF pages 20–23 are the sole scope authority. The
selected minimum is `Authentication_SessionPolicy_Put`,
`Authentication_SessionPolicy_Get`, `SeaCreateProject`, the explicit 2-5.4
disposition table in the implementation plan, and the existing Users
one-effective-tenant-role model. The broader catalog remains a reference and
does not authorize optional capabilities or generic role/capability frameworks.

| ID | Source/trigger | Selected value | Owner | Implementation artifact | Deterministic test | Deployment handoff | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| AU-1 | 2-1.1 mandatory | typed catalog for all enabled events | Audit | event catalog and tests | catalog coverage test | event execution report | stage-0-open |
| AU-2 | 2-1.2 mandatory | UTC, actor, outcome, normalized IP | Audit | event contract | field contract test | sampled event report | stage-0-open |
| AU-3 | 2-1.3 mandatory | existing tenant-owner/platform-admin review rule with selected tenant | Audit | review authorization | allowed/denied review test | access audit report | stage-0-open |
| AU-4 | 2-1.4 mandatory | typed DTOs; no opaque payload | Audit | review contracts | DTO contract test | response sample | stage-0-open |
| AU-5 | 2-1.5 mandatory | actor/time/type filter and sort | Audit | query contract | query scope/sort test | review probe | stage-0-open |
| AU-6 | 2-1.6 mandatory | read-only product audit review; no hashes, chains, or witnesses | Audit | read-only review endpoint and DTOs | update/delete absence and review contract test | review probe | stage-0-open |
| AU-7 | 2-1.7 mandatory | one positive configured storage threshold and in-product warning | Audit | capacity threshold query state | below/reached threshold test | capacity review record | stage-0-open |
| AU-8 | 2-1.8 mandatory | continue operation, log one redacted structured host error, and return dependency-unavailable for failed reads | Audit | best-effort write/read failure contract | operation continuation and recovery test | fault report | stage-0-open |
| CR-1 | 2-2.1 mandatory | AES inventory with exact mode/length | Hosting/owners | crypto inventory | inventory validator | release crypto scan | stage-0-open |
| CR-2 | 2-2.2 mandatory | hash/HMAC/password inventory | Hosting/owners | crypto inventory | algorithm validator | release crypto scan | stage-0-open |
| IA-1 | 2-3.1 mandatory | positive threshold, unknown IDs included | Authentication | policy options | parity/threshold test | lockout probe | stage-0-open |
| IA-2 | 2-3.2 mandatory | 5 attempts; 15-minute denial | Authentication | attempt policy | known/unknown timing test | lockout report | stage-0-open |
| IA-3 | 2-3.3 mandatory | non-secret security state | Users/Authentication | security-state model | secret-redaction test | state probe | stage-0-open |
| IA-4 | 2-3.4 mandatory | shared configurable password rules | Users/Authentication | password policy | all-entry-point test | policy report | stage-0-open |
| IA-5 | 2-3.5 mandatory | only declared pre-auth actions | Authentication | anonymous action register | endpoint metadata validator | anonymous route report | stage-0-open |
| IA-6 | 2-3.6 mandatory | password plus one independent mechanism | Authentication | evaluated auth config | remote auth mechanism test | authentication evidence | stage-0-open |
| IA-7 | 2-3.7 mandatory | identity, roles, history | Authentication/Users | active security state | state transition test | session report | stage-0-open |
| IA-8 | 2-3.8 mandatory | one session by default | Authentication | session policy | replacement/notice test | concurrent-session probe | stage-0-open |
| IA-9 | 2-3.9 mandatory | security changes revoke sessions | Authentication | revocation coupling | attribute-change test | revocation report | stage-0-open |
| UD-1 | 2-4.1 mandatory | executable role/object matrix | owning Applications | authorization matrix | matrix validator | endpoint authorization report | stage-0-open |
| UD-2 | 2-4.2 mandatory | server-trusted subject/tenant | owning Applications | use-case policy | client-manipulation test | denial probe | stage-0-open |
| UD-3 | 2-4.3 mandatory | authorize every protected use case | owning Applications | application policies | direct HTTP denial tests | tenant/object report | stage-0-open |
| UD-4 | 2-4.4 mandatory | deny over session bound | Authentication | session source of truth | bound test | session probe | stage-0-open |
| UD-5 | 2-4.5 mandatory | clear buffers/remove temp files | Files/Hosting | residual-data controls | reuse isolation test | residual-data report | stage-0-open |
| UD-6 | 2-4.6 mandatory | type/size/format/owner/rate metadata | owning module | transfer metadata | pre-provider rejection test | transfer probe | stage-0-open |
| UD-7 | 2-4.7 mandatory | authenticated encrypted transfer | Hosting/owning integration | channel register | invalid-channel test | channel evidence | stage-0-open |
| UD-8 | 2-4.8 mandatory | owner authorization and format limits | owning module | export policy | export denial test | export report | stage-0-open |
| UD-9 | 2-4.9 mandatory | named bulk-export capabilities | owning module | export administration | capability test | management report | stage-0-open |
| UD-10 | 2-4.10 mandatory | keyed file/non-file integrity | Files/owning module | manifests and checksums | mismatch/delete test | integrity report | stage-0-open |
| UD-11 | 2-4.11 mandatory | block and warn on mismatch | Files/owning module | remediation state | blocked-use test | warning/remediation record | stage-0-open |
| SM-1 | 2-5.1 mandatory | `Authentication_SessionPolicy_Put` changes the existing security-function policy | Authentication | named session-policy policy and application handler | unauthenticated/denied/authorized, bounds, antiforgery, audit | management probe | implementation-in-progress |
| SM-2 | 2-5.2 mandatory | `Authentication_SessionPolicy_Get` reads the existing protected security feature | Authentication | named read policy and application handler | unauthenticated/denied/authorized, tenant, audit | management probe | implementation-in-progress |
| SM-3 | 2-5.3 mandatory | `SeaCreateProject` uses the existing Sea capability with a responsibility-specific operation requirement | Sea | `SeaAuthorizationOperations.CreateProject` | no generic Manage path; authorization test | management probe | implementation-in-progress |
| SM-4 | 2-5.4 mandatory | explicit supported/unsupported disposition for every PDF checklist category | owning modules | implementation-plan disposition table and existing owners | disposition/operation metadata plus focused owner tests | management report | implementation-in-progress |
| SM-5 | 2-5.5 mandatory | existing role-backed model; no new role/capability framework | Users | existing named Users policies and role owner | role policy and denial tests | role report | implementation-in-progress |
| SM-6 | 2-5.6 mandatory | one permitted effective tenant role; atomic canonical replacement/revoke | Users | `ReplaceTenantRole` compatibility adapter and handler | idempotency, active-session, conflict/retry, invariant, audit | membership report | blocked-unverified |
| TSF-1 | 2-6.1 mandatory | fail closed; preserve integrity | Hosting/owners | failure matrix | fault/restart tests | recovery record | stage-0-open |
| TSF-2 | 2-6.2 mandatory | encrypted separate-machine links | Hosting | topology/link register | placement/channel test | topology scan | stage-0-open |
| TSF-4 | 2-6.4 mandatory | UTC trusted OS clock | Hosting/operators | clock policy | monotonic-duration test | time record | stage-0-open |
| TSF-5 | 2-6.5 mandatory | manual product-version update | Release operator | update runbook | provenance validator | release record | stage-0-open |
| RA-1 | 2-7.1 mandatory | restart/read-only; no HA claim | Hosting/operators | degraded-mode matrix | dependency fault tests | recovery evidence | stage-0-open |
| AC-1 | 2-8.1 mandatory | transactional session maximum | Authentication | session store | concurrency test | session probe | stage-0-open |
| AC-2 | 2-8.2 mandatory | 1–30 minute idle bound | Authentication | timeout policy | expiry test | timeout probe | stage-0-open |
| AC-3 | 2-8.3 mandatory | server revoke before success | Authentication | logout command | logout test | logout probe | stage-0-open |
| AC-4 | 2-8.4 mandatory | show last success UTC time | Users/Web | history contract/UI | visible-history test | UI evidence | stage-0-open |
| AC-5 | 2-8.5 mandatory | show last failure/count | Users/Web | history contract/UI | visible-failure test | UI evidence | stage-0-open |
| AC-6 | 2-8.6 mandatory | stable until acknowledge | Users/Web | history acknowledgement | persistence test | UI evidence | stage-0-open |
| AC-7 | 2-8.7 mandatory | normalized CIDR restriction | Authentication/Hosting | proxy/IP policy | CIDR test | access probe | stage-0-open |
| TC-1 | 2-9.1 mandatory | HTTPS trusted channel | Caddy/Hosting | Caddy and TLS config | config validator | TLS scan | stage-0-open |
| TC-2 | 2-9.2 mandatory | supported communication via channel | Caddy/Hosting | ingress routing | plaintext/route test | public probe | stage-0-open |
| TC-3 | 2-9.3 mandatory | plaintext authentication impossible | Caddy/Authentication | redirect/rejection policy | HTTP auth test | public probe | stage-0-open |
| VULN-1 | nsmail-1 mandatory | exact version/digest inventory | Release operator | release inventory | inventory completeness test | vulnerability scan | stage-0-open |
| VULN-2 | nsmail-1 mandatory | exact security headers | Caddy | Caddy header policy | route-header test | header scan | stage-0-open |
| VULN-3 | nsmail-1 mandatory | host-only secure cookie inventory | Authentication | cookie policy | cookie attribute test | cookie scan | stage-0-open |
| VULN-4 | nsmail-1 mandatory | public/private listener allowlist | Hosts | compose/port manifest | static port test | listener scan | stage-0-open |
| VULN-5 | nsmail-1 mandatory | explicit methods; unsafe verbs 4xx | Hosting/owners | endpoint metadata | method test | method scan | stage-0-open |
| VULN-6 | nsmail-1 mandatory | owned CSRF lifecycle | Authentication | CSRF contract | missing/stale/replay test | CSRF probe | stage-0-open |
| HTTPS-1 | HTTPS selected | declared HTTPS behavior | Caddy | Caddy config | Caddy parse test | TLS report | stage-0-open |
| HTTPS-2 | HTTPS selected | TLS only; SSL rejected | Caddy | TLS bounds | negative protocol test | TLS scan | stage-0-open |
| HTTPS-3 | Main/provider HTTPS client | invalid peers rejected | Hosting/integration | HttpClient policy | invalid-cert test | outbound probe | stage-0-open |
| TLSS-1 | Caddy TLS server | TLS 1.2 only pending decision | Caddy | protocol config | protocol validator | suite scan | stage-0-open |
| TLSS-2 | Caddy TLS server | reject SSL/TLS below 1.2 | Caddy | protocol config | negative protocol test | TLS scan | stage-0-open |
| TLSS-3 | Caddy TLS server | exact key/curve selection | Release operator | certificate policy | key/curve validator | certificate scan | stage-0-open |
| TLSC-1 | enabled TLS clients | declared protocol/suites | Hosting/integrations | client inventory | inventory validator | outbound scan | stage-0-open |
| TLSC-2 | enabled TLS clients | RFC 6125 identity | Hosting/integrations | handler policy | hostname test | outbound probe | stage-0-open |
| TLSC-3 | enabled TLS clients | invalid cert fail closed | Hosting/integrations | handler policy | invalid-cert test | outbound probe | stage-0-open |
| TLSC-4 | ECDH/ECDHE client suite | approved NIST curves | Hosting/integrations | suite inventory | curve validator | outbound scan | stage-0-open |
| CERT-1 | TLS certificate validation | RFC 5280 path/CA/EKU/revocation | Hosting/operators | certificate policy | chain validator | certificate report | stage-0-open |
| CERT-2 | CA certificate validation | CA=TRUE required | Hosting/operators | certificate policy | CA constraint test | certificate report | stage-0-open |
| CERT-3 | TLS/signing certificate | X.509v3 purpose EKU | Hosting/operators | certificate policy | EKU test | certificate report | stage-0-open |
| CR-3 | product key generation | generation/destruction inventory | Hosting/operators | key inventory | key-lifecycle test | key report | stage-0-open |
| CR-4 / TSF-6 | product signing or automatic/signed-file update selected | not triggered; no product signing or update-integrity feature | Release operator | applicability disposition | applicability check | release record if triggered | not-triggered |
| TSF-3 | shared secure IT product data | OPEN shared-data map | Owners | data-sharing register | compatibility test | interoperability report | stage-0-open |

The matrix is intentionally not a completion claim. The Stage 0 gate requires
the evaluator state, exact release inputs, membership analysis, topology
placement, and integrity lint to close before any implementation behavior is
changed.
