# Vitarish Network Application Security Questionnaire: Implementation and Evidence Plan

**Status:** Revised plan; compatibility claims remain blocked until Stage 0 closes applicability and evaluator questions.
**Questionnaire source:** `راهنمای تکمیل پرسشنامه نرم_افزار.pdf`, 42 pages, questionnaire version 1.0 and completion-guide version 1.1.
**Target outcome:** The evaluated release satisfies every unconditional requirement and every conditional requirement triggered by its declared capabilities, with source, deterministic tests, evaluated configuration, and release evidence.
**Implementation authority:** This version-controlled document authorizes the ordered implementation slices after Stage 0 freezes the evaluated boundary. It does not authorize silently narrowing product scope to avoid a requirement.

**Profile relationship:** This plan creates the separate `vitarish-network-questionnaire-v1.0` evaluated profile. It does not widen, replace, or reuse evidence from the existing Main-only `vitarish-toe-main-evaluated-2026-08-15` profile under `docs/security/iso15408/`; that profile remains valid only for its declared single-machine Main/PostgreSQL/Redis boundary. A release claims exactly one profile.

Guide-facing screenshot capture, access-path writing, and screenshot redaction are owned by the separate [guide screenshot-collection plan](guide-evidence-collection-plan.md). This plan owns every other release-evidence artifact and the authoritative per-test evidence index. Authenticated E2E screenshots required here remain implementation feedback, not submission-evidence collection.

The questionnaire, completed responses, guide documents, screenshots, scans, manifests, and evidence bundle are controlled evaluation artifacts, not product inputs. Runtime code, migrations, deployment composition, and implementation tests must operate without them: they must not read, embed, serve, branch on, or log their contents. Stage 0 records each artifact's controlled location, version, and hash; Stage 9 validates those bindings outside the product runtime.

## Compatibility rule

Questionnaire compatibility is not inferred from a build, framework default, private network, or unchecked box. Completion requires all of the following:

1. Every unconditional row is implemented and selected.
2. Every conditional section triggered by an included protocol or capability is implemented and selected.
3. Every unselected conditional or optional row has a truthful applicability reason tied to the evaluated boundary.
4. Every selected option states its exact configured value; “secure,” “supported,” or “provided by the platform” is insufficient.
5. Code-owned behavior has deterministic proof. Environment-owned behavior has release-specific evidence. A control spanning both requires both.
6. The Persian submission, English working response, implementation, tests, release manifest, and deployed topology agree.

Stage 9 covers **every mandatory and triggered conditional row**, not merely every row someone chose to select.

## Outcome, scope, and non-goals

### Intended evaluated boundary

The target boundary is the full Vitarish network product delivered by this repository for the evaluated release:

- Caddy public ingress configuration, which is inside this profile's TOE;
- Main host and active server modules;
- repository-delivered browser applications used to exercise product capabilities;
- PostgreSQL, Redis, object/file storage, and enabled external providers as supporting dependencies;
- authentication, authorization, audit, security administration, user-data import/export, and background processing used by active capabilities.

Stage 0 freezes one reproducible deployment topology. For every component pair it states whether the pair is co-located or communicates across machines. A separate-machine link is supported only with an authenticated confidentiality-and-integrity channel. “Private network” alone is not secure-channel proof.

The public CA, DNS provider, host administration, backup execution, external alert receiver, and operator incident-response process are environmental dependencies. Their configuration and execution remain required release evidence where they support a selected control. Exclusion from the product boundary does not permit the product to omit its side of a control, such as emitting a capacity alert or validating a peer certificate.

If an Android or iOS product is included, pause and add the separate application protection-profile questionnaire required by the guide.

### Non-goals

- No certification, EAL, HA, multi-region, backup-execution, incident-response, or alert-delivery claim without independently verifiable evidence.
- No automatic update subsystem. The evaluated update mechanism is authorized manual deployment unless Stage 0 records a different need.
- No SSH product interface. Operator SSH is an environmental control and is not selected as a product capability.
- No mTLS claim unless an active evaluated flow requires it.
- No destructive contract/data change; use expand-migrate-contract, forward-only migrations, and explicit compatibility windows.
- No generic repository, mediator framework, second authorization system, custom UI system, or centralization of module business rules.
- No secret, credential, private key, token, audit payload, or patient-data exposure in evidence.

## Authoritative ownership

| Concern | Authoritative owner | Required boundary |
| --- | --- | --- |
| Evaluated topology, ingress, trusted proxies, CORS, host health, public TLS | Hosting/Main under `src/Hosts` | Composition and transport only; no module business rules. |
| Password, failed-attempt response, authentication realm, mechanisms, sessions, login history, notices, access policy | Authentication and Users | Domain/Application own rules; Identity, EF, Redis, cookies, and UI are adapters. Client-supplied tenant data never selects a pre-authentication realm. |
| Audit event contract, append integrity, retention, capacity state, emergency path, review | Audit library | Audit is source of truth; APIs/UI expose authorized DTOs only. |
| Audit/data completeness checkpoint protocol | Audit library and owning module Application | They define the signed checkpoint payload and verification rules through application-owned ports; the release operator operates the independent append-only witness and cannot redefine event or data meaning. |
| Tenant/resource authorization and data meaning | Owning module Application | Transport metadata never grants access. |
| File bytes, checksums, quarantine, verified references | Files | Consumers do not reimplement file-integrity decisions. |
| Sensitive non-file integrity projection | Owning module Application | Shared infrastructure stores/verifies manifests but does not define canonical business data. |
| Transfer size/type/format rules | Owning module, with global transport ceilings in Hosting | Module values may be stricter, never broader. |
| Internal and outbound TLS policy | Hosting for shared clients; owning integration for endpoint identity | Native certificate validation is mandatory; no per-client bypass. |
| Browser CSRF lifecycle and framework-registration input | Authentication | Authentication owns protected-flow policy, issuance, rotation, invalidation, replay semantics, and the **single framework-registration input**. Hosting composes that input exactly once. Base remains framework-only generic endpoint middleware driven solely by endpoint metadata; Base must not reference Authentication contracts or own a token/cookie lifecycle. |
| Trusted wall clock | Evaluated host/operator; Application consumes an owned clock port | Duration logic uses monotonic time where available; persisted audit time is UTC. |
| Release manifest, evidence index, attestation, and revocation record | Release operator | The canonical evidence bundle is in write-once/immutable storage. A release-only signing key held outside that bundle signs its manifest and evidence index; verification uses public material and is fail-closed. |

Domain remains independent of UI, API, EF, HTTP, storage, Redis, Caddy, and vendors. Application depends only on Domain and Application-owned ports. Contracts own HTTP shapes, routes, operation IDs, and exposed security values. Generated clients change only through their source and canonical generator.

## Fixed evaluated baseline

Administrative settings may be stricter but may not weaken these values.

| Policy | Evaluated default and hard bound |
| --- | --- |
| Password | Minimum 8 characters; upper-case, lower-case, numeric, and special character required. Configured length may increase, never fall below 8. |
| Failed authentication threshold | 5 attempts for every identifier, including unknown identifiers. A stricter positive value may be configured. |
| Failed authentication response | 15-minute denial by default; known and unknown identifiers receive indistinguishable behavior and responses. |
| Idle browser session | Default 30 minutes. Authorized administrators may configure global or per-user values from 1 through 30 minutes. |
| Concurrent browser sessions | Default 1. Administrators may permit 2 or 3 only for a recorded functional need. Every new parallel session creates a durable notice for previously active sessions; at the maximum, policy explicitly rejects or replaces the oldest. |
| Security-attribute changes | Password, role, authentication method, allowed-IP, and material access-policy changes revoke affected sessions before confirmation. |
| Roles | In the evaluated configuration, exactly one role per user per tenant. An authorized assignment atomically replaces the existing tenant-role membership, audits the replacement, and revokes affected sessions. Tenant/subject/resource checks still apply. |
| Evaluated TLS protocol set | TLS 1.2 only for every **TLS-enabled evaluated connection**. TLS 1.3 may be enabled, selected, or claimed only after Stage 0 records written evaluator approval of its use and exact suites. SSL 1.0/2.0/3.0 and TLS 1.0/1.1 are always rejected. Used suites and key/curve parameters must be selectable and scan-proven. Every separate-machine link instead requires authenticated confidentiality and integrity; a same-machine exception requires frozen topology proof and fails when the link can be configured remotely. |
| Audit IP | Normalized source IP remains with each retained audit event. No independent redaction unless the evaluator approves a documented representation. |
| Audit retention | Stage 0 sets the period. Expiry is authorized, segmented, auditable, and distinguishable from unauthorized deletion. |

## Requirement closure matrix

These stable plan IDs map to the questionnaire rows. Stage 0 adds exact Persian text, selected options, and applicability to the response artifacts.

### Unconditional core requirements

