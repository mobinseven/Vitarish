# Vitarish initial Security Target

Status: initial Security Target claim for issues [#768](https://github.com/mobinseven/Vitarish/issues/768) and [#769](https://github.com/mobinseven/Vitarish/issues/769). This is an implementation-backed draft for evaluator review, not a certification result. Reviewed 2026-08-15.

This document is authoritative for the initial security claim. The configuration manifest, component inventory, and topology remain authoritative for the evaluated build and deployment evidence:

- [`evaluated-configuration.md`](evaluated-configuration.md)
- [`toe-inventory.md`](toe-inventory.md)
- [`topology.md`](topology.md)
- [`sfr-sar-matrix.md`](sfr-sar-matrix.md)

## TOE definition and claim boundary

The Target of Evaluation (TOE) is the Vitarish Main evaluated configuration: one published `Vitarish.Main.dll` process/container and its evaluated in-process Authentication, Requests, Users, Parties, Platform, and Feed modules. The TOE boundary includes the Main host's security-relevant middleware and endpoint composition, identity and authorization behavior, tenant boundary enforcement, audit integration, data-protection integration, and the enabled module behavior listed in the configuration manifest.

The TOE is evaluated in a **single-machine configuration**. The initial claim assumes one machine running one Main instance, one PostgreSQL service, and one Redis service on the private evaluated network. The claim is about the security behavior of that configuration at its defined interfaces; it is not a claim about every project, build profile, application, or deployment in this repository.

Database, cache, storage, operating system, container runtime, and ingress remain environmental dependencies and are not TOE components unless a future Security Target explicitly includes them. Host controls, TLS termination, browser/user-agent behavior, and release/operator processes are likewise environmental. Their configuration, patching, authentication, isolation, availability, and recovery are assumptions or deployment evidence items.

## Explicit exclusions

The initial availability and integrity claims do not cover multi-instance failover. They cover only the stated single-machine configuration and the behavior of one Main instance while its environmental dependencies satisfy their assumptions. No claim is made for distributed deployment, multi-region behavior, rolling upgrades, blue/green replacement, cluster failover, replicated state, automatic disaster recovery, or cross-instance consistency.

Changing the machine topology, running more than one Main instance, adding replicas or failover, changing the evaluated build profile, enabling excluded modules/apps, or adding an ingress/service not represented by the manifest creates a different configuration and requires a revised Security Target and matrix.

## Security problem definition

### Assets

The protected assets are authenticated identities and credentials, sessions and tokens, tenant membership and role attributes, user/tenant/platform data, audit records, data-protection key material, configuration and evaluated release artifacts. The detailed owner and storage mapping is in [`toe-inventory.md`](toe-inventory.md).

### Threats

| ID | Threat | Security concern |
| --- | --- | --- |
| `T.UNAUTH` | An unauthenticated or malformed client submits requests to obtain protected data or invoke protected operations. | Authentication, input handling, and authorization must reject unauthorized access. |
| `T.CROSSTENANT` | An authenticated tenant user manipulates identifiers, host context, or role data to access or alter another tenant's assets. | Tenant context and authorization attributes must be enforced at every protected operation. |
| `T.PRIVESC` | A tenant administrator or ordinary member attempts to obtain platform-wide or otherwise unauthorized privileges. | Role and administrative boundaries must be enforced and security-relevant changes attributable. |
| `T.TAMPER` | A hostile client or compromised environmental service attempts to alter protected data, keys, configuration, or audit evidence through the TOE interfaces. | The TOE must validate inputs, authorize mutations, and preserve the integrity of security-relevant records within the stated environment assumptions. |
| `T.AUDITLOSS` | A security-relevant action occurs without an attributable record or authorized review capability. | The TOE must generate and expose appropriately scoped audit records. |
| `T.REPLAY` | A captured session, token, antiforgery value, or internal callback is replayed to repeat or extend an operation. | Authentication/session, antiforgery, callback-envelope, and duplicate-call controls must reject invalid or stale use. |
| `T.ORIGIN` | A hostile origin, Host header, or cross-site request abuses browser credentials or trusted forwarded headers. | Exact CORS origins, antiforgery, TLS policy, and bounded proxy trust must be enforced. |
| `T.INTERNAL` | An untrusted caller reaches an endpoint intended only for an internal service. | The endpoint must require its declared internal credential and application capability. |
| `T.AUDITALTER` | An actor alters or suppresses audit evidence. | Audit writes and review must be authorized and preserved through the stated data-service assumptions. |
| `T.CONFIG` | Unsafe security configuration, keys, or evaluated identity changes weaken the deployed TOE. | Startup validation must fail closed and release configuration must be attributable. |
| `T.SUPPLY` | A compromised or unrecorded dependency/image changes evaluated behavior. | The release record must identify source, artifacts, and dependency/image inputs. |
| `T.DOS` | A client exhausts the single Main instance or an environmental dependency. | The TOE limits exposed authentication traffic and fails closed; capacity and upstream protection remain environmental. |
| `T.DISCLOSE` | Sensitive data, tokens, keys, or diagnostics are disclosed through an API, error, log, or configuration evidence. | Access control, redaction, and non-secret diagnostics must preserve confidentiality. |
| `T.SINGLEFAIL` | The single Main process or an environmental dependency becomes unavailable or inconsistent. | The initial claim is limited to bounded single-machine operation; it does not promise multi-instance or failover availability/integrity. |

### Assumptions

| ID | Assumption | Environmental responsibility |
| --- | --- | --- |
| `A.PHYSICAL` | The host, operating system, container runtime, volumes, and network are physically and administratively protected. | Operator/host owner |
| `A.SERVICES` | PostgreSQL and Redis are available, authenticated, isolated to the private network, and preserve or restore their data according to the deployment record. | Database/cache/storage owner |
| `A.PROXY` | The TLS reverse proxy or ingress terminates and protects external transport, forwards only the configured upstream traffic, and applies its recorded ACL/certificate policy. | Ingress owner |
| `A.OPERATOR` | Operators protect secrets, use the evaluated image/profile, restrict deployment access, and record release identity and image digests. | Release operator |
| `A.CLIENT` | Users and browser/user-agent software protect credentials, tokens, and local state and use the documented interfaces. | User/client environment |
| `A.TIME` | The host time source is sufficiently reliable for timestamps and operational evidence. | Operating-system/environment owner |

There is no assumption of a distributed cluster, replica, multi-region service, rolling upgrade, or failover mechanism in this initial claim.

## Security objectives

### TOE objectives

| ID | Objective | Owner and evidence direction |
| --- | --- | --- |
| `O.AUTH` | Identify and authenticate users before protected operations, including the configured password/passkey flows. | Authentication module/Main host; endpoint metadata and authentication regression tests. |
| `O.ACCESS` | Enforce tenant, ownership, role, and platform-administrator access rules for protected objects and operations. | Module application/API owners; checked Main-module authorization matrix and direct authorization tests. |
| `O.INPUT` | Validate transport and use-case inputs and reject malformed, replayed, or unauthorized requests at the relevant boundary. | Transport and application owners; validation, antiforgery, callback replay, and negative tests. |
| `O.AUDIT` | Generate attributable audit records for defined security-relevant actions and provide authorized, tenant-scoped review. | Audit library and module application owners; audit migrations, writer/query tests, and evaluated startup record. |
| `O.INTEGRITY` | Preserve the integrity of security-relevant state through authorized application writes and protected key/cache integration, within the single-machine environmental assumptions. | Main host/data-protection owner; security configuration tests and protected storage evidence. |
| `O.AVAIL` | Keep the evaluated single Main instance operational and fail closed when required environmental dependencies are unavailable; this objective has no multi-instance failover claim. | Main host/operator; startup-failure evidence, rate-limit tests, and deployment record. |
| `O.CONF` | Prevent disclosure of protected data and security material through authorized API behavior and non-secret diagnostics. | Module/API and host owners; authorization tests and diagnostic-output review. |

### Environmental objectives

| ID | Objective | Owner and evidence direction |
| --- | --- | --- |
| `OE.HOST` | Protect and patch the operating system, container runtime, host, volumes, network, and physical machine. | Host owner; patch/administrative record. |
| `OE.DATA` | Operate, authenticate, isolate, back up, and recover PostgreSQL, Redis, and persistent storage. | Data-service owner; deployment and recovery record. |
| `OE.INGRESS` | Provide TLS, ingress routing, certificate lifecycle, upstream restrictions, and external availability at the deployment boundary. | Ingress owner; recorded TLS/ACL configuration. |
| `OE.OPERATE` | Protect secrets, deploy only the recorded evaluated configuration, and control release and operator access. | Release operator; commit, digest, artifact-hash, and secret-reference record. |
| `OE.CLIENT` | Protect client credentials/tokens and use the TOE through the documented secure interface. | User-agent owner; operational guidance. |

## Security claims

The initial claim is the proposed SFR/SAR set in [`sfr-sar-matrix.md`](sfr-sar-matrix.md). The matrix is deliberately traceable to current implementation evidence and records exclusions where the repository does not support a defensible claim. It is not permission to infer claims for excluded apps, providers, infrastructure, or distributed operation.

The working assurance objective is EAL4-style: methodical design, implementation, test, vulnerability, delivery, and lifecycle evidence must be assembled to the degree an evaluator requires. This is a planning objective only, not an EAL4 claim, Protection Profile conformance claim, or certification result. The v3.1 R4 SFR/SAR matrix is the accepted non-normative planning baseline for issue #769; an evaluator must select the normative version, components, operations, and evidence before any conformance claim.

## Traceability and gap register

Every threat is addressed by the TOE or environmental objective(s) below; `T.SINGLEFAIL` is intentionally bounded by `O.AVAIL` and the explicit single-machine exclusion.

| Threat | Objective or assumption | Closure evidence owner |
| --- | --- | --- |
| `T.UNAUTH`, `T.PRIVESC` | `O.AUTH`, `O.ACCESS`, `O.INPUT` | Authentication/module owners |
| `T.CROSSTENANT` | `O.ACCESS`, `O.INPUT` | Module application owners |
| `T.TAMPER`, `T.CONFIG` | `O.INTEGRITY`, `OE.OPERATE`, `OE.DATA` | Main host/release/data owners |
| `T.AUDITLOSS`, `T.AUDITALTER` | `O.AUDIT`, `OE.DATA` | Audit/data owners |
| `T.REPLAY`, `T.ORIGIN`, `T.INTERNAL` | `O.AUTH`, `O.INPUT`, `OE.INGRESS`, `OE.CLIENT` | Host/API/ingress owners |
| `T.SUPPLY` | `OE.OPERATE`, `OE.HOST` | Release/host owners |
| `T.DOS` | `O.AVAIL`, `OE.INGRESS`, `OE.DATA` | Main/ingress/data owners |
| `T.DISCLOSE` | `O.CONF`, `O.ACCESS`, `OE.CLIENT` | Module/API/client owners |
| `T.SINGLEFAIL` | `O.AVAIL`, `A.SERVICES`, `OE.DATA` and the explicit single-machine exclusion | Main/data-service owners |

| Gap | Owner | Closure condition |
| --- | --- | --- |
| Evaluated startup and excluded-route probe record | Release operator | Local candidate evidence `EV-768-20260816-STARTUP`, `EV-768-20260816-INVALID-CONFIG`, `EV-768-20260816-ARTIFACTS`, and `EV-768-20260816-ROUTES` is recorded in `evaluated-configuration.md`; release evidence still requires a clean commit identity and the #771-derived exact endpoint probes. |
| EAL4-style lifecycle and vulnerability evidence | Security/evaluation owner | Evaluator-approved evidence set, dependency review, and vulnerability-analysis result are linked to the release record. |
| Ingress operational evidence | Ingress owner | TLS termination, ACL, certificate lifecycle, and private-upstream configuration are recorded for the evaluated deployment. |
| Main-module operation evidence | Module/API owners | Checked matrix rows, endpoint metadata validation, and negative/positive authorization tests cover each composed module operation. |

The objective and exclusion mappings must be reviewed against #771's final endpoint inventory before release evidence is accepted. The current base-prefix route probes are not endpoint-absence evidence because the authorization fallback can return `401` for an unmatched protected prefix.

## Security target limitations and revision triggers

This Security Target must be revised before claiming any of the following: more than one machine or Main instance; database/cache replication or failover; multi-region placement; rolling or blue/green upgrades; cluster orchestration; ingress as part of the TOE; an included operating system, container runtime, database, cache, or storage product; an excluded module/provider/app; or an availability/integrity guarantee beyond the single-machine configuration.
