# Host agent routing

Applies to host-level files outside a more specific host directory, including
shared Docker Compose and runtime orchestration configuration.

The repository and scoped `AGENTS.md` instructions own host-composition,
security, and runtime-dependency guidance. Do not load Web feature skills for
host orchestration.
Files inside `Main` or `Web` also follow their nearer `AGENTS.md`.

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
