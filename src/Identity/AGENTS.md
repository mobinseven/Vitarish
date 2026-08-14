# Identity agent routing

Identity changes are security-sensitive. The repository and scoped `AGENTS.md`
instructions own identity and backend-boundary guidance. React and Web-host
routing rules do not apply to Identity-only work.
Preserve redaction, least privilege, compatibility, and explicit failure paths.

## Named security values

- **[MUST]** Keep claim types, authentication scheme names, authorization policy
  names, issuer/audience identifiers, and configuration keys at their
  authoritative Identity or configuration owner. Reuse them across handlers,
  policies, host wiring, clients, and tests; do not scatter security-sensitive
  literals or create a second source of truth. Exact protocol-value assertions
  may retain literals when the value itself is the behavior under test.