| ID | Row | Required closure and proof |
| --- | --- | --- |
| AU-1 | 2-1.1 | Enumerated event catalog covers every applicable mandatory event on pages 5–6, with owner, start/outcome semantics, redaction, and success/failure tests. An unsupported optional underlying function receives a row-specific rationale. |
| AU-2 | 2-1.2 | Every audit event contains UTC date/time, type, actor identity when known, outcome, and normalized source IP. |
| AU-3 | 2-1.3 | Named capabilities protect audit reads; direct store access is an operator boundary. Every allowed/denied review is audited. |
| AU-4 | 2-1.4 | Audit DTOs are readable, typed, relevant, and free of opaque payload dumps; contract tests reject missing/invalid fields. |
| AU-5 | 2-1.5 | Authorized review filters and sorts by actor, UTC date/time, and event type at minimum; tenant scope is server-enforced. |
| AU-6 | 2-1.6 | Tenant-local sequence and keyed chain detect changed/deleted events; keys remain outside the audit database and old key IDs remain verifiable. |
| AU-7 | 2-1.7 | Configurable warning/critical capacity thresholds create durable in-product notices and an operator alert event. Release evidence proves external delivery. |
| AU-8 | 2-1.8 | Audit-store failure/exhaustion enters restricted mode, writes a critical event through the Stage-0-frozen protected bounded emergency audit journal, and fails closed for mutations requiring audit. Recovery/reconciliation are tested. |
| CR-1 | 2-2.1 | Crypto inventory identifies every AES use, mode, key length, library, and purpose. At least one permitted AES mode/length is present and evidenced. |
| CR-2 | 2-2.2 | Inventory identifies every hash/HMAC/password-hash use. SHA-256/384/512 selections have exact proof; SHA-1 is not claimed absent separately approved protocol need. |
| IA-1 | 2-3.1 | A fixed or administrator-managed positive failed-attempt threshold applies at every authentication entry point, including unknown identifiers. |
| IA-2 | 2-3.2 | Reaching the threshold causes the declared 15-minute denial response; known and unknown identifiers remain indistinguishable in response and duration. |
| IA-3 | 2-3.3 | User security state includes identifier, authentication methods/data references, account state, and role/capability assignments without exposing secrets. |
| IA-4 | 2-3.4 | Creation, invitation, password change, reset, and administrative reset share configurable password rules. |
| IA-5 | 2-3.5 | Stage 0 selects exact pre-authentication actions; architecture tests reject undeclared anonymous actions. |
| IA-6 | 2-3.6 | Evaluated remote access enables username/password plus at least one independent mechanism such as OTP or passkey. Do not claim MFA unless multiple factors are required in one authentication. |
| IA-7 | 2-3.7 | Active security state contains user ID, roles/capabilities, and successful/failed authentication history. |
| IA-8 | 2-3.8 | Default single-session behavior invalidates the previous session. If parallel sessions are enabled for a recorded need, every new parallel session creates a durable notice for existing sessions. |
| IA-9 | 2-3.9 | Changes to active security attributes revoke affected sessions so old attributes cannot remain effective. |
| UD-1 | 2-4.1 | Executable authorization matrix covers system administrators, ordinary users, protected object categories, and create/read/change/delete/metadata operations as applicable. |
| UD-2 | 2-4.2 | Access policy uses authorized roles/capabilities plus server-trusted session/request attributes; client-supplied ownership or tenant values are never authoritative. |
| UD-3 | 2-4.3 | Owning Application use cases authorize every active-subject/protected-object operation before access. Direct HTTP tests prove denial. |
| UD-4 | 2-4.4 | Access is denied when the effective concurrent-session bound is exceeded; this rule shares the Authentication-owned session source of truth. |
| UD-5 | 2-4.5 | Sensitive temporary files are securely removed, rented sensitive byte buffers are cleared, storage reuse cannot reveal prior content, and isolation tests cover resource reuse. Managed-string limitations are documented. |
| UD-6 | 2-4.6 | Every import/upload declares data type, maximum size, format/media type, authorization owner, and applicable frequency/rate rule before provider use. |
| UD-7 | 2-4.7 | User data crosses an external or separate-machine boundary only through an identified authenticated confidentiality-and-integrity channel. |
| UD-8 | 2-4.8 | Every export/download applies module-owned authorization and explicit type/size/format policy before data leaves the product. |
| UD-9 | 2-4.9 | Named administrative capabilities manage export restrictions so purposeless or unrestricted bulk export is impossible. |
| UD-10 | 2-4.10 | Stored sensitive files and canonical non-file projections have keyed integrity verification that detects unauthorized change or deletion. |
| UD-11 | 2-4.11 | Integrity mismatch blocks sensitive use/export and creates a durable warning for authorized roles plus a safe remediation/restore path. |
| SM-1 | 2-5.1 | Named capabilities control supported security-function behavior, including determine/change/enable/disable operations where applicable. |
| SM-2 | 2-5.2 | Named capabilities restrict query/change/delete/default operations over active-user security attributes selected under IA-7. |
| SM-3 | 2-5.3 | Named capabilities restrict supported create/read/change/delete/default operations over product data; no generic administrator bypass exists. |
| SM-4 | 2-5.4 | The exhaustive management checklist below is implemented or has the guide-permitted row-specific rationale for an unsupported optional underlying function. |
| SM-5 | 2-5.5 | Authorized administrators can define the evaluated roles/capabilities and their permitted product operations. |
| SM-6 | 2-5.6 | Users can be assigned one evaluated role per tenant. The authorized assignment use case atomically replaces the existing tenant-role membership, audits it, revokes affected sessions, and handles concurrent assignments without leaving more than one effective role. Independent tenant/resource checks still apply. |
| TSF-1 | 2-6.1 | Software and hardware/dependency failure preserve authorization, isolation, audit guarantees, and data integrity; errors reveal no internals. Recovery evidence covers restart and restore. |
| TSF-2 | 2-6.2 | Separate-machine links use authenticated encryption. Same-machine satisfaction requires proof of co-location and rejection of unsupported remote configuration. |
| TSF-4 | 2-6.4 | Security time uses UTC from the evaluated OS clock; synchronization and change protection are release evidence. Duration logic resists wall-clock jumps. |
| TSF-5 | 2-6.5 | Authorized operators can apply a manual update identified by commit and immutable digest; preflight, migration, health, rollback limits, and provenance are documented. |
| RA-1 | 2-7.1 | Stage 0 defines core functions and accepted degraded behavior for software failures. Restart, bounded retry, safe recovery/read-only behavior, and consistency are proven. If the evaluator requires uninterrupted HA, pause and add redundancy. |
| AC-1 | 2-8.1 | Server transactionally enforces the configured per-user concurrent-session maximum. |
| AC-2 | 2-8.2 | Administrators manage global and per-user idle time within safe bounds; all remote interactive sessions terminate after the effective interval. |
| AC-3 | 2-8.3 | The initiating user can sign out; server-side revocation completes before success. |
| AC-4 | 2-8.4 | After authentication, UI shows the last successful session attempt with date and time. |
| AC-5 | 2-8.5 | UI shows the last failed session attempt and the number of failures since the previous success, with date and time. |
| AC-6 | 2-8.6 | Access history remains visible until explicit acknowledgement or remains continuously available in a stable account surface. |
| AC-7 | 2-8.7 | Administrators may restrict session creation by normalized client CIDR; rules apply only after trusted-proxy normalization and are audited. |
| TC-1 | 2-9.1 | HTTPS is selected as the logically distinct trusted channel and protects authentication, confidentiality, integrity, and change detection. |
| TC-2 | 2-9.2 | Valid users and external IT products can initiate supported remote communication only through the trusted channel. |
| TC-3 | 2-9.3 | Initial user authentication is impossible over plaintext transport. |
| VULN-1 | `nsmail-1` component/version test | A generated, release-bound inventory identifies every enabled framework, package, runtime, container image, operating-system base image, web server, database, cache, browser dependency, and provider with exact resolved version or digest and vulnerability disposition. |
| VULN-2 | `nsmail-1` header test | Public ingress enforces an exact, route-aware response-header policy: HSTS, cache policy for sensitive responses, `Pragma`, `X-Content-Type-Options`, framing protection, CSP, referrer policy, and permissions policy. The Stage 0 evaluator decision records the disposition for obsolete `X-XSS-Protection`. |
| VULN-3 | `nsmail-1` cookie test | A cookie inventory declares owner, purpose, name, domain/path, lifetime, persistence, `SameSite`, `Secure`, `HttpOnly`, and host-only behavior. Cookies are host-only unless an approved cross-subdomain need exists; use the `__Host-` prefix where compatible. |
| VULN-4 | `nsmail-1` open-port test | A deployment port allowlist identifies every public, private, health, diagnostic, and development-only listener. Undeclared public listeners and published state-service ports block release. |
| VULN-5 | `nsmail-1` unsafe-method test | Each route has an explicit HTTP-method allowlist. `TRACE`, `TRACK`, unexpected `CONNECT`, unsupported verbs, and unsafe method-override headers receive non-revealing `4xx`; `OPTIONS` is limited to valid CORS preflight. |
| VULN-6 | `nsmail-1` sensitive-form test | The browser CSRF contract defines protected authentication modes, issuance, expiry/rotation, invalidation, replay behavior, CORS interaction, and rejection of missing, stale, cross-user, cross-origin, and replayed tokens. |

