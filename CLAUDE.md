@AGENTS.md

## Claude Code

Claude Code reads `CLAUDE.md` at session start. `AGENTS.md` remains the canonical repository instruction file; this file only adds Claude-specific discovery guidance.

### Skill Discovery

- Canonical project skills live in `.agents/skills/<skill-name>/SKILL.md`.
- The shared skill operating model lives in `.agents/skills/README.md`.
- Claude-discoverable bridge skills live in `.claude/skills/<bridge-name>/SKILL.md`.
- Each bridge skill is only routing metadata. When a bridge skill is selected, read `.agents/skills/README.md`, then read and follow the referenced canonical `.agents/skills/<skill-name>/SKILL.md` before changing files.
- Do not treat `.claude/skills/` bridge files as replacements for canonical skill files.
- Do not duplicate full skill bodies into `.claude/skills/`; keep `.agents/skills/` as the single source of truth.
- When adding, renaming, or deleting a canonical skill, update the matching `.claude/skills/` bridge in the same change.
- A bridge name may be shorter than the canonical skill name; the bridge body names the canonical file to read.

### Default Claude Workflow

1. Read `AGENTS.md` and keep its architecture rules in force.
2. Read `.agents/skills/README.md` for the shared skill contract and ownership matrix.
3. Classify the task.
4. Select only the relevant skill bridges from `.claude/skills/`.
5. Read the canonical skill files under `.agents/skills/`.
6. Apply the skill instructions, then implement, test, and summarize the change.

For one focused pull request, start with `/platform-pr-delivery-loop`, then load only the task-specific skills required by the slice.
