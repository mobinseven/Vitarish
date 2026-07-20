---
name: platform-skill-governance
description: Owns creation, merging, naming, routing, deletion, and validation of project skills. Use only for skill-system maintenance.
---

# Platform Skill Governance

## Rules

1. Organize by decision ownership, not folders or workflow phases.
2. Keep global invariants in `AGENTS.md`, routing exceptions in the index, and one repeatable procedure per skill.
3. Merge overlapping decision owners; split only when triggers, sources, procedures, and handoffs materially differ.
4. Use precise positive/negative triggers. Boundary skills must support plan, implementation, and review without duplicating orchestration.
5. Do not repeat global policy, route chains, purpose prose, or handoffs already clear from frontmatter/index.
6. Target ≤30 words for descriptions and ≤180 body words; exceed only for unique safety-critical rules.
7. Keep frontmatter to `name` and `description`; directory and name must match.
8. Update active indexes, bridges/metadata, and references after changes.

## Stop

- Skills overlap without a narrower boundary.
- A skill becomes a policy manual, ADR, roadmap, or archive.
- Ordinary tasks require most skills.
- Renames leave stale active references.
