# .NET test agent routing

Load the boundary skill that owns the behavior under test (for example,
backend use case, data evolution, transport contract, external integration, or
trust boundary). Do not load all production skills simply because a test is
under `tests`, and do not use test-only changes to redefine production
behavior. Tests for `tests/Modules` follow `src/Modules/AGENTS.md`; tests for
`tests/Apps` follow `src/Apps/AGENTS.md` conceptually.

Do not apply React or Web-host routing rules to these .NET tests. Web package
tests remain governed by `src/Hosts/Web/AGENTS.md`.
