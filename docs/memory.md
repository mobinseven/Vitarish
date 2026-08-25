# Memory

Purpose:
Durable repo-specific lessons previous sessions learned the hard way.
Do not duplicate AGENTS.md, architecture.md, ADRs, runbooks, or module docs.

## Read first

- `AGENTS.md` is the operating policy.
- `docs/codebase-map.md` is the navigation map.
- `docs/architecture.md` and ADRs define boundaries.
- This file only stores practical lessons and recurring pitfalls.

## Durable decisions to remember

- UTC `DateTime` for persisted instants.
- `private-practice-owner` and `patients` replace the retired `private-practice-general` web host; the backend Clinic and SEA modules remain active.
- Internal module API endpoints are protected by default.
- Generated clients/contracts are regenerated, not manually edited.

## Commands and failure modes

- Focused backend builds are inner-loop only.
- Merge-level backend confidence requires `ModuleBuildProfile=All` or full solution build.
- Run npm workspace commands from `src/Hosts/Web`.
- `fetch failed` usually means the .NET host is not running at `NEXT_PUBLIC_API_BASE_URL`.
- OpenAPI mutation `403` usually means CSRF/antiforgery mismatch.
- Wrong generated hooks often mean Orval points at the wrong spec.

## Recurring pitfalls

- Optional module references break focused builds.
- Do not hide disabled modules with preprocessor symbols.
- Query flows must not mutate state.
- Provider reference versions must not default to `unknown`.
- Tamin provider dates must use the provider’s expected calendar/date format.
- Knowledge normal-user QA must not reuse owner command components.

## Package/tool quirks

- Orval output is generated.
- `apiFetch` is a shared Web transport package concern; generated React clients are feature package concerns after ADR-0008 migration. Intelligence Web generation and realtime dictation are owned by `src/Hosts/Web/shared/intelligence`; do not reintroduce them in `clinic-web`. Other legacy app-bound generated output can exist until its feature package is extracted.
- Date input and localization validators enforce repo-specific React rules.
- `docs/docs.csproj` is for Visual Studio browsing only.

## Fragile files

- `src/Build/ModuleBuildProfiles.props`
- `src/Hosts/Main/Main.csproj`
- `src/Hosts/Web/package.json`
- `src/Hosts/Web/package-lock.json`
- generated OpenAPI clients/specs/hooks/query keys/mocks
- ADRs under `docs/architecture/decisions`

## Temporary workarounds

Use dated entries only. Each entry must include:

- Status
- Added date
- Owning module/doc
- Remove condition
- Lesson for Codex
