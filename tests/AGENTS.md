# .NET test agent routing

Load the boundary skill that owns the behavior under test (for example,
backend use case, data evolution, transport contract, external integration, or
trust boundary). Do not load all production skills simply because a test is
under `tests`, and do not use test-only changes to redefine production
behavior. Tests for `tests/Modules` follow `src/Modules/AGENTS.md`; tests for
`tests/Apps` follow `src/Apps/AGENTS.md` conceptually.
Tests for `tests/Libraries` follow the matching production library's scoped
`AGENTS.md` conceptually when one exists.

Do not apply React or Web-host routing rules to these .NET tests. Web package
tests remain governed by `src/Hosts/Web/AGENTS.md`.

## Security proof

- **[MUST]** Test the security rule at its owner and its bypass at the boundary.
  Domain/Application tests prove policy and failure semantics; direct HTTP
  tests prove authentication, tenant/resource authorization, antiforgery,
  endpoint metadata, and safe error mapping. UI tests are not authorization
  proof.
- **[MUST]** Framework defaults, mocks that reproduce the expected answer,
  compile success, and happy-path-only tests are not proof of password,
  session, audit, TLS, integrity, transfer, or failure controls. Assert exact
  configured values or observable rejection/failure behavior.
- **[MUST]** Use injected/fake clocks for lockout, idle-session, challenge,
  retention, and retry timing tests. Do not use wall-clock sleeps or weaken
  thresholds to make tests fast.
- **[MUST]** Security persistence tests cover migration from existing data,
  optimistic concurrency, duplicate/idempotent calls, unavailable stores,
  partial writes, and recovery. Do not fabricate audit chains or integrity
  manifests for legacy rows merely to make assertions pass.
- **[MUST]** A required cross-boundary control is complete only after the
  applicable real-host E2E workflow in the scoped production `AGENTS.md` has
  run and its fresh authenticated screenshot has been inspected.