### Conditionally triggered requirements

| ID | Trigger | Required closure |
| --- | --- | --- |
| HTTPS-1 | HTTPS is selected, therefore mandatory | Public ingress implements the questionnaire-declared HTTPS protocol behavior. |
| HTTPS-2 | HTTPS is selected, therefore mandatory | HTTPS is implemented only over TLS; SSL is rejected. |
| HTTPS-3 | Product acts as HTTPS client | Invalid peer certificates do not establish a connection or receive a bypass; validation follows CERT-1–3. |
| TLSS-1 | Public ingress acts as TLS server, therefore mandatory | Support the declared evaluated TLS protocol set: TLS 1.2 is mandatory; TLS 1.3 is permitted only after the Stage 0 written evaluator decision. Inventory and select the actual questionnaire-listed suites used by the release. |
| TLSS-2 | Public ingress acts as TLS server, therefore mandatory | Reject SSL 1.0/2.0/3.0 and TLS 1.0/1.1. |
| TLSS-3 | Public ingress acts as TLS server, therefore mandatory | State and prove exact RSA key length and/or approved ECDH curve and DH size corresponding to the selected suites. |
| TLSC-1 | Any enabled module calls external HTTPS/TLS | Use the declared evaluated TLS protocol set: TLS 1.2 is mandatory; TLS 1.3 is permitted only after the Stage 0 written evaluator decision. Inventory the actual permitted suite(s). Include mail, SMS, storage, identity, payment, and provider clients. |
| TLSC-2 | Any enabled module calls external HTTPS/TLS | Apply RFC 6125 reference-identity/hostname validation. |
| TLSC-3 | Any enabled module calls external HTTPS/TLS | Reject invalid server certificates without user, administrator, or callback bypass. |
| TLSC-4 | An outbound suite uses ECDH/ECDHE | Offer only the approved NIST curves selected in the questionnaire. |
| TLSB-1 | Certificate-based client authentication/mTLS is selected | Use X.509v3 mutual TLS authentication. Otherwise record the no-mTLS applicability reason. |
| TLSB-2 | Product authenticates TLS clients by certificate | Reject a client when its distinguished name or SAN does not match the expected identity. |
| CERT-1 | HTTPS/TLS certificate validation is used | Validate RFC 5280 paths to a trusted CA, require `basicConstraints CA=TRUE`, document path-length support, revocation method, and applicable EKUs. |
| CERT-2 | HTTPS/TLS certificate validation is used | Accept a CA certificate only when `basicConstraints` is present and `CA=TRUE`. |
| CERT-3 | HTTPS/TLS or conditional signing is selected | Use X.509v3 for the selected purpose; require Server Authentication and conditional Client Authentication, OCSP Signing, or Code Signing EKUs. |
| SSH-1 | SSH becomes a product interface | Implement the listed SSH RFCs; currently not applicable because operator SSH is not product support. |
| SSH-2 | SSH becomes a product interface | Support both public-key and password authentication as required by the questionnaire. |
| SSH-3 | SSH becomes a product interface | Declare and enforce the maximum accepted SSH packet size. |
| SSH-4 | SSH becomes a product interface | Select and restrict encryption to at least one questionnaire-permitted algorithm. |
| SSH-5 | SSH becomes a product interface | Select and restrict host/public-key use to at least one permitted algorithm. |
| SSH-6 | SSH becomes a product interface | Select and restrict integrity/MAC use to at least one permitted algorithm. |
| SSH-7 | SSH becomes a product interface | Select and restrict key exchange to at least one permitted algorithm. |
| SSH-8 | SSH becomes a product interface | Rekey before either one hour or 1 GiB is exceeded. |
| SSH-9 | SSH becomes a product interface | SSH clients authenticate servers against an approved local host-key trust store. |
| CR-3 | Product generates cryptographic keys | Inventory generation and secure destruction. TLS/platform key generation must be accounted for rather than assumed away. |
| CR-4 / TSF-6 | Product signs data or performs automatic/signed-file updates | Implement approved algorithm/key length and certificate EKU rules. Manual immutable-image deployment does not create an automatic-update claim. |
| TSF-3 | Product shares security data with another secure IT product | Map shared authentication data, keys, signatures, or audit data and prove compatible interpretation; otherwise record non-applicability. |

### Exhaustive SM-4 management checklist

Assign and test every applicable operation below:

- audit-review group membership and permissions;
- audit capacity thresholds, warning routes, failure behavior, and retention;
- criteria granting or denying access to protected objects;
- residual-resource protection behavior where configurable;
- import type, size, format, and rate rules;
- integrity-failure response and remediation;
- failed-attempt threshold and denial response;
- password complexity and minimum length;
- user authentication data and permitted pre-authentication actions;
- enabled authentication mechanisms and rules;
- pre-authentication/session restrictions such as allowed CIDRs;
- default active-user attributes and access-control values;
- role/capability definitions and assignments;
- maximum sessions and replacement/rejection policy;
- session-start restrictions;
- global and per-user inactivity limits.

Each mutation requires server authorization, validation against hard bounds, optimistic concurrency where competing updates are possible, and allowed/denied audit evidence.

## Ordered implementation stages

### Stage 0 — Applicability, TOE, and evaluator decision gate

**Depends on:** none.
**Outcome:** One authoritative matrix and reproducible topology exist before behavior changes.

- Copy every questionnaire row/option into a versioned matrix with plan ID, status, trigger, owner, selected value, implementation, test, release evidence, and disposition. At this gate, `proof` means an assigned proof path, not passing implementation evidence.
- Create the new authoritative profile set under `docs/security/network-questionnaire-v1.0/`: `evaluated-configuration.md`, `security-target.md`, `toe-inventory.md`, `topology.md`, and `requirements-matrix.md`. The existing `docs/security/iso15408/` documents remain the authority for the Main-only profile; add a cross-link that states the two profiles are separate rather than editing their scope to imply equivalence.
- Create `src/Hosts/docker-compose.network-questionnaire-v1.0.yml` as the versioned deployment manifest for this profile. Freeze its included Caddy/ingress, hosts, modules, browser apps, storage, providers, authentication methods, transfer routes, and administrative surfaces; every repository capability not listed is disabled or has an explicit non-applicability disposition. The evaluated remote-authentication profile always includes username/password and exactly one independently operated second mechanism selected from OTP or passkey; neither flow is a fallback, alias, or silent substitution for the other.
- Record Caddy as inside this profile's TOE. Record each included browser app, PostgreSQL, Redis, object storage, and enabled provider as either inside the TOE or an environmental dependency. If a component is environmental, the profile records the product-side control and the release evidence required from its operator; it is not silently treated as inside the TOE.
- Produce a data-flow diagram with trust boundaries, placement, protocols, authentication, and transferred user/security data. For every runtime link, freeze either (a) authenticated confidentiality and integrity across separate machines, or (b) a same-machine proof comprising component placement, non-routable endpoint, and configuration controls showing the link cannot be remotely configured; private networks and unpublished ports alone are insufficient.
- Record exact product, OS image, web server, database, Redis, Caddy, runtime, and language versions from the release mechanism.
- Resolve with the evaluator: audit-IP retention/redaction; audit-full behavior; whether RA-1 requires HA; whether TLS 1.3 is permitted despite the questionnaire's TLS 1.2 wording and, if so, the exact permitted suites; and revocation expectations. Caddy is fixed as inside the TOE. Until the TLS decision is written, the evaluated protocol set is TLS 1.2 only for every TLS-enabled evaluated connection.
- Resolve with the evaluator: the guide's obsolete `X-XSS-Protection` expectation and whether its requested per-request CSRF token expiry/regeneration requires a server-owned nonce/replay mechanism or accepts a bounded reusable antiforgery-token design. Do not infer either result from framework defaults.
- Freeze the Audit-owned protected bounded emergency audit journal: storage and principal separately protected from the primary audit store and runtime database principal; bounded capacity and explicit overflow behavior; encryption and integrity protection; writer and authorized-reader identities; retention; reconciliation into the primary audit chain; relationship to the independent witness; and the precise fail-closed/restricted state. Stage 5 may implement this selected design only and may not introduce a substitute sink, store, or witness.
- Select the independently protected append-only witness for audit and sensitive-data completeness checkpoints. Record its operator, immutability/retention guarantee, authenticated channel and identity validation, signing/key custody, checkpoint cadence and maximum staleness, database-operator retention authority, reconciliation procedure, and the safe response when the witness is unavailable.
- For each enabled outbound client, record its endpoint identity, owner, enforcement plane (Caddy, runtime/OS, or provider), TLS-enabled/same-machine disposition, protocol/suite/curve disposition, certificate-chain and revocation behavior, and versioned positive/negative probe. A client without this record blocks Stage 1; a remotely configurable same-machine link is treated as a separate-machine link.
- Freeze release-evidence custody: canonical immutable/write-once storage, a distinct immutable unsigned-candidate location and signed-bundle location, canonical serialization and SHA-256 payload-hash rules for the manifest and evidence index, detached-signature format, release-only signing key outside every bundle, public verification material, signing-key ID and custody, issuer, profile identity, topology identity, amendment/supersession format, append-only signed revocation format, and retention/access controls. Signing must never rewrite a candidate manifest, index, report, or screenshot; it creates a separate signed attestation over the exact candidate payload hashes.
- Before role enforcement is designed, run a read-only production-equivalent membership analysis for every tenant-role naming pattern and record the query, snapshot identity, result, conflicting memberships, global-role disposition, and owner-approved migration decision. Do not assert that no conflicting data exists from source inspection alone.
- Add revision triggers for new endpoints, providers, distributed components, mobile clients, authentication methods, transfers, SSH, automatic update, mTLS, crypto primitives, emergency-journal dependencies, or evidence-custody dependencies.
- Implement the read-only `src/Hosts/scripts/Test-NetworkQuestionnairePlanIntegrity.ps1` lint before its gate below. It consumes only this version-controlled plan (including its versioned release-command contract) and the Stage-0 matrix and topology; it performs no deployment, product-runtime, or release-evidence mutation. Stage 2 may extend its checks only after this Stage-0 contract remains passing.

