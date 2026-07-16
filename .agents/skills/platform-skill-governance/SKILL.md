---
name: platform-skill-governance
description: Creates, merges, renames, deletes, routes, and validates Vitarish project skills while keeping AGENTS.md lean and decision ownership non-overlapping. Use only for skill-system or skill-index maintenance.
---

# Platform Skill Governance

## Purpose

Keep the skill collection small, discoverable, non-overlapping, and subordinate to `AGENTS.md`, accepted ADRs, and scoped documentation.

## Procedure

1. Start from decision ownership, not existing folder names or workflow phases.
2. Keep repository-wide invariants in `AGENTS.md`; keep routing detail in `.agents/skills/README.md`; keep one repeatable procedure in each skill.
3. Merge skills when they decide the same boundary. Split only when triggers, sources of truth, procedures, and handoffs are materially distinct.
4. Give every skill precise positive and negative triggers. Avoid broad descriptions that cause accidental loading.
5. Ensure technical boundary skills work in plan and implementation/review modes without duplicating orchestration.
6. Do not make individual skills reload the global route or repeat architecture policy.
7. Keep frontmatter to `name` and `description`; directory and name must match.
8. Update `AGENTS.md`, the skill index, host metadata/bridges actually used by the repository, and stale references when names change.

## Hard stops

- Two skills own the same decision without an explicit narrower boundary.
- A skill becomes a policy manual, architecture decision, product roadmap, or documentation archive.
- Routing requires loading most skills for ordinary tasks.
- A rename leaves stale active references.

## Handoff

Use `platform-artifact-architecture` only when skill changes also alter repository policy, placement, or durable architecture rules.
