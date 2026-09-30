# ADR-0014: Serve RSA and ECDSA certificates from the Caddy ingress

Status: Accepted<br>
Date: 2026-09-29<br>
Owner: Platform Architecture<br>
Scope: Shared public ingress for test and production

## Context

The ingress currently lets Caddy issue and renew P-256 ECDSA certificates.
Its TLS 1.2 policy also enables ECDHE-RSA suites, which require an RSA server
certificate for clients that negotiate those suites. Caddy's automatic
certificate configuration selects one key type, so switching it to RSA would
remove ECDSA coverage instead of serving both certificate types.

Caddy can load additional PEM certificate/key bundles from a directory, but
externally loaded certificates need an external renewal owner. The production
deployment already has a Cloudflare DNS API token, protected hostnames, and a
release-time Caddy configuration validator.

Assumptions:

- The existing eight test and eight production hostname settings are the
  complete public host set served by this ingress.
- Those names are public DNS names eligible for a publicly trusted ACME
  certificate; committed `.example.invalid` values are render-only examples.
- The production deployment operator owns the weekly renewal check.

Non-goals:

- Changing the TLS protocol floor, TLS 1.2 cipher list, TLS 1.3 cipher
  behavior, or ECDSA certificate automation.
- Enabling static RSA key exchange, finite-field DHE, or static ECDH cipher
  suites that the current Caddy/Go TLS implementation does not provide.
- Automating scheduled certificate renewal or publishing a separate operator
  image.

Ownership stays at the existing boundary: the ingress owns TLS configuration,
the deployment Compose bundle owns the certificate operator and persistent
volumes, the production deploy script owns fail-closed bootstrap and release
probes, and the host runbook owns the manual renewal procedure. No application
contracts, persisted product data, or generated output change.

## Decision

Keep Caddy's automatic P-256 ECDSA certificate issuance. Add one RSA-2048 SAN
certificate for all configured test and production ingress hostnames using the
existing Cloudflare DNS challenge. A profile-gated `lego` operator writes its
ACME account state to a dedicated persistent volume, assembles the renewed
certificate chain and private key into a bundle, saves one previous bundle,
and atomically publishes the active bundle to a second persistent volume.

Caddy mounts the bundle volume read-only and loads its PEM bundles alongside
the automatically managed certificate. Its image entrypoint rejects a missing,
empty, or symlinked required bundle. The production deploy script issues the
initial RSA certificate before configuration validation when that bundle is
absent. Renewal remains an operator action followed by Caddy validation and
reload.

The certificate uses all eight existing host settings for each environment.
This makes certificate coverage derive from the protected Compose inputs; it
does not introduce a second hostname list or a new protected environment key.

## Consequences

- TLS 1.2 clients can negotiate the configured ECDHE-RSA suites when Caddy
  selects the RSA certificate; clients using ECDHE-ECDSA can continue using
  the auto-managed ECDSA certificate.
- The single RSA certificate contains test and production hostnames in its
  public Subject Alternative Name list. Public certificate transparency makes
  these names visible; this is consistent with their use as public ingress
  names.
- A production deployment can stop before serving traffic if ACME issuance,
  bundle publication, or Caddy validation fails. The currently running Caddy
  process keeps its loaded certificate until a successful reload.
- The RSA certificate, previous bundle, and ACME account state live in named
  Docker volumes. Normal application rollback leaves those volumes intact.
  Operators must not remove them during deployment cleanup.
- The Cloudflare token is available to the on-demand certificate operator
  through the existing protected environment file. It must remain zone-scoped
  with only Zone read and DNS edit access and must not appear in command output.
- No new application API, data schema, generated output, or protected
  configuration key is introduced. New named volumes are additive; deleting
  them would remove the RSA certificate, its rollback copy, or ACME account
  state.

## Alternatives considered

### Change automatic certificate key type to RSA

Rejected because it would replace the automatically managed ECDSA certificate
instead of keeping both key types available.

### Serve only the current ECDSA certificate

Rejected because it cannot authenticate the configured ECDHE-RSA suites.

### Add another public proxy or terminate TLS in the applications

Rejected because this changes the evaluated ingress boundary and duplicates
certificate, network, and release lifecycle ownership.

## Implementation and validation plan

1. Add the RSA bundle loader and fail-closed image entrypoint. Acceptance:
   the entrypoint rejects a missing bundle for both runtime and validation;
   with a valid bundle, Caddy accepts the configuration.
2. Add the profile-gated ACME operator and deployment bootstrap. Acceptance:
   the protected host values feed one SAN request; failed issuance leaves the
   active bundle untouched; publication is atomic and the prior bundle is
   recoverable.
3. Add the renewal runbook and link it from server deployment guidance.
   Acceptance: it covers first issuance, renewal, validation, reload, RSA
   handshake verification, and recovery without printing secrets or keys.
4. Render the committed ingress Compose example and build the Caddy image.
   The release route probes must force an RSA-authenticated TLS 1.2 handshake
   for each configured production hostname class. Then perform the
   host-composition two-host workflow and inspect its screenshot before
   claiming complete host-composition runtime proof.

Applicable workflow guidance is the host-composition E2E gate in
`src/Hosts/AGENTS.md`; `docs/AGENTS.md` does not require a separate skill for
this deployment configuration and runbook.

The host E2E workflow and live certificate issuance require runtime services
and secrets not available to this repository-only change. These are release
validation gates, not claims made by a local Compose render.

## Risks and revision triggers

- The deployment operator must run the renewal procedure weekly; failure to
  assign that recurring task risks expiry. Replace the manual runbook with
  scheduled renewal only when its monitoring, concurrency, failure alerting,
  and reload path are owned and validated.
- Revisit the single SAN certificate if test hostnames cease to be public or
  must be isolated from the production certificate.
- Revisit this decision if the ingress TLS implementation changes or if the
  required cipher policy expands beyond the suites supported by its TLS
  library.

## References

- [Caddy TLS directive](https://caddyserver.com/docs/caddyfile/directives/tls)
- [lego obtain and renewal](https://go-acme.github.io/lego/obtain/index.print.html)
- [lego Cloudflare DNS provider](https://go-acme.github.io/lego/dns/cloudflare/index.print.html)
