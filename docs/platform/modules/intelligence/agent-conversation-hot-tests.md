# Agent conversation hot-test harness

The repeatable harness is `tools/testing/agent-conversations-hot-tests.ps1`. It starts the existing PostgreSQL and Redis services through `src/Hosts/docker-compose.main.yml`, runs deterministic Intelligence agent/conversation and contract tests, and stops the services unless `-KeepServices` is supplied. It performs no manual database mutation.

Run the deterministic profile from PowerShell with a secret supplied by the environment:

```powershell
$env:REDIS_PASSWORD = '<deployment redis password>'
pwsh tools/testing/agent-conversations-hot-tests.ps1
```

Provider-dependent checks are opt-in and separated from deterministic coverage. They require a running agent-runtime deployment and an explicit base URL:

```powershell
$env:AGENT_RUNTIME_BASE_URL = 'http://localhost:3003'
pwsh tools/testing/agent-conversations-hot-tests.ps1 -Provider
```

The deterministic profile covers conversation handler history replacement, lease outcomes, invalid/oversized history handling, contract serialization, and runtime credential/request boundaries. The provider profile covers the HTTP runner contract and must be run only against a disposable test provider. Full PostgreSQL migration, encrypted-history restart, concurrent lease, stale-lease, owned-deletion, and Data Protection continuity scenarios are release-gate scenarios and require the same harness environment with the host and migration applied; their evidence must be recorded in the release run log. Test output is minimal and the harness does not print credentials or runtime payloads.
