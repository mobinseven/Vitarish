# Platform ADR Steward Skill

A repo-scoped agent skill for maintaining platform Architecture Decision Records.

## Install

Copy this directory to:

```txt
.agents/skills/platform-adr-steward/
```

Codex-style agents discover repository skills from `.agents/skills` and load the full `SKILL.md` only when the skill is selected.

## What It Does

- drafts ADRs
- reviews whether changes need ADRs
- detects possible conflicts with accepted ADRs
- proposes supersession links
- maintains `_index.md`
- provides a validator script for deterministic ADR hygiene checks

## What It Does Not Do

- accept decisions without explicit approval
- delete ADR history
- silently rewrite accepted decisions
- create architecture decisions from nothing

## Suggested Repository Layout

```txt
docs/
  architecture/
    decisions/
      _index.md
      _template.md
      0001-record-architecture-decisions.md

.agents/
  skills/
    platform-adr-steward/
      SKILL.md
      assets/
      references/
      scripts/
```

## Bootstrap ADR Files

Starter files are included under:

```txt
starter/docs/architecture/decisions/
```

Copy them into your repository's `docs/architecture/decisions/` folder.

## Validate ADRs

```bash
python .agents/skills/platform-adr-steward/scripts/validate_adrs.py docs/architecture/decisions
```

The validator checks:

- filename format
- sequential numbering
- status validity
- required metadata
- required sections
- basic index presence

## Scope-first document layout

Generated documents should be stored under the owning app/module workspace, not only by artifact type.

```txt
docs/apps/{app-slug}/modules/{module-slug}/
  journeys/
  roadmaps/
  tasks/
  decisions/
```

Shared platform capabilities use:

```txt
docs/platform/modules/{module-slug}/
  roadmaps/
  tasks/
  decisions/
```

A document has one home folder. Cross-app or cross-module effects are recorded with metadata and links, not duplicated files.