**Pre-implementation integrity-lint gate:** before Stage 1 or any behavior change, run the plan's integrity lint. It traces every mandatory or triggered requirement to one owner, frozen topology disposition, implementation slice, deterministic command, expected artifact, evidence-integrity protection, and release-blocking failure. It rejects contradictory TLS wording, cross-module dependency inversion (including Base referencing Authentication CSRF contracts), unselected emergency dependencies, and evaluator decisions unspecified after Stage 0 closes.

**Acceptance:** every mandatory row has an owner and planned proof path; every declared conditional trigger and evaluator interpretation has an owner and closure; the integrity lint passes; the new profile documents and deployment manifest agree on the declared included/excluded components. Stage 1 must mechanically reconcile that declaration with executable behavior; a newly discovered endpoint, client, provider, listener, or conditional trigger reopens Stage 0 and blocks later stages until it is disposed. The Main-only profile identity, compose file, topology, and evidence are rejected as proof for this profile. Current main-compose exposure and the currently missing Caddyfile are not compliant evidence. An incompatible mandatory control causes architecture revision before Stage 1.

### Stage 1 — Mechanically checked inventory and baseline

**Depends on:** Stage 0.
**Outcome:** Current behavior and security-sensitive surfaces are mechanically discoverable.

- Inventory protected, anonymous, binary/multipart, transfer, authentication, audit, management, update, and health endpoints.
- Inventory persisted user-data owners, sensitive categories, temporary/buffer paths, providers, checksums, crypto, TLS clients, and network links.
- Record routes, operation IDs, generated consumers, cookies, migrations, audit rows, roles, configuration keys, and failure behavior.
- Generate the VULN-1 component/version inventory from resolved package locks, published artifacts, container image digests, and enabled composition; scan it for known vulnerabilities and require an approved disposition for every finding.
- Inventory actual listeners, published ports, route methods, CORS preflight behavior, response classes, and every cookie emitted by public flows.
- Reject endpoints lacking authentication profile, applicable anonymous justification, audit disposition, or user-data transfer classification.
- Reject enabled providers/links lacking TLS and peer-validation disposition.
- Compare this generated inventory with the Stage 0 declaration. Any mismatch is a Stage 0 revision trigger, not an implicit exclusion or a Stage 1-only exception.

**Acceptance:** inventories reconcile with executable mappings and deployment composition; baseline tests capture behavior; generated output is untouched.

### Stage 2 — Transport, cryptography, time, and update boundary

**Depends on:** Stage 1.
**Outcome:** Public, internal, and outbound trust boundaries are reproducible.

- Add the missing `src/Hosts/caddy/Caddyfile`; configure HTTP redirect, the declared evaluated TLS protocol set, permitted server cipher/key/curve behavior, a versioned response-header configuration surface, and private Main proxying. Pin Caddy image version/digest. Stage 2A finalizes and tests the exact response-header policy through that surface.
- While the TLS 1.2-only baseline applies, set Caddy's `:443` server protocols explicitly to `h1 h2`, omit `h3`, and configure TLS minimum and maximum as TLS 1.2. The network-profile compose manifest publishes TCP 80/443 only; it does not publish UDP 443/QUIC. TLS 1.3 or HTTP/3 requires the Stage 0 written evaluator decision, an updated profile baseline, and new protocol/suite/port evidence before either is enabled.
- Parameterize hostnames and DNS/certificate credentials through environment/secret-store references; never commit credentials.
- Align trusted proxies/forwarded headers with the Caddy network; reject untrusted forwarded headers.
- Give each separate-machine internal link certificate-authenticated TLS or another approved secure protocol. A same-machine-only exception is valid only when its Stage-0 placement and non-remote-configurability proof still holds at release; otherwise it is a separate-machine link.
- Centralize safe outbound defaults without hiding provider identity: for every TLS-enabled outbound client, enforce TLS 1.2, native RFC 5280 validation, RFC 6125 hostname validation, no invalid-certificate bypass, explicit timeout/cancellation, the Stage-0-selected revocation behavior, and the recorded enforcement plane. The client-specific positive/negative probe proves the selected protocol/suite/curve disposition and certificate/revocation failure behavior; Caddy, runtime/OS, and provider controls are not interchangeable claims.
- Build the crypto inventory: algorithm, mode, length, purpose, library, key owner, rotation/destruction, and questionnaire selection.
- Use evaluated OS time for wall clock and monotonic elapsed time for durations where feasible; document synchronization and clock-jump behavior.
- Define authorized manual update: immutable identity, provenance, preflight, migration order, health verification, rollback constraints, and audit/release record.

**Acceptance:** checks reject missing authenticated confidentiality/integrity on separate-machine links, a remotely configurable same-machine exception, public Main exposure, unsafe certificate callbacks, undeclared TLS clients, `h3` in adapted Caddy configuration, or UDP 443 in the evaluated topology. TLS 1.2 and HTTP/2-over-TLS succeed; TLS 1.3 and HTTP/3-only probes fail without fallback. Every outbound-client disposition has passing versioned positive/negative probes. Disposable TLS smoke tests and scan assertions pass.

### Stage 2A — Vulnerability, cookie, method, and CSRF closure

**Depends on:** Stages 0–2.
**Outcome:** The guide's installation and vulnerability checks are executable product and deployment behavior, not a documentation exercise.

- Define the public response-header policy in versioned Caddy/Main configuration. Apply `Strict-Transport-Security`, sensitive-response `Cache-Control` and `Pragma`, `X-Content-Type-Options: nosniff`, framing protection, CSP, `Referrer-Policy`, and `Permissions-Policy`; route exceptions require a named owner and test. Implement or document the evaluator-approved disposition for `X-XSS-Protection`.
- Make the cookie inventory authoritative. Require `Secure` and appropriate `HttpOnly`/`SameSite` values; omit `Domain` for host-only cookies unless an approved cross-subdomain flow requires it; use `__Host-` names where compatible. Do not infer cookie properties from one authentication cookie.
- Define a route-method manifest at the endpoint owner. Enforce it at transport without moving module authorization into Hosting. Reject `TRACE`, `TRACK`, unexpected `CONNECT`, unsupported verbs, and unsafe method overrides with a non-revealing `4xx`; permit `OPTIONS` only for valid CORS preflight.
- Define the browser CSRF lifecycle: which authenticated browser mutations require antiforgery; how tokens are issued, expired/rotated, invalidated after login/logout/session replacement/security-attribute changes, and rejected on replay or origin/user mismatch. If Stage 0 requires one-time request tokens, add the smallest server-owned nonce/replay owner; otherwise retain only the evaluator-approved bounded-token behavior.
- Consolidate the existing duplicated antiforgery registration behind Authentication's single framework-registration input. Hosting composes that input once. Base remains framework-only generic endpoint middleware driven by endpoint metadata and has no reference to an Authentication contract; neither Base nor Hosting owns, defines, or duplicates a token/cookie lifecycle. Add an architecture/dependency-direction test proving this boundary.
- Add a deployment port manifest and a running-topology check. Declare Caddy TCP 80/443 public listeners, no UDP 443 listener while TLS 1.2-only applies, the private Main upstream, state-service listeners, health endpoints, and development-only ports. Fail evaluated deployment when a port is undeclared, Main bypasses ingress publicly, PostgreSQL/Redis are host-published, or QUIC/HTTP/3 is exposed without the Stage 0 TLS 1.3 decision.
- Generate sanitized, release-bound component/version, header, cookie, port, method, and CSRF reports. Never include secrets, raw cookies, tokens, or sensitive payloads.

