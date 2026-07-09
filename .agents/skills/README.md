# Vitarish Agent Skills

This directory is the canonical home for project skills.

A skill is a task-specific operating manual. It should route work, define boundaries, state required checks, and produce consistent outputs. It must not duplicate `AGENTS.md` architecture rules or become a second source of truth for product or architecture decisions.

## Canonical Roots

- Canonical skill files live in `.agents/skills/<skill-name>/SKILL.md`.
- Claude bridge skills live in `.claude/skills/<bridge-name>/SKILL.md`.
- Bridge skills only route discovery. They must point back to the canonical `.agents/skills/**/SKILL.md` file and must not duplicate full instructions.
- `AGENTS.md` is the routing index and architecture rule source. This README defines the shared skill operating model.

## Shared Skill Contract

Every canonical skill should follow this structure unless a section is not applicable:

```md
---
name: <skill-name>
description: <when to use this skill, in one concise sentence>
---

# <Readable Skill Name>

## Purpose
## Use When
## Do Not Use For
## Relationship to Other Skills
## Sources of Truth
## Operating Procedure
## Required Output
## Hard Stops
## Guardrails
## Handoff
## Style
```

Use the same meaning for these sections across all skills:

| Section | Meaning |
| --- | --- |
| Purpose | The one responsibility of the skill. |
| Use When | Positive routing triggers. |
| Do Not Use For | Negative routing triggers that prevent skill overlap. |
| Relationship to Other Skills | Upstream/downstream skills and boundary ownership. |
| Sources of Truth | Files, ADRs, schemas, or conventions the skill must respect. |
| Operating Procedure | The minimum ordered steps. |
| Required Output | The expected shape of the skill result. |
| Hard Stops | Conditions that must block or redirect the work. |
| Guardrails | Ongoing rules that prevent drift, duplication, or over-design. |
| Handoff | The next skill or workflow step when this skill is complete. |
| Style | Tone, brevity, and formatting guidance. |

## Skill Families

### Product Discovery and Planning

```txt
platform-user-journey-curator -> platform-feature-roadmap-curator -> platform-adr-steward if architecture changes
```

Use these skills to clarify user intent, product scope, sequencing, acceptance criteria, and architecture impact before implementation work starts.

### Architecture and Placement

```txt
platform-adr-steward -> platform-artifact-placement-steward -> platform-frontend-backend-isolation when cross-layer boundaries are involved
```

Use these skills to preserve ownership, dependency direction, source-of-truth rules, and accepted ADRs.

### API-to-React Delivery

```txt
platform-openapi-contract-steward -> platform-generated-react-client-steward -> platform-feature-ui-integration-steward -> platform-localization-maintenance when user-facing copy changes
```

Use this chain when backend HTTP contracts drive generated clients and React/HeroUI feature integration.

### Testing

```txt
platform-test-curator
```

Use this skill to decide the smallest useful test set and to separate deterministic unit/component/contract tests from environment-dependent hot scenarios.

### PR and Automated Loop Control

```txt
platform-pr-delivery-loop
platform-auto-plan-generator -> platform-auto-plan-executor -> platform-auto-loop-reviewer -> platform-loop-state-manager
```

Use the PR delivery loop for human-directed one-PR work. Use the automated loop skills only for issue-driven automation and loop state transitions.

## Routing Rules

1. Load only the skills required by the current task.
2. Start with the narrowest skill that owns the current decision.
3. Prefer upstream skills only when the current task lacks product, roadmap, architecture, placement, or testing context.
4. Do not run every skill by default.
5. Do not let two skills own the same decision. Pick the owner, then let other skills review or receive handoff.
6. When a skill conflicts with `AGENTS.md` or an accepted ADR, follow `AGENTS.md` or the ADR and update the skill later.
7. Keep scoped documents in one source-of-truth folder; indexes are navigation only.

## Decision Ownership Matrix

| Decision | Owning skill |
| --- | --- |
| User goal, pain, journey stage, workflow fit | `platform-user-journey-curator` |
| MVP, phase, release slice, roadmap-to-ticket derivation | `platform-feature-roadmap-curator` |
| Architecture decision, supersession, accepted-decision conflict | `platform-adr-steward` |
| File/project/folder ownership and dependency placement | `platform-artifact-placement-steward` |
| Interface/Contracts/Client/Api/Application/Domain boundary | `platform-frontend-backend-isolation` |
| OpenAPI operation, schema, auth, errors, breaking change | `platform-openapi-contract-steward` |
| Generated client, Orval, apiFetch, generated DTO/hooks/MSW | `platform-generated-react-client-steward` |
| React/HeroUI feature hook, form/table/dialog integration | `platform-feature-ui-integration-steward` |
| User-facing React localization and formatting | `platform-localization-maintenance` |
| Required tests and hot scenarios | `platform-test-curator` |
| One focused PR lifecycle | `platform-pr-delivery-loop` |
| Issue-to-plan automation | `platform-auto-plan-generator` |
| Plan execution automation | `platform-auto-plan-executor` |
| Automated implementation review | `platform-auto-loop-reviewer` |
| Loop state and termination | `platform-loop-state-manager` |

## Skill Quality Checklist

A skill is healthy when it:

- has one clear responsibility;
- has both positive and negative routing rules;
- names upstream and downstream skills;
- references sources of truth instead of copying them;
- has hard stops for unsafe or out-of-scope work;
- produces a predictable output shape;
- avoids broad ceremony for small changes;
- prevents duplicate source-of-truth documents;
- preserves the architecture rules in `AGENTS.md`;
- can be followed by humans and automation without contradictory decisions.

## Change Rules

When adding, renaming, deleting, or materially changing a skill:

1. Update the canonical `.agents/skills/<skill-name>/SKILL.md`.
2. Update the matching `.claude/skills/**` bridge if discovery changes.
3. Update `AGENTS.md` routing when the task trigger changes.
4. Update this README when the shared operating model or ownership matrix changes.
5. Do not duplicate a full skill body into another skill, `AGENTS.md`, `CLAUDE.md`, or a bridge file.
