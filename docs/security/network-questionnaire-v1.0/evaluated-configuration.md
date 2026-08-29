# Network questionnaire v1.0 evaluated configuration

**Profile identity:** `vitarish-product-evaluated-v1.0`

**Status:** Stage 2 repository transport and cryptography baseline selected;
deployment and release-instance evidence remain outside repository implementation.

This is a separate profile from [`docs/security/iso15408/`](../iso15408/), whose
identity is `vitarish-toe-main-evaluated-2026-08-15` and whose boundary is only
Main, PostgreSQL, and Redis. Evidence, compose files, topology, and claims do
not cross between profiles.

## Frozen repository boundary

The profile contains Caddy public ingress, the production Main host, the
repository-delivered browser applications, PostgreSQL, Redis, file/object
storage when enabled by the release, and only explicitly listed external
providers. Main is built with `ModuleBuildProfile=All`; modules and apps are
listed in [toe-inventory.md](toe-inventory.md). A release that enables another
module, app, provider, endpoint family, transfer route, authentication method,
mobile client, SSH interface, mTLS flow, or automatic update reopens Stage 0.

The product-evaluated profile selects the full compiled module and app boundary.
The pre-existing Main-only evaluated profile remains restricted to its declared
six modules and no apps; the two identities are explicit and are tested
independently.

Remote authentication is username/password plus phone/SMS OTP for accounts with
Identity two-factor authentication enabled. Accounts without that account-level
flag complete password authentication directly. Passwordless phone,
passwordless email, and passkey login are disabled in this profile; SMS OTP is
not a fallback or alias. The evaluator account is a dedicated account with
`TwoFactorEnabled=true`, a confirmed phone number, and no shared credentials.

## Baseline policy

Until a written evaluator decision changes it, the profile uses TLS 1.2 only on
every TLS-enabled evaluated connection, Caddy `h1 h2`,
`TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256`, P-256, no HTTP/3, no UDP 443,
minimum eight-character complex passwords, five failed attempts, fifteen-minute
denial, one concurrent browser session, and a thirty-minute idle session.
TLS 1.0/1.1 and SSL are rejected. Private placement or a Docker network is not
treated as a secure channel.

## Exact release inputs

The release mechanism must resolve and record a product version for every item
below before evidence collection. An explicit versioned image/reference and
source revision may be recorded for operational clarity; a cryptographic digest
is not required by this profile.

| Item | Repository baseline | Release value |
| --- | --- | --- |
| .NET target/runtime | `net10.0`; `mcr.microsoft.com/dotnet/aspnet:10.0` family | deployment-evidence-pending: exact version |
| Main build | `src/Hosts/Main/Dockerfile`, `ModuleBuildProfile=All` | source revision and image version |
| PostgreSQL | PostgreSQL 18 line | deployment-evidence-pending: exact image version |
| Redis | Redis 7.4.5 baseline | deployment-evidence-pending: exact image version |
| Caddy | official Caddy image, explicit `h1 h2` policy | deployment-evidence-pending: exact version |
| Web applications | release-supplied versioned images | deployment-evidence-pending: exact image versions |
| packages/providers | `src/Directory.Packages.props`, lockfiles, resolved npm lock | release version report |

No secret, credential, private key, token, patient data, questionnaire file, or
release screenshot is an input to repository implementation or committed
configuration.

The Main container receives the authentication attempt-key pepper from the
protected `VITARISH_AUTH_ATTEMPT_KEY_PEPPER` deployment input. It is used only
for realm-scoped keyed attempt identifiers and is never recorded in release
evidence.

## Stage 0 decisions

