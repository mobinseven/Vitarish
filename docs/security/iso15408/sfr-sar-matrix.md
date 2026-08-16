# Initial SFR/SAR matrix

Status: proposed initial matrix for issues [#769](https://github.com/mobinseven/Vitarish/issues/769), tied to the Security Target in [`security-target.md`](security-target.md). Reviewed 2026-08-15. This matrix is not a certification result; the evaluator must confirm the applicable Common Criteria/ISO/IEC 15408 edition, component operations, dependencies, and evidence.

The matrix uses Common Criteria v3.1 Release 4 family/component identifiers as a planning baseline. The proposed assurance set is EAL2-shaped, but no EAL claim is made until evaluator review. SFRs are claimed only for the single-machine TOE boundary; environmental services and deployment controls are not silently converted into TOE functionality.

## SFR traceability

| SFR | Proposed security requirement | Objective/threat trace | Current implementation evidence | Boundary/claim limitation |
| --- | --- | --- | --- | --- |
| `FIA_UID.2` | Identify users before any action requiring identity. | `O.AUTH`; `T.UNAUTH` | Main/Identity request context and authentication endpoints | Does not claim identification of operators to the host, database, cache, or ingress. |
| `FIA_UAU.2` | Authenticate users before protected actions using configured password and passkey flows. | `O.AUTH`; `T.UNAUTH` | Authentication handlers; Users Identity/WebAuthn services | Credential/device security remains partly environmental. |
| `FDP_ACC.1` | Enforce the TOE access-control policy over protected tenant, user, role, platform, and feed objects. | `O.ACCESS`; `T.CROSSTENANT`, `T.PRIVESC` | Module API authorization metadata and application authorization | Scope is the evaluated endpoint allowlist only. |
| `FDP_ACF.1` | Apply authenticated identity, tenant, ownership, role, and platform-administrator attributes to access decisions. | `O.ACCESS`; `T.CROSSTENANT`, `T.PRIVESC` | Parties/Users tenant and role boundaries; Main tenant resolution | No claim for excluded modules/apps or arbitrary database-level access. |
| `FAU_GEN.1` | Generate attributable audit records for defined security-relevant authentication, administration, and state-change actions. | `O.AUDIT`; `T.AUDITLOSS` | `SecurityAuditEvent`, `ISecurityAuditWriter`, `SecurityAuditWriter`, additive audit migration, protected/explicitly opted-in anonymous endpoint audit state machine, password credential-change state machine, and WebAuthn credential mutation coverage; `IAuditWriter` remains the ordinary entity-change path | Completeness of the final event catalogue requires evaluator/test evidence. |
| `FAU_SAR.1` | Provide authorized review of audit records within the supported query boundary. | `O.AUDIT`; `T.AUDITLOSS` | `IAuditQueryService`, `ISecurityAuditQueryService`, query services requiring platform-admin or active-tenant-owner review access, and tenant-scoped EF filters | Does not make PostgreSQL administration or backup inspection a TOE function. |
| `FMT_MSA.1` | Manage security attributes only through authorized identity, role, tenant, and platform administration functions. | `O.ACCESS`; `T.PRIVESC`, `T.TAMPER` | Users/Parties administration APIs and role services | Exact attribute-operation assignments require evaluator confirmation. |
| `FMT_MSA.3` | Provide restrictive defaults for security attributes when objects/accounts/relationships are created. | `O.ACCESS`; `T.CROSSTENANT`, `T.PRIVESC` | Tenant/role creation and Identity defaults | Default behavior must be demonstrated for every claimed object class. |
| `FMT_SMF.1` | Identify the security-management functions exposed by the evaluated APIs. | `O.AUTH`, `O.ACCESS`, `O.AUDIT`; `T.PRIVESC` | Authentication, credential, role, tenant, and audit service/API owners | Does not include deployment/operator management functions. |
| `FMT_SMR.1` | Maintain the security roles used by the TOE, including tenant roles and platform administration. | `O.ACCESS`; `T.PRIVESC` | Identity roles, tenant role membership, `PlatformAdmin` policy | Role names and mappings remain owned by their implementation owners. |
| `FPT_STM.1` | Provide reliable time for security-relevant timestamps, subject to `A.TIME`. | `O.AUDIT`; `T.AUDITLOSS` | Audit `OccurredAt` persistence and UTC/date-time conventions | The host clock and time synchronization are environmental. |

### SFRs intentionally not claimed in the initial matrix

The initial claim does not include a TOE TLS/trusted-path requirement, cryptographic certification requirement, security testing/self-test requirement, session-lock requirement, or distributed availability/integrity requirement. TLS/ingress, cryptographic primitive/provider assurance, host time, storage durability, and failover are environmental or outside the claim unless a future Security Target explicitly expands the boundary and evidence.

## Durable audit operation contract

Mandatory security mutations use a two-phase, idempotent record protocol owned by the initiating application boundary:

| Phase | Durable event | Request behavior |
| --- | --- | --- |
| Before mutation | `started` with the caller's idempotency key plus `:started` | A store failure returns the existing provider-failure/503 contract and the mutation is not invoked. A duplicate `started` key is treated as an outcome that is not confirmed; the mutation is not repeated. |
| After mutation | `succeeded` or `failed` with the same key plus `:completed` | A successful mutation is not reported as success unless this completion write is confirmed. A completion-store failure returns provider-failure/503; the mutation outcome remains recoverable from the durable `started` record and is not automatically replayed. |

The audit writer owns no retry loop. The initiating boundary owns retries and must reuse the same idempotency key. PostgreSQL enforces `(TenantId, IdempotencyKey)` uniqueness; a duplicate write returns `AlreadyRecorded`. The security-event shape stores only event metadata and redacted reason codes, never credentials, tokens, request bodies, prompts, provider responses, or full clinical payloads.

The query boundary requires an active tenant plus either `PlatformAdmin` or the active tenant's owner role. The existing entity-audit table and query contract remain unchanged except for the same review authorization and tenant filtering. This initial contract is single-machine only; key-ring continuity, rolling rotation, and distributed ordering remain outside the claim.

## SAR traceability

| SAR | Proposed assurance requirement | Evidence expected from this repository/release | Scope note |
| --- | --- | --- | --- |
| `ASE_INT.1` | ST introduction | `security-target.md`, configuration ID, TOE name/version, and document status | Evaluator confirms ST completeness and edition. |
| `ASE_CCL.1` | Conformance claims | This matrix's stated v3.1 R4 planning baseline and explicit no-certification status | No PP/cPP conformance is claimed. |
| `ASE_SPD.1` | Security problem definition | Assets, threats, assumptions, and environmental boundaries in `security-target.md` and `toe-inventory.md` | Includes the single-machine limitation and excluded deployment modes. |
| `ASE_OBJ.1` | Security objectives | TOE/environmental objectives and threat/assumption rationale in `security-target.md` | Availability objective is explicitly single-machine only. |
| `ASE_REQ.1` | Derived security requirements | SFR/SAR tables and traceability in this document | Evaluator checks dependencies and operations. |
| `ASE_TSS.1` | TOE summary specification | Configuration manifest, inventory, source evidence, and evaluated smoke evidence | Summary must remain limited to the evaluated profile. |
| `ADV_ARC.1` | Security architecture description | Main middleware/composition, trust boundaries, topology, and fail-closed evaluated profile | Does not describe environmental components as TOE internals. |
| `ADV_FSP.2` | Security-enforcing functional specification | Authoritative Contracts/API mappings, authorization metadata, Identity, tenant, audit, and data-protection boundaries | Generated OpenAPI remains derived release evidence. |
| `ADV_TDS.1` | Basic TOE design | Main and enabled module composition, layer ownership, persistence/key boundaries | Excluded modules and apps are outside design scope. |
| `AGD_OPE.1` | Operational user guidance | Evaluated configuration deployment guidance, secret handling, ingress requirements, and operator evidence record | Must document single-machine operation and prohibited topology changes. |
| `AGD_PRE.1` | Preparative procedures | Evaluated compose file, image/profile selection, environment input contract, database/cache initialization | No production secrets belong in repository evidence. |
| `ALC_CMC.2` | Use of a CM system | Git commit, source baseline, module profile, Dockerfile, compose, and lockfile | Release operator must record immutable hashes/digests. |
| `ALC_CMS.2` | Parts of the TOE CM coverage | TOE component inventory and generated OpenAPI/release artifact inventory | Generated outputs are derived and never hand-edited. |
| `ATE_COV.1` | Evidence of test coverage | Evaluated route allowlist probes, excluded-route probes, and focused security regression tests | Coverage must be expanded if the SFR set expands. |
| `ATE_DPT.1` | Testing: basic depth | Unit/integration tests for authentication, tenant/role authorization, audit, and evaluated composition | Full solution/merge validation remains separate from this documentation change. |
| `ATE_FUN.1` | Functional testing | Reproducible evaluated build/startup smoke procedure and recorded outputs | A local smoke run is not certification evidence by itself. |
| `AVA_VAN.2` | Vulnerability analysis | Dependency/configuration review, threat analysis, and evaluator vulnerability analysis | No claim of resistance to higher attack potential. |

## Required acceptance evidence

Before release or evaluation, the owner must preserve the evaluated source commit, image digests, generated OpenAPI hash, artifact hashes, dependency inputs, startup/route probe output, test results, and deployment record. Evidence must show one machine and one Main instance, PostgreSQL/Redis as environmental services, and no distributed, multi-region, rolling-upgrade, or cluster-failover claim.
