---
name: platform-skill-governance
description: Maintains the Vitarish project skill collection, skill routing, bridge skills, AGENTS.md harmony, metadata, progressive disclosure, and validation. Use when creating, merging, deleting, renaming, criticizing, or optimizing skills, AGENTS routing, or Claude bridge skills.
---

# Platform Skill Governance

## Purpose

Keep project skills lean, uniform, discoverable, and subordinate to `AGENTS.md` and accepted ADRs. This skill owns skill collection design; it does not own product, architecture, API, UI, or test decisions.

## Use

Use this skill for `.agents/skills/**`, `.claude/skills/**`, skill routing in `AGENTS.md` or `CLAUDE.md`, skill metadata, skill consolidation, bridge-skill maintenance, and skill authoring reviews.

Skip it for ordinary code changes that only use existing skills.

## Read

- `AGENTS.md`
- `.agents/skills/README.md`
- Canonical skill files under `.agents/skills/<skill-name>/SKILL.md` that are being changed.
- Bridge files under `.claude/skills/<skill-name>/SKILL.md` that are being changed.

## Procedure

1. Start from decision ownership, not current folder names. Merge or delete skills when one responsibility covers the intended use.
2. Check AGENTS.md → Default Chains for any chain containing this skill. If the task also touches another skill from that chain, confirm the chain's stated order before proceeding — do not run out of sequence without a stated reason.
3. Keep `AGENTS.md` as the architecture and routing source of truth. Skills may point to it; they must not duplicate its full rules.
4. Keep every skill body concise, procedural, and under 500 lines.
5. Use only `name` and `description` in `SKILL.md` frontmatter unless a host-specific validator explicitly allows more.
6. Write descriptions in third person with clear triggers and no angle brackets.
7. Use one-level references only when detail is too large for `SKILL.md`.
8. Do not add per-skill README, changelog, starter, example, or archive files unless they are directly executed or loaded by the skill.
9. Keep `.claude/skills/**` as bridges that point to canonical `.agents/skills/**` files.
10. Regenerate or update `agents/openai.yaml` when skill names or descriptions change.
11. Validate metadata and search for stale skill names before handoff.

## Output

```md
## Skill Governance
Collection changes:
Routing changes:
Removed duplication:
Sources of truth:
Validation:
Trade-offs:
Rejected alternatives:
```

## Hard Stops

- A skill conflicts with `AGENTS.md` or an accepted ADR.
- Two skills own the same decision without a clear handoff.
- A skill becomes a policy manual, product roadmap, architecture decision, or documentation archive.
- A bridge file duplicates canonical instructions.
- A change leaves stale routing in `AGENTS.md`, `CLAUDE.md`, `.agents/skills/README.md`, or `.claude/skills/**`.
- A doc, ADR, or validator script this skill names under Read or Validation cannot be found or read. Stop and report the gap. Do not proceed as if the rule does not apply.

## Validation

Run the skill validator for each changed canonical skill. If the provided validator dependency is unavailable, run an equivalent structural check for frontmatter, names, descriptions, bridge targets, and stale references, then report the validator limitation.
