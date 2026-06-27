---
name: platform-feature-roadmap-curator
description: Create, review, sequence, and maintain scoped feature/artifact roadmaps, then derive approved roadmap phases into implementation tickets. Use for roadmap planning, MVP/phase definition, release slicing, scope placement, roadmap review, and approved ticket derivation.
---

# Platform Feature Roadmap Curator

## Purpose

Create roadmap documents that explain intent, scope, sequencing, and delivery direction for platform features, artifacts, modules, and design objectives. A roadmap is not a backlog: it must clarify why the work exists, who benefits, what belongs now/later, and how short-term work protects long-term product and architecture goals.

## Use When

Use this skill for requests to create, review, maintain, scope, phase, or convert a roadmap for a feature, artifact, module, product capability, design objective, user journey, MVP, release plan, or approved task set.

Use **User Journey Curator** first for user-facing/workflow-heavy design objectives. Use **ADR Steward** when roadmap items introduce or change architecture decisions.

## Operating Rules

1. **Scope first.** Classify ownership before creating/updating a roadmap or task set:

   ```txt
   App:
   Module:
   Capability:
   Related apps:
   Related modules:
   ```

2. **One home folder.** Store each roadmap/task set under one primary owner and list affected scopes in metadata.

   | Scope | Roadmaps | Tasks |
   | --- | --- | --- |
   | App-owned | `docs/apps/{app-slug}/modules/{module-slug}/roadmaps/` | `docs/apps/{app-slug}/modules/{module-slug}/tasks/` |
   | Platform/shared | `docs/platform/modules/{module-slug}/roadmaps/` | `docs/platform/modules/{module-slug}/tasks/` |

   Use `references/scope-location-rules.md` for placement decisions.

3. **Intent before tasks.** Do not start with implementation tickets. Every roadmap must answer: why, who benefits, served journey/pain/business objective, smallest useful version, deferred scope, long-term direction, required artifacts/features, risks/dependencies/ADRs.

4. **Approval gate.** Generate execution-ready tickets only when the roadmap is explicitly approved, has `Status: Approved`, the user says to treat it as approved, or the user requests provisional tickets. Otherwise output only clearly marked `Proposed Tasks`.

5. **No hidden architecture decisions.** If a roadmap implies an architecture change, recommend an ADR instead of burying the decision in a ticket.

## Discovery

Ask only for missing information already unavailable from conversation, journey maps, ADRs, issues, PRDs, or code. Prefer 5-8 compact questions. If the user asks for best effort, proceed with explicit assumptions.

Default discovery set:

1. What feature/artifact/capability is being roadmapped?
2. Which app/module owns it?
3. Why now?
4. Which user journey, pain point, or business objective does it serve?
5. What is required for the first useful version?
6. What is short-term but not MVP?
7. What is the long-term direction?
8. What is out of scope?
9. What existing artifacts/screens/services/APIs are affected?
10. What architecture, security, tenant, data, or integration constraints apply?
11. What defines success?

## Roadmap Contract

Status values: `Draft`, `Proposed`, `Approved`, `Deprecated`, `Superseded`. Do not mark `Approved` unless explicitly instructed or already present in source.

Use `assets/roadmap-template.md`. Required metadata:

```txt
Date:
Status:
App:
Module:
Capability:
Owner:
Related journey:
Related ADRs:
Related modules:
Confidence:
```

Preferred phases: `MVP / First Useful Version`, `Short-Term`, `Mid-Term`, `Long-Term`. Use `Now / Next / Later` only for lighter roadmaps. Do not invent dates unless deadlines or release windows are provided.

## Ticket Contract

Use `assets/task-template.md`. Each ticket must include why, scope, acceptance criteria, validation steps, dependencies, source roadmap section, source journey stage when available, ADR dependency when applicable, and out-of-scope notes.

Prefer thin vertical slices over broad technical buckets. Do not generate vague reminders or tickets that rewrite stable core logic without explicit justification.

## Output Modes

### Ask Clarifying Questions

```md
## Roadmap Clarification

I need a little context before drafting the roadmap:

1.
2.
3.
4.
5.
```

### Draft Roadmap

```md
## Proposed File

<scoped path>/roadmaps/NNNN-title-roadmap.md

## Roadmap Draft

<roadmap content>

## Index Updates

<local and global index rows>

## Follow-up Items

<open questions or recommended next steps>
```

### Roadmap Review

```md
## Roadmap Review

Owning scope:
Strengths:
Gaps:
Missing decisions:
Scope risks:
Sequencing risks:
Recommended changes:
```

### Ticket Derivation

```md
## Implementation Ticket Derivation

Source roadmap:
Roadmap status:
Owning scope:
Execution readiness: Ready / Provisional

## Proposed File

<scoped path>/tasks/NNNN-title-tasks.md

## Ticket Drafts

<ticket content>

## ADR Required

Yes / No / Possible

## Journey Impact

<journey traceability>
```

## Guardrails

- Do not produce a roadmap that is only a task list.
- Do not skip the why.
- Do not move long-term ambitions into MVP unless required for the first useful version.
- Do not bypass roadmap approval to create execution-ready tickets.
- Do not invent unknown business priorities.
- Do not duplicate documents across app/module folders.
- Do not create multiple sources of truth.
- Reference a journey map when user workflow is affected.
- Keep output concise, practical, and concrete enough to guide delivery.
