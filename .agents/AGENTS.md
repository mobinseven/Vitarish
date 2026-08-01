# Skill-system maintenance

Keep canonical skills under `.agents/skills`; synchronize active bridges,
metadata, validators, and references after changes. Add a boundary skill only
when maintenance changes that boundary's decision procedure.

Organize skills by decision ownership. Merge overlapping owners and split only
when triggers, sources, procedures, and handoffs materially differ. Keep global
invariants in root `AGENTS.md`; do not copy policy, architecture documentation,
route chains, purpose prose, or obvious agent behavior into skills.

Use precise positive and negative triggers. Keep frontmatter to `name` and
`description`, match the directory name, target descriptions at 30 words or
fewer and bodies at 180 words or fewer, and exceed those limits only for unique
safety-critical guidance. Stop if ordinary tasks require most skills or a skill
becomes a policy manual, ADR, roadmap, or archive.
