# Host agent routing

Applies to host-level files outside a more specific host directory, including
shared Docker Compose and runtime orchestration configuration.

Load `platform-artifact-architecture` for host composition, project placement,
or dependency direction; `platform-trust-boundary` for network exposure,
secrets, identity, tenant isolation, or other security controls; and
`platform-external-integration` for remote runtime dependencies. Add
`platform-test-validation` for build, startup, or runtime validation decisions.

Do not load backend-use-case or React feature skills for host orchestration.
Files inside `Main` or `Web` also follow their nearer `AGENTS.md`.
