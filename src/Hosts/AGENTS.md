# Host agent routing

Applies to host-level files outside a more specific host directory, including
shared Docker Compose and runtime orchestration configuration.

The repository and scoped `AGENTS.md` instructions own host-composition,
security, and runtime-dependency guidance. Do not load Web feature skills for
host orchestration.
Files inside `Main` or `Web` also follow their nearer `AGENTS.md`.

## Evaluated deployment trust boundary

- **[MUST]** Keep the evaluated topology explicit. For every runtime link,
  either prove the components are constrained to the same machine or use an
  authenticated confidentiality-and-integrity channel. A private Docker
  network, unpublished port, or firewall rule is not a substitute for
  transport security across machines.
- **[MUST]** Public browser traffic terminates HTTPS at the versioned ingress;
  Main remains a private upstream. Reject plaintext authentication, SSL, and
  TLS below 1.2. Do not claim protocol, cipher, key/curve, certificate-chain,
  EKU, revocation, redirect, or HSTS behavior from defaults: bind the claim to
  repository configuration and a scan of the exact release hostname.
- **[MUST]** Treat hostnames, trusted-proxy networks, TLS policy, certificate
  references, dependency placement, and public port exposure as validated
  security configuration. Missing or weaker-than-baseline production values
  must stop startup or deployment before traffic is served.
- **[MUST]** A product-generated health state is not delivered operational
  evidence. For capacity, integrity, audit, or dependency failures, preserve
  both sides: the product emits a durable authorized signal, and release proof
  verifies delivery through the configured operator channel.
- **[SHOULD]** Keep release evidence keyed to the exact commit, immutable image
  digests, configuration hashes, migration state, hostname, component
  versions, and scan time. Evidence from another topology or release is not
  reusable.

## E2E proof feedback

- **[MUST]** For host orchestration, runtime lifecycle, authentication,
  authorization, tenant resolution, shared composition, or browser-visible
  data changes, use the real `src/Hosts/E2e` host with the Web two-host runner.
- **[MUST]** Treat the authenticated screenshot produced by that run as
  implementation feedback: inspect it, correct any visual, loading, data, or
  authorization-flow defect, and rerun until the suite passes and the result
  is acceptable. A screenshot captured only after the work, without inspection,
  is not proof of completion.
- Do not use a Main-host E2E branch or test-only authentication bypass as a
  substitute for the separate E2E host.
