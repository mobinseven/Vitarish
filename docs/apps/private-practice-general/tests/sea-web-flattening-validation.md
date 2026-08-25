# SEA Web flattening validation evidence

Status: Follow-up evidence for `Flatten sea-web concern-first source boundaries`
Owner: Web platform / Private Practice General
Scope: `@vitarish/sea` path-only source-boundary refactor
Related journey: N/A
Collected: 2026-07-13

## Purpose

Record the baseline and post-change validation evidence for flattening `@vitarish/sea` from the redundant `src/features/sea` wrapper into package-level concern roots. This document is evidence only; it does not define SEA behavior, public exports, query keys, generated contracts, or app navigation.

## Commits compared

| Revision | Commit | Notes |
| --- | --- | --- |
| Baseline | `3453696` | Parent of the SEA flattening commit. |
| Post-change | `91b4fe9` | `Flatten sea-web concern-first source boundaries`. |

Both revisions were validated from `src/Hosts/Web` after `npm install` and after running the SEA generated-client command for that revision. Generated output and `package-lock.json` changes caused by local validation were not committed.

## Validation summary

| Command | Baseline result | Post-change result | Classification |
| --- | --- | --- | --- |
| `npm run generate:api --workspace @vitarish/sea` | Pass, exit `0`. | Pass, exit `0`. | No regression. |
| `npm run typecheck --workspace @vitarish/sea` | Fail, exit `2`. | Fail, exit `2`. | Pre-existing blockers remain; paths changed from old SEA layout to new SEA layout only. |
| `npm run test --workspace @vitarish/sea` | Fail, exit `1`; 4 suites and 11 tests pass before 2 suites fail. | Fail, exit `1`; 4 suites and 11 tests pass before 2 suites fail. | Pre-existing auth generated-client blocker. |
| `npm run validate:web-ui-boundary` | Pass, exit `0`. | Pass, exit `0`. | No regression; post-change includes the `src/features/sea` reintroduction guard. |

## Failure classification

### Auth generated users client

`npm run typecheck --workspace @vitarish/sea` fails on both baseline and post-change because `auth-web` references generated users artifacts that are not present in the validation checkout:

```text
../auth-web/src/auth/userTypes.ts(5,86): error TS2307: Cannot find module '../api/generated/users/model' or its corresponding type declarations.
../auth-web/src/users.ts(1,34): error TS2307: Cannot find module './api/generated/users/users' or its corresponding type declarations.
```

`npm run test --workspace @vitarish/sea` fails on both baseline and post-change for the same `auth-web/src/users.ts` generated-client import after successfully running 4 SEA test suites and 11 tests:

```text
Test Files  2 failed | 4 passed (6)
Tests  11 passed (11)
Error: Failed to resolve import "./api/generated/users/users" from "../auth-web/src/users.ts".
```

Classification: pre-existing, not introduced by the SEA path-only refactor.

### HeroUI Pro type declarations

`npm run typecheck --workspace @vitarish/sea` fails on both baseline and post-change because `@heroui-pro/react` and subpath type declarations are unavailable in this validation environment. Baseline errors point to the old SEA paths and post-change errors point to the new concern-first paths, which confirms the same dependency blocker moved with the files rather than being introduced by the refactor.

Examples from baseline:

```text
src/features/sea/projects/components/SeaProjectsPage.tsx(4,62): error TS2307: Cannot find module '@heroui-pro/react' or its corresponding type declarations.
../web-ui/src/state/DetailGrid.tsx(4,26): error TS2307: Cannot find module '@heroui-pro/react/item-card' or its corresponding type declarations.
```

Examples from post-change:

```text
src/components/projects/SeaProjectsPage.tsx(4,62): error TS2307: Cannot find module '@heroui-pro/react' or its corresponding type declarations.
../web-ui/src/state/DetailGrid.tsx(4,26): error TS2307: Cannot find module '@heroui-pro/react/item-card' or its corresponding type declarations.
```

Classification: pre-existing environment/dependency blocker, not introduced by the SEA path-only refactor.

### Implicit-any cascades

The typecheck output contains implicit-any diagnostics in both baseline and post-change output. These follow the same affected component areas and are downstream of unavailable HeroUI Pro component types.

Classification: pre-existing cascade from the same type declaration blocker.

## Path checks

Post-change path checks from `src/Hosts/Web`:

```bash
rg -n 'features/sea' shared/sea apps shared/clinic
```

Result: no matches.

```bash
rg -n 'sea-web/src/features/sea' ../..
```

Result: one intentional validator guard reference:

```text
../../Hosts/Web/scripts/validate-web-ui-boundary.mjs:66:    path: resolve(__dirname, "../shared/sea/src/features/sea"),
```

A wider repository search still finds historical migration documentation under `docs/apps/private-practice-general/decisions/0001-migration-matrix.md`; those entries describe earlier app-to-package migration evidence and are intentionally historical.

## Cleanliness confirmation

- No generated client, generated DTO, generated hook, query-key, or MSW mock edits are committed.
- No package dependency or package-lock changes are committed.
- No compatibility barrels are retained under `src/features/sea`.
- The old `src/Hosts/Web/shared/sea/src/features` directory is absent after the refactor.
- Public root exports remain routed through `src/Hosts/Web/shared/sea/src/index.ts`; only internal source paths changed.

## Architecture audit

Artifact: SEA flattening validation evidence  
Type: Documentation  
Responsibility: Record baseline and post-change validation evidence for the SEA Web path-only refactor.

SRP:                    Pass - The document only records validation evidence for one refactor.  
Single state model:     Pass - It does not define runtime state or duplicate SEA behavior.  
Low coupling:           Pass - It references commands and files without creating code dependencies.  
Abstraction:            Pass - No abstraction is introduced.  
Command-query:          N/A - Documentation does not expose command/query APIs.  
Failure safety:         Pass - Validation failures are classified explicitly.  
Open/Closed:            Pass - Evidence is additive and does not alter stable source behavior.  
Core protection:        N/A - Documentation does not touch core logic.  
KISS/YAGNI:             Pass - It captures only the missing PR evidence requested by review.  
Single source of truth: Pass - It records evidence and does not redefine generated contracts, query keys, or business rules.