**Acceptance:** public ingress tests prove every declared header/cookie invariant; a runtime port scan matches the manifest; all forbidden methods return `4xx`; only valid CORS preflight uses `OPTIONS`; and protected mutations reject missing, stale, cross-user, cross-origin, and replayed CSRF tokens according to the Stage 0 decision.

### Stage 3 — Identity-owned access policy and authentication defense

**Depends on:** Stage 2A.
**Outcome:** One authority owns password, failed-attempt, authentication-method, and pre-authentication rules.

- Add a persisted `SecurityAccessPolicy`, Application read port, optimistic concurrency, and named administrator commands.
- Include password rules, failure threshold/response, enabled mechanisms, pre-auth actions, session bounds, global/per-user idle bounds, parallel-session policy, and allowed-IP policy where cohesive; do not create a generic settings bag.
- Validate hard bounds and audit allowed/denied updates with policy version.
- Apply one password validator to creation, invitation, change, reset, and administrative reset; remove conflicting checks.
- Define an Authentication-owned `AuthenticationRealmId` selected only from Authentication configuration or a validated trusted-host resolver before Authentication runs. A body, query, header, cookie, or client-supplied tenant value never selects the realm or partitions attempt state.
- Track known and unknown identifiers by `(AuthenticationRealmId, protected normalized identifier)`, where the identifier is normalized and retained as a keyed HMAC/peppered derivation rather than raw unknown credentials. Known-user lookup occurs only within that realm. Preserve indistinguishable thresholds, delay class, responses, and audit outcomes.
- Use the Host-owned normalized client IP only as a separate abuse/rate-limit dimension, not as part of the credential-lockout key. If tenant-local credentials are a current requirement, permit tenant scope only after trusted host-to-tenant resolution; invalid or unknown hosts use the generic realm and the same non-disclosing result.
- Implement username/password as a mandatory evaluated remote-authentication mechanism and exactly one Stage-0-selected, independently operated OTP or passkey mechanism. Prove password flow, second-mechanism flow, and both negative cases: password cannot silently become the second mechanism and the second mechanism cannot silently substitute for password. Do not claim MFA unless one evaluated flow requires multiple factors.

**Contracts/migration:** additive policy contracts and forward-only policy/attempt migration; persist realm identity and protected identifier derivation/key ID without rewriting credentials. Seed baseline and define existing-attempt expiry/mixed-version behavior.

**Acceptance:** IA requirements pass, policy cannot weaken baseline, unknown-user behavior is indistinguishable, tenant body/query/header/cookie changes cannot partition an attempt counter, spoofed forwarded headers cannot select the trusted-IP limiter, and anonymous mappings equal declared pre-auth actions.

### Stage 4 — Durable sessions, history, and restrictions

**Depends on:** Stage 3.
**Outcome:** Server-owned state implements IA-8/9 and AC-1–7 exactly.

- Persist opaque session ID, user, tenant, mechanism, issued/last-seen/revoked time, normalized IP, revocation reason, notice state, and concurrency version.
- Validate sessions on every protected cookie request; missing, revoked, expired, wrong-tenant, disallowed-IP, or policy-violating sessions fail closed.
- Default to one session. If 2 or 3 are enabled, notify every existing session on each new parallel session and apply reject/replace transactionally at the maximum.
- Resolve global/per-user idle timeout within 1–30 minutes. Cookie lifetime never outlives server validity.
- Revoke sessions after password, role, authentication-method, allowed-IP, or material policy changes.
- Persist last success/failure, failures since prior success, and acknowledgement; display date/time until acknowledgement or in a stable account surface.
- Apply CIDR allowlists only after trusted-proxy normalization.

**Contracts/migration:** additive session/history/notice/IP-range contracts; regenerate clients; forward-only migrations; invalidate legacy session-ID-less cookies once.

**Acceptance:** single/parallel/max paths, every-new-session notice, idle expiry, sign-out, attribute revocation, history acknowledgement, cross-tenant denial, and CIDR denial pass deterministic and real E2E tests.

### Stage 5 — Complete audit, capacity behavior, and review

**Depends on:** Stages 3–4.
**Outcome:** AU-1–8 are product behavior; external delivery remains separate evidence.

- Define a typed catalog for every applicable mandatory event on pages 5–6, including required fields, start/outcome, and redaction; never store request-body dumps.
- Audit owns named capabilities for audit review/read operations and enforces them before query execution; add direct allowed/denied review tests and audit both outcomes. Stage 7 governs assignment of those capabilities, not the Audit-owned review decision.
- Add normalized IP, category, tenant-local sequence, previous digest, HMAC digest, and key ID.
- Append event and chain head transactionally; retry only bounded concurrency conflicts at the audit boundary. Required mutation audit failure is fail-closed. Application/API/UI/background layers expose no audit-event deletion, purge, or retention operation.
- Keep integrity keys outside PostgreSQL and preserve old-key verification after rotation. The runtime database principal may select/insert audit state only; it cannot update, delete, truncate, or alter audit tables. Schema and approved retention use separately controlled database-operator principals.
- Publish an append-only, independently protected witness checkpoint at a bounded interval and immediately before retention. Each checkpoint contains tenant, checkpoint epoch, covered sequence range, chain head digest, key ID, timestamp, and prior witness reference; the witness retains history and accepts no rewind. Verification recomputes the local chain and compares it with the latest witnessed checkpoint, so a deleted or rewound tail is detected. Checkpoint publication failure becomes durable critical state; after the declared maximum staleness, audit-required mutations fail closed until ordered reconciliation confirms every missing checkpoint.
- Verify changed records, deleted sequences, invalid HMACs, missing/rewound witness checkpoints, and unauthorized retention gaps on schedule; verification never repairs evidence automatically.
- Define warning/critical thresholds; produce durable authorized notices and external alert event/outbox; prove external delivery separately.
- Implement only the Stage-0-frozen Audit-owned protected bounded emergency audit journal, its separately protected storage/principal, encryption/integrity, authorized writer/readers, capacity/overflow behavior, retention, restricted fail-closed state, witness relationship, and reconciliation into the primary chain. Do not silently discard mandatory events, overwrite history, replace the journal, or introduce an unselected emergency dependency.
- Add paged review with filter **and sort** by actor, UTC time, and event type. Enforce platform/tenant scope and audit review attempts/outcomes.
- Implement approved retention only through a database-operator retention procedure, never an application/background deletion path. Before removal, it records the authorized range, reason, approval, and pre-retention witnessed checkpoint in an append-only retention ledger; the post-retention state is checkpointed and verified. Preserve IP for the retained event lifetime unless explicitly approved otherwise.

**Contracts/migration:** additive queries and forward-only event/head/checkpoint/retention-ledger/capacity/notice changes; old rows remain explicit legacy-verification. The witness configuration, checkpoint cadence, maximum staleness, and database-principal grants are release-bound security configuration.

**Acceptance:** catalog/field tests pass; no non-database audit deletion surface or runtime-principal delete permission exists; changed rows, deleted/rewound tails, missing checkpoints, unauthorized retention, emergency-journal overflow, and journal/primary/witness reconciliation failures are detected; review sort/filter works; thresholds notify; primary/witness/journal failure enters the Stage-0-selected proven safe state; access is tenant-safe.

### Stage 6 — User-data transfer, residual-data, and integrity

**Depends on:** Stages 1 and 5.
**Outcome:** UD-1–11 close without moving business authorization into Hosting or Files.

- Add transfer direction, category, maximum size, media/format, rate/frequency, audit operation, and sensitivity metadata.
- Enforce transport ceilings before provider activation; module Application still authorizes tenant, subject, resource, and purpose.
- Restrict bulk/sensitive export to named capabilities and owner-managed rules; audit all attempts.
- Make Files own streamed SHA-256, claimed comparison, persisted value, validation, scanning, quarantine, blocked access, durable warning, and safe re-upload/restore.
- Require verified Files references or owner-specific verified source checksums.
- Add keyed manifests for sensitive non-file data using owner-defined canonical projections. For each owner, checkpoint an independently witnessed, versioned completeness root over the expected object identities, canonical-projection digests, and cardinality; a deleted data row plus its local manifest therefore changes the recomputed root. Mismatch blocks sensitive reads, rendering, processing, mutation, export, and downstream provider release, and creates warning/remediation evidence. Witness publication/reconciliation follows the Stage 5 staleness and fail-closed rules.
- Inventory residual-data paths; clear rented sensitive buffers, remove sensitive temp files, prevent storage-key reuse leakage, and keep secrets out of immutable strings where feasible.
- Define cancellation, timeout, duplicate, idempotency, partial-failure, and dependency-unavailable behavior. Do not buffer unbounded content.

**Contracts/migration:** metadata feeds OpenAPI; additive integrity status as needed; forward-only manifest/file-state migrations; audited resumable legacy enrollment; unverified sensitive legacy export remains blocked.

**Acceptance:** unclassified binary routes fail; oversized, disallowed, purposeless, or cross-tenant transfer is rejected before provider use; resource-reuse tests pass; altered data, a deleted manifest, or a deleted data-and-manifest pair cannot be read, rendered, processed, mutated, released to a provider, or exported, and creates a warning.

