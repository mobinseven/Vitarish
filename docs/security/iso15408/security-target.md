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

| ID | Objective |
| --- | --- |
| `O.AUTH` | Identify and authenticate users before protected operations, including the configured password/passkey flows. |
| `O.ACCESS` | Enforce tenant, ownership, role, and platform-administrator access rules for protected objects and operations. |
| `O.INPUT` | Validate transport and use-case inputs and reject malformed, replayed, or unauthorized requests at the relevant boundary. |
| `O.AUDIT` | Generate attributable audit records for defined security-relevant actions and provide authorized, tenant-scoped review. |
| `O.INTEGRITY` | Preserve the integrity of security-relevant state through authorized application writes and protected key/cache integration, within the single-machine environmental assumptions. |
| `O.AVAIL` | Keep the evaluated single Main instance operational and fail closed when required environmental dependencies are unavailable; this objective has no multi-instance failover claim. |

### Environmental objectives

| ID | Objective |
| --- | --- |
| `OE.HOST` | Protect and patch the operating system, container runtime, host, volumes, network, and physical machine. |
| `OE.DATA` | Operate, authenticate, isolate, back up, and recover PostgreSQL, Redis, and persistent storage. |
| `OE.INGRESS` | Provide TLS, ingress routing, certificate lifecycle, upstream restrictions, and external availability at the deployment boundary. |
| `OE.OPERATE` | Protect secrets, deploy only the recorded evaluated configuration, and control release and operator access. |
| `OE.CLIENT` | Protect client credentials/tokens and use the TOE through the documented secure interface. |

## Security claims

The initial claim is the proposed SFR/SAR set in [`sfr-sar-matrix.md`](sfr-sar-matrix.md). The matrix is deliberately traceable to current implementation evidence and records exclusions where the repository does not support a defensible claim. It is not permission to infer claims for excluded apps, providers, infrastructure, or distributed operation.

The proposed assurance set is EAL2-shaped for initial evaluator review; no EAL or certification level is claimed until an evaluator confirms the applicable Common Criteria/ISO/IEC 15408 version, dependencies, operations, evidence, and evaluation scope.

## Security target limitations and revision triggers

This Security Target must be revised before claiming any of the following: more than one machine or Main instance; database/cache replication or failover; multi-region placement; rolling or blue/green upgrades; cluster orchestration; ingress as part of the TOE; an included operating system, container runtime, database, cache, or storage product; an excluded module/provider/app; or an availability/integrity guarantee beyond the single-machine configuration.
