## What durable lessons have previous Codex sessions learned?

- `AGENTS.md` is the repository-wide operating policy. Treat every changed item as an artifact, classify it before editing, keep one responsibility per artifact, and include the required architecture audit when asked for implementation detail.
- Documentation is scoped by owner: `docs/README.md` is a navigation map only, while authoritative content belongs in the narrow owning folder such as `docs/architecture`, `docs/platform`, `docs/apps`, `docs/hosts`, `docs/development`, or `docs/runbooks`.
- Use project skills only when the task matches their routing. Open the specific `SKILL.md` instead of loading every skill.
- Focused module builds are for inner-loop speed only. Before merge-level handoff, run the full host graph with `ModuleBuildProfile=All` or the full solution build.
- React Web apps must keep app composition, generated clients, API adapters, route handlers, and localization at the app boundary. Shared Web UI must receive state, display text, navigation intents, and handlers through props or slots.
- Contracts are serialization boundaries. They should not contain business logic, persistence models, UI state, or backend implementation details.
- Persisted instants are UTC `DateTime` values, not offset-preserving timestamp models.
- Internal module API endpoints are protected by default; do not expose module internals unless the relevant architecture decision explicitly allows it.
- The `private-practice-general` app is the current app identity. `clinic-owner` remains only as an explicit compatibility or historical reference.

## What decisions should not be reopened without reason?

- ADR-0001: architecture decisions are recorded as ADRs in `docs/architecture/decisions`.
- ADR-0002: persisted instants use UTC `DateTime`.
- ADR-0003: module Interface libraries are isolated with API client contracts; generated clients and contracts are not manually edited.
- ADR-0004: internal module API endpoints are protected by default.
- ADR-0005: OpenAPI endpoint conventions are standardized and should be followed instead of inventing endpoint metadata patterns.
- ADR-0006: platform journey routing is standardized.
- ADR-0007: React localization is standardized; user-facing copy should stay in the approved localization owner.
- ADR-0008: app experience boundaries are standardized; shared Web UI must not call backend transport directly.
- The documentation map remains non-authoritative; do not turn `docs/README.md` into a source of truth.
- Do not replace repository package managers or introduce speculative CQRS, mediator, generic repository, event-sourcing, inheritance, or design-system wrapper patterns without a current simplifying need.

## What commands worked or failed?

- Worked: `find /workspace -name AGENTS.md -print | sort` to discover policy files without recursive `ls`.
- Worked: `rg -n "<pattern>" -S <paths> --glob '!node_modules' --glob '!**/bin/**' --glob '!**/obj/**' --glob '!**/dist/**' --glob '!**/.git/**'` for repository-wide text discovery.
- Worked: `dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Users` for a focused Users inner-loop build.
- Worked: `dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Appointments` for a focused Appointments inner-loop build.
- Worked: `dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=Custom -p:EnableUsersModule=true -p:EnableAppointmentsModule=true` for a custom focused host build.
- Required for merge-level confidence when backend code changes: `dotnet build src/Hosts/Main/Main.csproj -p:ModuleBuildProfile=All` or the full solution build.
- Worked: `scripts/benchmark-focused-builds.sh` to compare focused build profiles.
- Worked: from `src/Hosts/Web`, `npm run build`, `npm run typecheck`, `npm run test`, `npm run validate:api-boundary`, `npm run validate:localization`, `npm run validate:date-inputs`, `npm run validate:web-ui-boundary`, `npm run validate:app-boundaries`, and `npm run validate:docs:app-boundaries` are the defined workspace validation commands.
- Known failure mode: Web hooks can report `fetch failed` when the .NET host is not running at `NEXT_PUBLIC_API_BASE_URL`.
- Known failure mode: OpenAPI mutation calls can return `ApiError: 403` when CSRF cookies or antiforgery headers are missing or mismatched.
- Known failure mode: generated hooks can be wrong when the Orval `input.target` path points at the wrong OpenAPI spec file.

