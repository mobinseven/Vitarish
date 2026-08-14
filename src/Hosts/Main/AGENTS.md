# Main host agent routing

This host is an outer composition and runtime boundary. Do not add domain or
use-case behavior here to avoid changing its owner. The repository and scoped
`AGENTS.md` instructions own backend-boundary guidance. Web-host routing rules
do not apply to backend host wiring. Move business behavior to its owning
App, Module, or Identity Application/Domain slice.

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
