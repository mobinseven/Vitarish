# E2E host security-proof contract

This host supplies isolated fixtures and adapters while exercising the normal
production routes, middleware, contracts, authentication, authorization,
tenant, session, audit, and Web transport paths. It follows the parent Host
contract and is proof infrastructure, not a second product implementation.

- **[MUST]** Do not add authentication bypasses, test credentials accepted by
  Main, alternate production endpoint mappings, weakened policies, or
  environment branches in production code. Substitute only explicit outer
  adapters at E2E composition.
- **[MUST]** Keep fixtures deterministic, tenant/resource scoped, minimal, and
  free of production secrets or sensitive payloads. A scenario must create or
  reset the state it owns and must not depend on execution order or stale
  screenshots/artifacts.
- **[MUST]** Failure injection is an E2E adapter capability with a named scope
  and reset path. Inject audit, storage, provider, clock, or dependency failure
  without teaching production handlers about E2E mode, and prove the normal
  path is restored afterward.
- **[MUST]** Security scenarios use public browser/API entry points and assert
  both success and bypass denial. Cover direct HTTP where UI cannot prove
  authorization, and never use Main's private HTTP address as HTTPS evidence.
- **[MUST]** The proof screenshot is fresh, authenticated, and created by the
  same passing run. Inspect it for loading, authorization, notice, history,
  integrity, and failure-state defects; correct and rerun before completion.
- **[MUST]** When a scenario adds persistent, session, audit, file, or
  background-job state, define cleanup or use an isolated disposable store.
  Do not leave generated screenshots, logs, databases, or temporary files
  outside the retained proof artifact set.