### Stage 7 — Security-management and authorization closure

**Depends on:** Stages 3–6.
**Outcome:** SM-1–6 and SM-4 are executable without generic bypass.

- Require explicit browser/bearer/internal/anonymous profile, anonymous justification, audit disposition, and transfer disposition in the authorization matrix.
- Add named capabilities for every applicable SM-4 operation; no generic security-administrator bypass.
- Replace the current additive tenant-role grant/revoke behavior with one authorized `ReplaceTenantRole` operation that atomically sets the single effective evaluated role (or removes membership where allowed), audits the before/after state, revokes affected sessions, and serializes or retries concurrent assignments without leaving more than one effective role. Preserve independent tenant/resource authorization.
- Retire, version, or constrain every legacy direct writer before enabling the invariant: tenant-role grant must use replacement semantics; tenant-role revoke must use the same owner; and generic role assignment must reject tenant-role names and be limited to explicitly listed global roles. Record affected operation IDs, generated consumers, compatibility window, migration path, and removal condition. No evaluated endpoint may retain additive tenant-role semantics.
- Execute the Stage 0 membership decision before cutover. Resolve every conflicting membership with an audited, owner-approved migration; block evaluated release if a conflict cannot be deterministically resolved. Global roles, including `PlatformAdmin` where applicable, are explicitly outside the per-tenant role cardinality only when documented in the matrix.
- Add optimistic concurrency and hard bounds to competing changes.
- Audit allowed/denied management reads/mutations as required.
- Add direct HTTP regression tests for authorization, tenant, antiforgery, sessions, audit review, and transfer rules.

**Acceptance:** management matrix equals executable mappings; every applicable operation has owner/proof; no endpoint is implicitly anonymous; every mutation has authorization/audit evidence.

### Stage 8 — Failure, recovery, and continuity proof

**Depends on:** Stages 2–7.
**Outcome:** TSF-1/2/4/5 and RA-1 are proven without unsupported HA claims.

- Define core functions and safe degraded behavior for process, PostgreSQL, Redis, object store, audit store/full, integrity key, provider, clock anomaly, disk pressure, and corrupted-data failures.
- Never fail open on authentication, authorization, tenant isolation, integrity, or audit. Preserve cancellation and bounded retry ownership; retried mutations require idempotency.
- Configure health checks and supervised restart; prove recovery after termination and dependency restoration.
- Perform backup/restore and migration-recovery exercises in release environment; no false success while recovery is incomplete.
- Verify errors expose no stack, source, secret, token, credential, or sensitive payload.
- If Stage 0 requires uninterrupted service, add an approved redundancy architecture before claiming RA-1.

**Acceptance:** fault injection and release recovery match the Stage 0 matrix; no bypass, corrupt confirmed state, unbounded retry, or swallowed failure occurs.

### Stage 8A — Evaluated-profile teardown and retention-controlled cleanup

**Depends on:** Stages 0–8, and is registered immediately after `Start-NetworkQuestionnaireProfile.ps1` starts the profile.
**Outcome:** Each profile run leaves no evaluated runtime resources behind, while release evidence remains available and tamper-evident.

- Implement `Stop-NetworkQuestionnaireProfile.ps1` under `src/Hosts/scripts/`. It accepts `-ProfileIdentity`, `-Commit`, and `-OutputDirectory`, rejects any profile other than `vitarish-network-questionnaire-v1.0`, and targets only the frozen compose project, its declared containers and networks, and explicitly declared ephemeral volumes. It must not stop Main, remove shared containers/networks/images, remove persistent data, or alter the candidate or signed-evidence locations.
- Register the stop script as a `finally` action immediately after a successful profile start, so it runs after both successful and failed build/test/verification paths. It writes the sanitized cleanup result before removal and exits nonzero if a declared evaluated resource remains. A cleanup failure blocks candidate formation, signing, and release.
- Stage 0 classifies every working output and failure artifact with its owner, storage location, retention period, access control, and deletion authority. After the required reports have either been copied into an immutable candidate or retained for the selected failed-run period, the cleanup owner may remove only that exact `<output>/<profile>/<commit>/` working subtree. It must never delete or rewrite an immutable candidate, signed attestation, amendment, revocation record, or their indexed payloads.
- Record cleanup execution, remaining-resource inspection, and any retention disposition in `run/cleanup-report.json` and `run/cleanup-report.txt`. A manually retained failed run records its expiry and custodian; expiry disposal is authorized, auditable, and limited to the classified working artifacts.

**Acceptance:** successful and faulted runs both stop the declared evaluated topology; no undeclared/shared resource is touched; a failed or skipped cleanup blocks release; working-output disposal is traceable; and immutable evidence remains unmodified and independently verifiable.

### Stage 9 — Submission and release evidence gate

**Depends on:** Stages 0–8A.
**Outcome:** Every mandatory and triggered row is truthful and tied to the exact release.

- Update English and Persian responses with ID, applicability, selected options, exact values, owner, configuration, tests, and evidence.
- Include final architecture/data-flow diagram and exact placement/versions.
- After the non-screenshot build/run/test reports are complete, capture any required guide screenshots under the companion plan and form one unsigned candidate bundle in the Stage-0-selected immutable candidate location. `Verify-NetworkQuestionnaireEvidence.ps1` validates its canonical manifest/index payloads, screenshot references, and required reports before any signature is created. Only a passing candidate-validation result permits the release-only signing key held outside every bundle to create a detached signed attestation over the exact canonical manifest and authoritative evidence-index payload hashes. The signed manifest records evaluated-profile identity, topology identity, profile-document and deployment-manifest hashes, clean commit, OpenAPI hash, immutable digests, component versions, policy version, authorization-matrix hash, Caddy hash, outbound TLS inventory, migrations, validation, capture time, issuer, signing-key ID, and key-custody reference. It rejects evidence labeled with another profile/topology or missing those bindings.
- The signed evidence index covers every applicable questionnaire row and guide test: guide ID where applicable, plan ID, applicability, owner, resolved configuration, preconditions, action/expected result, automated proof, configuration/transport/runtime artifacts, durable-state evidence when relevant, release binding, hashes, capture time, issuer, profile/topology identity, and redaction review. The separate screenshot-collection plan contributes only the screenshot/access-path references for applicable guide tests; it neither approves nor replaces the non-screenshot evidence required here.
- Evidence is never overwritten. An amendment creates a new signed superseding attestation that names and hashes the prior attestation; revocation is an append-only signed record. Independent verification uses the Stage-0-selected public verification material and fails closed on missing, altered, wrongly profiled/topologized, incomplete, unsigned, or revoked evidence.
- After signing, `Verify-NetworkQuestionnaireSignedEvidence.ps1` independently verifies the final signed bundle, including detached signatures, artifact/index payload hashes, profile/topology binding, and amendment/revocation state. It must prove that the attestation covers the unmodified candidate payload bytes and that its payload hashes equal the passing candidate-validation result. Submission is blocked unless both candidate validation and signed-bundle verification pass for the same candidate and manifest/index payload hashes.
- Run public TLS scan for chain, Server Authentication EKU, protocols, suites, key/curve, revocation, HSTS, and redirect. Any mismatch blocks release.
- Attach the adapted Caddy configuration, container/listener evidence showing no UDP 443, and exact-hostname probes proving TLS 1.2 and HTTP/2-over-TLS succeed while TLS 1.3 and HTTP/3-only fail without fallback. Any unexpected TLS 1.3, QUIC, or HTTP/3 exposure blocks this TLS 1.2-only profile.
- Attach the completed `nsmail-1` installation/vulnerability evidence: component and vulnerability inventory, public response-header report, cookie-attribute report, running-topology port scan with justification, unsafe-method probe report, CSRF lifecycle/replay report, and sanitized durable-state evidence where persistence itself is relevant. The separate guide screenshot-collection plan owns only guide-facing screenshot capture, access paths, and screenshot redaction.
- Attach internal/outbound TLS evidence for every enabled distributed/provider flow.
- Attach trusted-time, manual-update, backup/restore, alert-delivery, patching, and incident-response evidence where required, distinct from product behavior.
- Run the Web two-host runner against the non-publishable in-memory `src/Hosts/E2e` host as implementation feedback; inspect its fresh authenticated screenshot, correct defects, and rerun. Do not treat this run as evaluated-deployment proof. Separately run the frozen network-profile Caddy-to-Main topology and collect its release-bound transport, listener, configuration, and runtime evidence.
- Run the integrity lint again and perform an independent static review using the requirements matrix as the checklist. The review cites the exact plan sections closing CSRF ownership, TLS disposition, remote-authentication mechanisms, emergency journal, evidence integrity, release commands, and integrity lint. Never tick a row if its check was skipped, failed, or ran against another release/topology.

**Acceptance:** matrix validation and integrity lint find no mandatory/triggered row without passing proof or its required trace; independent static review closes all seven integrity findings with exact citations; the guide evidence index is complete, immutable, signed, independently verifiable, and release-bound; candidate-validation and signed-bundle-verification results bind to the same manifest/index hashes; hashes/digests match deployment; no evaluator decision remains; owner approves submission. A failed or skipped deterministic release command prevents signing, submission, and release.

