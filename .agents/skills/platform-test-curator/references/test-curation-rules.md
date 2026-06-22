# Test Curation Rules

1. Test the risk, not the implementation detail.
2. Hot tests are runtime scenarios, not simply important automated tests.
3. Prefer unit tests for local deterministic rules.
4. Use hot scenarios when correctness depends on DB, UI, auth, files, workers, network, latency, failure, concurrency, or deployment-like configuration.
5. Every required hot scenario must name why unit tests are insufficient.
6. Every hot scenario must name environment, setup, trigger, expected behavior, evidence, and cleanup.
7. Do not require infrastructure-heavy scenarios without a clear risk.
8. Do not fake a hot scenario as a single `dotnet test` command unless the command orchestrates the needed environment.
9. Suggested tests are not blockers. Required tests are blockers.
10. Keep the required set small, high-signal, and traceable to journey, roadmap, ADR, defect, or code risk.
