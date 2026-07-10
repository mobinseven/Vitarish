# Vitarish Project Skills

This directory is the canonical home for Vitarish project skills.

Skills are task-specific operating manuals. They route decisions, name sources of truth, and define the smallest safe procedure for a recurring class of work. They must not duplicate `AGENTS.md`, accepted ADRs, product docs, or implementation code.

## Canonical Layout

- Canonical skills live in `.agents/skills/<skill-name>/SKILL.md`.
- Skill UI metadata lives in `.agents/skills/<skill-name>/agents/openai.yaml`.
- Claude bridge skills live in `.claude/skills/<skill-name>/SKILL.md`.
- Bridge skills only route discovery. They must point to the canonical `.agents/skills/**/SKILL.md` file and must not duplicate instructions.
- Do not add per-skill README, changelog, starter, archive, or example files unless the skill directly executes or loads them.

## Current Skills

| Skill | Owns |
| --- | --- |
| `platform-scope-planning` | Product intent, journey/roadmap/task triage, owner scope, non-goals, smallest delivery slice |
| `platform-architecture-governance` | Artifact classification, placement, dependency direction, ADR impact, architecture audits |
| `platform-api-client-boundary` | OpenAPI endpoint metadata, contract DTOs, generated clients, apiFetch, API-to-React handoff |
| `platform-react-ui-boundary` | React Web UI placement, HeroUI usage, feature hooks, localization, accessibility, shared Web UI |
| `platform-test-strategy` | Required tests, regression checks, test placement, validation commands, hot scenarios |
| `platform-delivery-loop` | One focused delivery loop, implementation, validation evidence, review, feedback triage, automation state |
| `platform-simplification-audit` | Over-engineering review, deletion opportunities, reuse, stdlib/native replacement, YAGNI checks |
| `platform-skill-governance` | Skill collection design, metadata, bridges, `AGENTS.md` harmony, validation |

## Selection Rules

1. Load only the skill required by the current decision.
2. Prefer the narrowest skill that owns the decision.
3. Use `AGENTS.md` and accepted ADRs over skill text if they conflict.
4. Do not let two skills own the same decision. Use handoff between skills instead.
5. Do not create or load planning skills for small, local, already-scoped fixes.
6. Use the minimum-change ladder before creating new artifacts, folders, abstractions, or dependencies.
7. Keep scoped documents in one source-of-truth folder; indexes are navigation only.

## Default Chains

Use only the steps that apply:

```txt
New feature:
platform-scope-planning -> platform-architecture-governance -> platform-test-strategy -> platform-delivery-loop

OpenAPI to React:
platform-architecture-governance -> platform-api-client-boundary -> platform-react-ui-boundary -> platform-test-strategy -> platform-delivery-loop

React UI:
platform-react-ui-boundary -> platform-test-strategy -> platform-delivery-loop

Architecture-sensitive change:
platform-architecture-governance -> platform-test-strategy -> platform-delivery-loop

Simplification review:
platform-simplification-audit -> platform-architecture-governance when boundaries are affected

Skill maintenance:
platform-skill-governance -> platform-architecture-governance when repository policy changes
```

## Quality Contract

Every skill should:

- have one decision owner;
- have clear positive and negative triggers in the description and body;
- point to sources of truth instead of copying them;
- stay under 500 lines;
- use one-level references only when needed;
- define hard stops and handoffs;
- preserve the architecture pillars in `AGENTS.md`;
- avoid speculative ceremony and duplicate state.

## Change Rules

When adding, renaming, deleting, or materially changing a skill:

1. Update the canonical `.agents/skills/<skill-name>/SKILL.md`.
2. Update `.agents/skills/<skill-name>/agents/openai.yaml`.
3. Update the matching `.claude/skills/<skill-name>/SKILL.md` bridge.
4. Update `AGENTS.md`, `CLAUDE.md`, and this README when routing changes.
5. Search for stale old skill names before handoff.
6. Run the skill validator, or report why it could not run and use an equivalent structural check.