## Cross-boundary behavior

| Boundary | Required behavior |
| --- | --- |
| Browser → Caddy → Main | HTTPS only, exact origins, secure/HTTP-only cookies, antiforgery, trusted forwarded headers, no plaintext authentication. |
| Authentication → persistence | Cancellation propagates; policy/session/audit failure cannot confirm a session or security mutation. |
| Main → PostgreSQL/Redis | Parameterized persistence, strict config, no secret logging, fail closed; authenticated encryption across machines. |
| Module → external provider | Module authorizes purpose/data; transport validates TLS/certificate/hostname; explicit timeout/cancellation; mutation retry requires idempotency. |
| Module → Files/storage | Authorization/metadata precede stream; mismatch blocks; no unbounded buffering; distributed transport is encrypted/authenticated. |
| Audit → UI/alert receiver | DTOs only, tenant-scoped, no sensitive/HMAC material; product emits durable state and evidence proves delivery. |
| Audit/data → independent witness | Application-owned checkpoint payload crosses an authenticated channel to an append-only witness outside PostgreSQL and the runtime database principal. The witness returns a durable reference; unavailable, stale, mismatched, or rewound witness state triggers the declared restricted mode and ordered reconciliation. |
| Background job → data | Idempotent, tenant-aware, cancellable, auditable, bounded, resumable, and safe on partial failure. |
| Operator → deployment | Immutable artifact, authorized manual update, ordered migrations, health verification, rollback limit, release record. |

## Verification requirements

### Deterministic validation binding

Each implementation slice adds its focused test beside the owner and runs the applicable commands below before it can advance. A command is not a substitute for release-topology proof; its required topology and expected artifact are part of the result.

### Versioned release-command contract

Stage 0 implements `Test-NetworkQuestionnairePlanIntegrity.ps1`; Stage 2 implements the build/start/test/verification scripts; Stage 8A implements `Stop-NetworkQuestionnaireProfile.ps1` under `src/Hosts/scripts/`. Together they are the only release-command path for `vitarish-network-questionnaire-v1.0`. Each accepts `-ProfileIdentity`, `-Commit`, and `-OutputDirectory`; `-Commit` must resolve to a clean repository commit. Each rejects a profile other than `vitarish-network-questionnaire-v1.0`, writes only sanitized output, records its own version and start/end UTC times, and exits nonzero on invalid inputs, a failed probe, missing prerequisite, unexpected topology, report-write failure, cleanup failure, or skipped required check. A nonzero or skipped invocation prevents candidate formation, signing, and release.

For a release rooted at `<output>`, build/run/test/verification working reports use `<output>/<profile>/<commit>/build/`, `run/`, `test/`, and `verify/`. Once those reports and required screenshots are ready, the release operator writes one immutable candidate bundle to the separately selected candidate location. The final signed bundle is a separate immutable attestation and copies only the candidate reports/screenshots selected by the evidence index; it never overwrites the candidate. Candidate validation records canonical manifest/index payload hashes, and signed-bundle verification compares those hashes with the unmodified payload bytes and detached signatures. Raw secrets, cookies, tokens, private keys, audit payloads, and patient data never enter any of these locations.

| Script | Required inputs and behavior | Required sanitized outputs |
| --- | --- | --- |
| `Build-NetworkQuestionnaireRelease.ps1` | Profile identity, clean commit, output directory, and the frozen compose/profile inputs. Resolves and pins source/configuration inputs, builds and publishes the declared images, and refuses mutable image references. | `build/build-report.json`, `build/build-report.txt`, and `build/image-digests.json` with commit, input/configuration hashes, resolved versions, immutable digests, timestamps, and issuer. |
| `Start-NetworkQuestionnaireProfile.ps1` | Profile identity, clean commit, output directory, and build-report/digest paths. Starts only `src/Hosts/docker-compose.network-questionnaire-v1.0.yml`, waits for its declared health checks, and rejects additional compose files, direct-Main publication, undeclared listeners, or a profile/commit mismatch. | `run/start-report.json`, `run/health-report.json`, and `run/listener-topology.json` with sanitized container identities, health results, listeners, topology/protocol disposition, commit, digests, and configuration hashes. |
| `Stop-NetworkQuestionnaireProfile.ps1` | Profile identity, clean commit, output directory, and started-topology evidence. Runs from the registered `finally` path and stops only the frozen compose project's declared containers, networks, and explicitly ephemeral volumes. It rejects a profile/commit/topology mismatch, preserves persistent data and all candidate/signed evidence, and fails if a declared evaluated resource remains. | `run/cleanup-report.json` and `run/cleanup-report.txt` with the sanitized stopped-resource inventory, remaining-resource inspection, working-output retention/disposal disposition, timestamps, and fail-closed result. |
| `Test-NetworkQuestionnaireProfile.ps1` | Profile identity, clean commit, output directory, and the started topology evidence. Runs versioned transport, port, TLS, header, cookie, method, CSRF, endpoint, outbound-client, and remote-authentication probes against the frozen profile. It treats a missing required probe as failure. | `test/transport-report.json`, `test/port-report.json`, `test/tls-report.json`, `test/header-report.json`, `test/cookie-report.json`, `test/method-report.json`, `test/csrf-report.json`, `test/endpoint-report.json`, `test/outbound-tls-report.json`, `test/remote-authentication-report.json`, plus matching `.txt` summaries. |
| `Verify-NetworkQuestionnaireEvidence.ps1` | Profile identity, clean commit, output directory, immutable unsigned manifest/index candidate payloads, all required reports/screenshots, and public verification material. Verifies canonical payload and artifact hashes, profile/topology binding, required-report completeness, screenshot-reference completeness, and candidate eligibility before signing; it cannot create, sign, or alter evidence. | `verify/evidence-candidate-validation.json` and `verify/evidence-candidate-validation.txt`, identifying each checked artifact and hash, the canonical manifest/index payload hashes, completeness result, and fail-closed disposition. |
| `Verify-NetworkQuestionnaireSignedEvidence.ps1` | Profile identity, clean commit, output directory, immutable signed attestation, the unmodified candidate manifest/index payloads, candidate-validation result, all indexed reports/screenshots, and public verification material. Verifies detached signatures, that the attested manifest/index payload hashes equal the passing candidate-validation result and unmodified candidate payload bytes, profile/topology binding, amendment/revocation state, and final artifact completeness; it cannot create or alter evidence. | `verify/signed-evidence-verification.json` and `verify/signed-evidence-verification.txt`, identifying signature, payload-hash binding, attestation/revocation, completeness, and fail-closed results. |
| `Test-NetworkQuestionnairePlanIntegrity.ps1` | This revised, version-controlled plan path, which is the authoritative versioned release-command contract, and the Stage-0 matrix and topology. Before implementation and again at Stage 9, mechanically traces every mandatory/triggered requirement to owner, topology disposition, slice, deterministic command, artifact, evidence protection, and release blocker. | `verify/integrity-lint.json` and `verify/integrity-lint.txt`; rejects contradictory TLS wording, dependency inversion, unselected emergency dependencies, and unresolved evaluator decisions. |

`Verify-NetworkQuestionnaireEvidence.ps1` must complete successfully before the release-only key creates the detached manifest/index attestation. `Verify-NetworkQuestionnaireSignedEvidence.ps1` must then complete successfully before submission or release. A later amendment or revocation produces a new append-only signed record and requires both verification runs; neither script may mutate a prior evidence artifact.

| Scope | Command and required setup | Expected result/artifact |
| --- | --- | --- |
| Identity, policy, sessions, and role migration | `dotnet test tests/Modules/Authentication.Tests/Authentication.Tests.csproj` and `dotnet test tests/Modules/Users.Tests/Users.Tests.csproj` | Policy, realm, session, and replacement-role regression tests pass; the role-membership migration report is attached to the Stage 0 decision. |
| Audit and integrity checkpoints | `dotnet test tests/Libraries/Audit.Tests/Audit.Tests.csproj` plus the owning Files/module test project | Chain, witness, retention, and integrity tests pass; fault-injection evidence identifies the exact configuration. |
| Endpoint/contract inventory | `dotnet run --project tools/architecture -- openapi-endpoints` and the owning generated-client command after an OpenAPI change | No architecture violations; generated output is regenerated and reviewed rather than edited. |
| Browser/API/authentication feedback | From `src/Hosts/Web`, set `VITARISH_E2E_PROOF_SCREENSHOT` and run `npm run test:e2e:two-host` against `src/Hosts/E2e` | Passing two-host run and a freshly inspected authenticated screenshot under `artifacts/two-host-e2e`. |
| Frozen network profile | `src/Hosts/scripts/Build-NetworkQuestionnaireRelease.ps1`, `Start-NetworkQuestionnaireProfile.ps1`, `Test-NetworkQuestionnaireProfile.ps1`, and `Stop-NetworkQuestionnaireProfile.ps1` with the profile identity, clean commit, and one output root | Required sanitized build/run/test/cleanup reports with commit, image digests, configuration hashes, hostname, scan time, and stopped-resource inventory; no direct-Main or undeclared listener exposure remains. |
| Evidence verification and integrity lint | `src/Hosts/scripts/Verify-NetworkQuestionnaireEvidence.ps1`, `Verify-NetworkQuestionnaireSignedEvidence.ps1`, and `Test-NetworkQuestionnairePlanIntegrity.ps1` with the profile identity, clean commit, evidence/output root, and public verification material | Passing candidate-validation, signed-bundle-verification, and integrity-lint reports; no signing without candidate validation and no submission or release without all three. |
| Final structural and documentation review | `dotnet run --project tools/architecture`, `git diff --check`, and Markdown link/path validation | No architecture violations, whitespace errors, or broken plan links/paths. |

