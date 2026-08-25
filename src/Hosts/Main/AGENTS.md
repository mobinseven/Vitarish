# Main host agent routing

This host is an outer composition and runtime boundary. Do not add domain or
use-case behavior here to avoid changing its owner. The repository and scoped
`AGENTS.md` instructions own backend-boundary guidance. Web-host routing rules
do not apply to backend host wiring. Move business behavior to its owning
App, Module, or Identity Application/Domain slice.

## Security composition

- **[MUST]** Main composes security owners; it does not become one. Password,
  failed-attempt, session, audit, transfer, resource-authorization, integrity,
  and retention rules stay at their scoped owners and reach Main through
  explicit registrations, options, metadata, and ports.
- **[MUST]** Validate mandatory production security configuration on the real
  startup path before serving traffic. Do not defer rejection until the first
  request, replace validation with a warning, or add environment/test branches
  that bypass it.
- **[MUST]** Trust forwarded scheme, host, and client IP only after the shared
  Hosting boundary accepts the immediate proxy and configured proxy network.
  Main and modules must not parse raw forwarding headers independently.
- **[MUST]** Keep middleware order explicit and regression-tested when changing
  forwarded headers, HTTPS handling, authentication, session validation,
  antiforgery, CORS, authorization, tenant resolution, rate limiting, audit,
  exception handling, or endpoint metadata validation.
- **[MUST]** Main must not be publicly exposed in a supported ingress
  configuration. A direct private-HTTP probe is dependency diagnostics, not
  HTTPS or browser-security proof.

## Native AOT readiness

- **[MUST]** Apply the root Native AOT readiness rule. Keep host composition,
  endpoint mapping, dependency registration, configuration, and JSON metadata
  explicit or generated.

## Named host values

- **[MUST]** Reuse the authoritative owner for host route prefixes, endpoint
  operation IDs, authentication scheme/policy names, claim names, configuration
  keys, and JSON metadata identifiers. Main host wiring must not repeat these
  contract or security literals or become a second source of truth; use the
  existing Contracts, Identity, options, or generated metadata mechanism.

## E2E proof feedback

- **[MUST]** For production-host composition, middleware, authentication,
  authorization, tenant, lifecycle, API, or browser-visible data changes, use
  the real Web-to-`src/Hosts/E2e` flow before reporting completion.
- The E2E host is the isolated provider and fixture boundary; validate Main's
  behavior through the normal production path and do not add E2E branches,
  bypass routes, or test credentials to Main.
- Inspect the authenticated screenshot from the passing run as feedback and
  correct any defect it reveals before completing the work.