| Decision | Selected repository baseline | Evaluator state |
| --- | --- | --- |
| Audit IP | Retain normalized source IP for the full retained event lifetime | implementation-selected; deployment evidence pending |
| Audit full | Restricted mode; protected bounded emergency journal; audit-required mutations fail closed | implementation-selected; deployment evidence pending |
| RA-1 continuity | Restart, bounded retry, safe recovery/read-only behavior; no HA claim | implementation-selected; deployment evidence pending |
| TLS 1.3/suites | TLS 1.2 only; exact selected TLS 1.2 suites and key/curve settings are release-bound | implementation-selected; deployment evidence pending |
| Revocation | RFC 5280 path validation with documented OCSP/CRL behavior and fail-closed policy | implementation-selected; deployment evidence pending |
| `X-XSS-Protection` | Do not emit obsolete header unless evaluator explicitly requires it | implementation-selected; deployment evidence pending |
| CSRF renewal | Authentication-owned lifecycle; bounded reusable antiforgery tokens unless evaluator requires a nonce/replay owner | implementation-selected; deployment evidence pending |
| Emergency journal/witness | Separate protected bounded journal plus independent append-only witness | implementation-selected; deployment evidence pending |
| Role membership | One evaluated role per user/tenant; production membership snapshot is an operator-owned deployment input, not repository implementation | implementation-selected; deployment evidence pending |
| Runtime composition | Full product boundary; Main-only remains a separate restricted profile | implementation-selected |
| TLS server suite | `TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256`; RSA certificate key at least 2048 bits; P-256 ECDH | implementation-selected; deployment evidence pending |
| TLS clients | TLS 1.2; native X.509 path/CA/EKU/hostname validation; online revocation checking where the client exposes it; invalid certificates fail closed | implementation-selected; provider evidence pending |
| Time source | Operating-system UTC clock; monotonic runtime clock for elapsed durations | implementation-selected; deployment evidence pending |
| Update mechanism | Authorized manual deployment identified by product version and source revision; no automatic discovery, installation, signing, or update-file verification | implementation-selected; release evidence pending |

The implementation baseline is not a claim that a deployed release has been
evaluated. Release versions, production membership snapshots, operator
decisions, and deployment evidence belong to the separate deployment-evidence
phase.

## Required membership analysis record

This is a release-time, read-only query against the production-equivalent
database, not a source inspection claim. It must be executed with a consistent
snapshot and its sanitized result attached before one-role-per-tenant
enforcement is designed:

```sql
SELECT md5(u."Id") AS user_hash,
       CASE WHEN position(':' IN r."Name") > 0
            THEN md5(split_part(r."Name", ':', 1)) END AS tenant_hash,
       CASE WHEN position(':' IN r."Name") > 0
            THEN split_part(r."Name", ':', 2)
            ELSE r."Name" END AS role_name,
       count(*) OVER (
           PARTITION BY u."Id", CASE WHEN position(':' IN r."Name") > 0
                                      THEN split_part(r."Name", ':', 1) END
       ) AS membership_count
FROM "Users"."AspNetUserRoles" ur
JOIN "Users"."AspNetUsers" u ON u."Id" = ur."UserId"
JOIN "Users"."AspNetRoles" r ON r."Id" = ur."RoleId"
ORDER BY user_hash, tenant_hash, role_name;
```

The committed helper
[`Get-EvaluatedMembershipAnalysis.ps1`](../../../src/Hosts/scripts/Get-EvaluatedMembershipAnalysis.ps1)
executes this query read-only, rejects password-bearing command-line
connection strings, hashes user/tenant identifiers, and removes its temporary
query file. It does not replace the required operator-owned snapshot identity,
conflict decision, or migration approval.

**Snapshot identity:** deployment-evidence-pending; no production snapshot is a repository input

**Execution timestamp:** deployment-evidence-pending

**Conflicting memberships:** deployment-evidence-pending; no migration or enforcement is authorized by this document
**Global-role disposition:** `PlatformAdmin` and other global roles must be
explicitly separated from tenant-role cardinality.

**Owner-approved migration:** deployment-evidence-pending; no migration is authorized from this
document alone.

## Revision triggers

New endpoints, providers, distributed components, listeners, browser/mobile
clients, authentication mechanisms, transfer routes, SSH, automatic updates,
mTLS, crypto primitives, emergency-journal dependencies, evidence-custody
dependencies, or a changed evaluator interpretation reopen this document and
all three matrices.