- **Unit/Application:** policy bounds, passwords, realm/known-unknown parity, protected identifier derivation, client-tenant manipulation rejection, trusted-host realm selection, forwarded-header rejection, mechanisms, pre-auth actions, session limits/notices, idle resolution, revocation, one-role-per-tenant replacement and concurrent-assignment safety, CIDRs, audit scope, management permissions, transfer authorization.
- **Persistence:** migrations, audit chain/key rotation, runtime-principal deny grants, witnessed checkpoint append/replay/rewind detection, authorized database-only retention, capacity, emergency reconciliation, manifest completeness-root mismatch, Files quarantine, legacy enrollment.
- **Architecture:** endpoint/authorization/audit/transfer/residual/crypto/TLS inventories, anonymous justification, no unclassified binary route, OpenAPI/client regeneration.
- **Transport:** Caddy/compose, explicit `h1 h2`/no-`h3` adapted configuration, TCP-only 443 listener proof, TLS 1.2/HTTP-2 success and TLS 1.3/HTTP-3-only negative probes, plaintext rejection, trusted-proxy rejection, public scan, outbound identity/certificate failures, distributed-channel proof.
- **Vulnerability/install:** resolved component and known-vulnerability inventory, response headers by route class, all emitted cookie attributes, running port allowlist, unsafe-method and method-override rejection, CORS preflight, and CSRF issuance/expiry/invalidation/replay behavior.
- **Failure:** process, PostgreSQL, Redis, storage, audit stores, independent witness/staleness, disk, integrity key, session registry, clock, provider, corrupted data; prove safe response/recovery.
- **Real two-host E2E:** known/unknown lockout in the same authentication realm, client-tenant manipulation rejection, password plus OTP/passkey, session creation/replacement/notices, idle expiry, role/password invalidation, history acknowledgement, audit isolation/sort/filter, transfer authorization, integrity warning, cross-tenant denial, antiforgery, public HTTPS; inspect screenshot.
- **Release gate:** migrations, focused tests, builds/architecture checks, generated review, manifest, TLS/time/update/backup/alert evidence. An unrun mandatory check blocks compatibility.

## Compatibility, migration, rollback, and operations

- Routes, operation IDs, generated clients, cookies, persisted security/audit/session/file formats, configuration keys, and role semantics are compatibility boundaries. The evaluated profile enforces one role per user per tenant through atomic replacement only after the Stage 0 membership analysis has resolved existing memberships; no assertion about current data is made without that release-bound analysis.
- Contracts change additively; existing clients remain valid through a documented window; generated clients are reviewed.
- Legacy cookies without session IDs are invalidated once, with forced sign-in as recovery.
- Authentication realms, protected identifier derivations/key IDs, and attempt-state expiry are compatibility boundaries. Key rotation retains verification of active attempt state; a client-provided tenant value never becomes a compatibility selector for a pre-authentication realm.
- Policy, session, audit, capacity, notice, file-state, and manifest changes use forward-only migrations with defined mixed-version behavior.
- Old audit rows remain legacy-verification; no false chain is fabricated.
- Audit events are append-only outside database-operator retention: application ports, APIs, UI, and background jobs have no deletion capability; the runtime database principal is denied update/delete/truncate/DDL. Authorized retention records its range and approval before removal, is witnessed before and after execution, and remains distinguishable from tampering.
- Witness checkpoints are append-only compatibility state. An audit checkpoint covers tenant sequence/head; a sensitive-data checkpoint covers the owner-defined completeness root, cardinality, and projection version. Rollback preserves their ability to verify prior state and never rewinds a witness epoch.
- The network profile's identity, included-component allowlist, TOE/environment disposition, and deployment-manifest path are compatibility and release boundaries. The Main-only profile remains a separately versioned claim; no test, scan, artifact, or operational evidence crosses profiles without a new profile-specific run.
- Legacy sensitive data enrolls through audited resumable jobs; sensitive export remains blocked until verified/remediated.
- Rollback cannot blindly cross a forward-only security migration; use roll-forward or a tested compatible image and state data effects.
- Key rotation retains old verification capability. Missing required key material is critical unavailability, never permission to skip verification.
- Capacity, emergency-audit, integrity, and revocation signals have operational owners/routes. Product emits; release evidence proves delivery.

## Risks and mitigations

| Risk | Mitigation |
| --- | --- |
| Questionnaire conflicts with modern platform behavior/evaluator expectations | Resolve Stage 0 questions in writing; do not encode optimistic interpretation. |
| Full scope triggers hidden outbound/internal TLS requirements | Mechanically inventory every client/link and reject missing disposition. |
| Audit chain, emergency journal, and retention interact incorrectly | Freeze journal/witness semantics in Stage 0, then fault-inject and reconcile deterministically. |
| Database-privileged tail/data-and-manifest deletion evades local HMAC verification | Deny runtime mutation grants and compare local chains/completeness roots with an independently protected append-only witness; treat stale, missing, mismatched, or rewound checkpoints as critical. |
| Multiple sessions conflict with IA-8 | Default to one; require functional need and every-new-session notice. |
| Concurrent role assignments could leave conflicting effective access | Serialize or retry the tenant-role replacement transaction; prove that a user never has more than one effective evaluated role per tenant and revoke sessions after the committed replacement. |
| Security policy becomes an unowned settings bag | Keep invariants with owners; aggregate only cohesive access-policy values. |
| Guide expectations conflict with browser/framework defaults for `X-XSS-Protection` or per-request CSRF renewal | Obtain Stage 0 evaluator disposition; implement a nonce/replay owner only if required and prove the accepted behavior at public ingress. |
| Integrity manifests are expensive/unstable | Version owner-defined canonical projections; scan incrementally; make progress resumable. |
| Residual-data claims exceed managed-runtime guarantees | Narrow claims to owned buffers/temp/storage reuse and document unavoidable limitations. |
| Evidence drifts from deployment | Bind it to commit, digests, config hashes, migrations, hostname, and scan time. |
| Caddy default HTTP/3 or a UDP 443 port mapping silently violates the TLS 1.2-only profile | Configure `h1 h2` explicitly, omit UDP 443, and release-gate adapted configuration, listeners, and TLS/HTTP negative probes. |
| Client-controlled tenant input fragments unknown-user lockout state or reveals tenant/account existence | Select the authentication realm only from configuration or trusted host resolution; use protected normalized identifiers and generic non-disclosing handling for invalid/unknown hosts. |
| Network profile silently inherits the narrower Main-only scope or its evidence | Use a distinct profile identity, authoritative document set, deployment manifest, allowlist, and release gate; reject cross-profile evidence. |
| RA-1 means uninterrupted HA | Treat as Stage 0 decision; add approved redundancy or stop the claim. |

## Rejected alternatives

- Treating an unselected mandatory row as unsupported.
- Keeping three sessions with notice only on the fourth login.
- Treating a private Docker network as encrypted transport.
- Treating health checks as authorized-user notification and delivered alerts.
- Claiming TLS compliance from protocol versions alone.
- Relying on database permissions alone for audit tamper detection.
- Redacting audit IP after an arbitrary period without evaluator approval.
- Adding HA before resolving RA-1 interpretation.
- Centralizing module authorization/data semantics in Hosting, Audit, or Files.

## Revision triggers

Reopen Stage 0 for any change to evaluated-profile identity, topology/placement, ingress, authentication, sessions, mobile clients, SSH/mTLS, updates, crypto, certificates/revocation, providers/outbound clients, transfer routes, sensitive-data classification, audit retention/full behavior, role semantics, or evaluator interpretation.

## Definition of complete

This plan is complete only when:

- all Stage 0 decisions are closed;
- the separate network-profile configuration, Security Target, inventory, topology, matrix, and deployment manifest agree, while the Main-only profile remains explicitly separate;
- every mandatory and triggered conditional row has implementation and passing proof;
- real cross-boundary E2E and screenshot inspection pass;
- release evidence belongs to the exact artifacts/topology;
- migrations, compatibility, rollback limits, security/operational effects, residual risks, deviations, and excluded scope are recorded;
- no claim depends solely on framework defaults, private-HTTP probes, compilation, or unsupported assertion.
