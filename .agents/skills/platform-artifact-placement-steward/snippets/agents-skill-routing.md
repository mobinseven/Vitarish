# AGENTS.md skill-routing snippet

Add this section to the root `AGENTS.md` or to the nearest scoped `AGENTS.md`.

```md
## Skill Routing

Before modifying code, classify the task and explicitly select relevant skills.

- For artifact placement, module ownership, repository locations, dependency direction, test placement, docs impact, or ADR impact: use `$platform-artifact-placement-steward`.
- For frontend/backend isolation, ADR-0003 Interface/Contracts/Client/Api boundaries, or boundary validator findings: use `$platform-frontend-backend-isolation` if available, otherwise use `$platform-artifact-placement-steward` and read `references/adr-0003-boundary-rules.md`.
- For localization, hard-coded UI text, resource ownership, or `IStringLocalizer` usage: use `$platform-localization-maintenance`.
- For implementation of an accepted ADR or architecture drift from an ADR: use `$platform-adr-implementation-review`.

When a skill is used, include this line in the plan or final response:

`Applied skill: <skill-name>`
```
