# Vitarish Network Application Security Questionnaire: Deployment-Evidence Plan

**Status:** Separate release-evidence phase for the `vitarish-network-questionnaire-v1.0` profile.
**Prerequisite:** [implementation-plan.md](implementation-plan.md) is complete for the exact repository commit and the implementation matrix has no `implementation-gap` or `blocked` rows.

## Purpose and boundary

This plan owns release-instance proof, not repository implementation. It runs the committed profile artifacts against a frozen deployment and produces evidence tied to the exact commit, image digests, configuration hashes, topology, and hostname.

It owns live topology and listener scans, certificates and provider configuration, external vulnerability scans, two-host E2E, screenshots, backup/restore and alert-delivery records, immutable evidence custody, signing, amendment/revocation, and submission.

It does not add product behavior, alter application code, edit generated output, reuse evidence from the Main-only ISO profile, or treat private-network placement, a build, or static validation as deployment proof.

## Shared sources and matrices

- `requirements-matrix.md` is the canonical catalog of every mandatory row and only conditional rows triggered by the frozen Vitarish boundary. Optional rows are absent.
- `implementation-matrix.md` records repository-owned proof and must be complete before this phase starts.
- `deployment-evidence-matrix.md` records release-instance proof and references the stable requirement IDs without duplicating row definitions.
- `evaluated-configuration.md`, `security-target.md`, `toe-inventory.md`, `topology.md`, and `src/Hosts/docker-compose.network-questionnaire-v1.0.yml` define the exact profile.

Any new endpoint, provider, distributed component, listener, protocol, authentication method, transfer route, mobile client, SSH/mTLS capability, or other conditional trigger reopens Stage 0 and blocks evidence collection until the matrices and profile are updated.

## Evidence command contract

The following committed scripts are the only release-command path for this profile. Each accepts `-ProfileIdentity`, `-Commit`, and `-OutputDirectory`; rejects another profile, a dirty or mismatched commit, missing prerequisites, unexpected topology, failed probes, report-write errors, cleanup failures, and skipped required checks; and writes sanitized output only.

| Script | Required result |
| --- | --- |
| `Build-NetworkQuestionnaireRelease.ps1` | Pinned source/configuration inputs, immutable image digests, and sanitized build reports. |
| `Start-NetworkQuestionnaireProfile.ps1` | Only the frozen Compose project starts; health, listener, topology, protocol, commit, digest, and configuration-hash reports are written. |
| `Test-NetworkQuestionnaireProfile.ps1` | Transport, port, TLS, headers, cookies, methods, CORS/CSRF, endpoints, outbound TLS, and remote-authentication probes all run; a missing required probe fails the phase. |
| `Stop-NetworkQuestionnaireProfile.ps1` | The registered `finally` path stops only declared evaluated resources, preserves persistent data and evidence, and fails if resources remain. |
| `Verify-NetworkQuestionnaireEvidence.ps1` | Candidate artifact hashes, manifest/index payloads, profile/topology binding, required reports, and screenshot references pass before signing. |
| `Verify-NetworkQuestionnaireSignedEvidence.ps1` | Detached signatures, unmodified payload hashes, candidate-validation binding, amendment/revocation state, and final completeness pass before submission. |
| `Test-NetworkQuestionnairePlanIntegrity.ps1` | Requirement-to-owner, topology, implementation proof, deployment proof, artifact, and release-blocker mappings remain complete. |

Reports are written beneath `<output>/<profile>/<commit>/build/`, `run/`, `test/`, and `verify/`. Candidate and signed bundles are separate immutable locations. No secret, cookie, token, private key, audit payload, or patient data enters those locations.

## Ordered release-evidence stages

1. Validate the clean commit, profile identity, matrices, evaluator decisions, and implementation handoff.
2. Build and start only the frozen network profile; record health and listener topology.
3. Run all required probes, scans, two-host E2E, screenshot collection, provider/TLS checks, backup/restore evidence, alert-delivery evidence, and failure/recovery evidence.
4. Register teardown immediately after successful startup and retain or dispose of working output only according to its classified retention policy.
5. Build the unsigned candidate, validate every indexed artifact and canonical manifest/index hash, then create a separate signed attestation with the release-only key.
6. Independently verify the signed bundle, amendments, and revocations. Any failed or skipped mandatory check blocks submission and release.

## Required release proof

- Exact deployment topology, public/private listeners, no direct Main exposure, no undeclared UDP 443/QUIC, and expected TLS protocol/suite/certificate behavior.
- Component/version/digest and vulnerability inventories, response headers, cookie attributes, method restrictions, CORS/CSRF behavior, and port allowlist.
- Two-host authentication, lockout parity, second mechanism, sessions, invalidation, tenant isolation, transfer authorization, audit review, integrity warnings, antiforgery, and public HTTPS.
- Trusted time, manual update provenance, migrations, backup/restore, capacity/alert delivery, incident-response records, external provider identity validation, and distributed-channel proof.
- Immutable, signed, independently verifiable evidence index covering every mandatory and triggered conditional row.

## Completion gate

Deployment-evidence completion requires every mandatory and triggered conditional row to have passing release-bound proof, exact profile/topology binding, complete candidate validation, successful signed-bundle verification, no unresolved evaluator decision, and owner-approved submission. It is a separate gate and does not change repository implementation status.
