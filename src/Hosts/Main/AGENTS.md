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