## What recurring bugs/pitfalls exist?

- Duplicating business rules in controllers, repositories, DTOs, migrations, UI components, localization resources, generated clients, or skill files violates the repository policy.
- Query flows must not mutate usage state, analytics, last-used timestamps, or other observable state.
- Missing route parameters and failed detail loads commonly redirect to list pages; document only behavior with direct static or runtime evidence.
- Optional module references in active host/UI code break focused builds. Move optional code behind project-composition boundaries or add the module to the required profile graph.
- Do not hide disabled module references with C# preprocessor symbols.
- Do not use temporary architecture exceptions as permanent policy; non-empty temporary exception lists are validator failures after completed rollout.
- Provider reference imports must not default every sync version to `unknown`; it breaks idempotency and traceability.
- For Tamin provider work, do not send Gregorian ISO dates where the provider expects its own calendar/date format.
- For Knowledge normal-user QA, do not reuse owner command components for normal-user read flows.
- Keep citation/source labels visible in all-corpus QA modes so answer citations do not become ambiguous.

## What package/library quirks matter?

- The Web host uses npm workspaces under `src/Hosts/Web`; run workspace scripts from that directory.
- Orval-generated clients, DTOs, hooks, query keys, and MSW mocks are generated artifacts and must not be manually edited.
- HeroUI usage belongs in React Web UI boundaries; do not create custom design-system wrappers unless they solve a current simplifying need.
- `apiFetch` and app-owned generated clients are app boundary concerns, not shared Web UI concerns.
- Date input and localization validators exist because React date and copy handling have repository-specific rules.
- The `docs/docs.csproj` project exists only for Visual Studio documentation browsing and is not an authoritative docs source.

## What fragile files should Codex treat carefully?

- `AGENTS.md` because it is the repository-wide architecture and delivery policy.
- `.agents/skills/*/SKILL.md` because project skills are task-specific operating manuals and routing sources.
- `.claude/skills/*` because Claude bridge skills should point back to canonical project skills.
- `docs/architecture/decisions/*` because accepted ADRs should not be reopened casually.
- `docs/architecture/conventions/adr-0003-implementation-conventions.md` and `docs/architecture/validation/adr-0003-interface-boundary-validation.md` because they define and validate module boundary rules.
- `src/Build/ModuleBuildProfiles.props` because it is the focused module build source of truth.
- `src/Hosts/Main/Main.csproj` because it owns the current Main host baseline module composition.
- `src/Hosts/Web/package.json` and `src/Hosts/Web/package-lock.json` because they define the npm workspace and lockfile state.
- Generated OpenAPI clients, DTOs, hooks, query keys, and MSW mocks because they must be regenerated, not manually edited.
- App identity and journey docs under `docs/apps/private-practice-general` and legacy `docs/apps/clinic-owner` because `clinic-owner` is compatibility/historical only.

## What current TODOs or temporary workarounds exist?

- `docs/apps/private-practice-general/README.md` states that `clinic-owner` is retained only as a temporary compatibility reference where explicitly needed.
- ADR-0003 rollout conventions mention `ReportOnly` modules as a visible baseline mode for non-migrated modules; this should not become permanent policy.
- Architecture validation forbids temporary exceptions once rollout is complete.
- Knowledge normal-user React UI currently uses an existing owner workspace response as a temporary read model; add a dedicated user-facing corpus query contract when backend support is available.
- Prescription template management still has known gaps: authorization policy hardening, medical reference-code availability lookup during load, explicit EF migration generation for environments without automatic schema management, richer handler/integration tests, and optional create-from-existing-prescription workflow.
- Tamin provider planning has pending work around provider endpoint quirks, idempotency by provider/code-system/version, and avoiding unsupported identity, pharmacy, price, allowed-count, digital-signature, Ditas, or paper fallback behavior unless real SDK methods exist.
