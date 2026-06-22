---
name: platform-feature-roadmap-curator
description: Curate scoped feature and artifact roadmaps through a short discovery conversation, then derive approved roadmap phases into implementation tickets and tasks. Use when planning, documenting, sequencing, reviewing, or converting the roadmap of a feature, artifact, module, product capability, or design objective into delivery work.
---

# Platform Feature Roadmap Curator

## Purpose

Create and maintain roadmap documents for platform features, artifacts, modules, and design objectives.

This skill looks backward at available context, asks what the user is trying to achieve, documents what should happen next, and derives implementation tickets only after the roadmap is approved or explicitly treated as approved.

A roadmap is not just a backlog. It explains why the work exists, who benefits, what belongs now, what belongs later, and how future work stays aligned with product, user, and architecture goals.

## Use When

Use this skill when the user asks to:

- create a roadmap
- document a roadmap for a feature or artifact
- plan implementation phases
- turn a user journey into deliverable milestones
- define MVP, short-term, mid-term, and long-term objectives
- split a capability into releases
- decide what should be built now, next, or later
- derive tickets/tasks from an approved roadmap
- review or maintain an existing roadmap
- decide where a roadmap or task set belongs by app/module scope

## Relationship to Other Skills

Use the User Journey Curator first when the design objective is user-facing or workflow-heavy.

Use the ADR Steward when a roadmap item introduces or changes an architectural decision.

This skill connects roadmap items back to:

- user journey stages
- business objectives
- user objectives
- technical constraints
- architecture decisions
- implementation artifacts
- tickets and tasks

## Scope-First Repository Contract

Before creating or updating a roadmap or task set, classify its owning scope:

```txt
App:
Module:
Capability:
Related apps:
Related modules:
```

App-owned roadmaps and tasks:

```txt
docs/apps/{app-slug}/modules/{module-slug}/roadmaps/
docs/apps/{app-slug}/modules/{module-slug}/tasks/
```

Shared platform roadmaps and tasks:

```txt
docs/platform/modules/{module-slug}/roadmaps/
docs/platform/modules/{module-slug}/tasks/
```

Examples:

```txt
docs/apps/clinic-owner/modules/patient-procedures/roadmaps/0001-patient-procedure-roadmap.md
docs/apps/clinic-owner/modules/patient-procedures/tasks/0001-patient-procedure-tasks.md
docs/platform/modules/search/roadmaps/0002-semantic-retrieval-roadmap.md
```

Each roadmap and task file has exactly one home folder. If it affects multiple apps/modules, place it under the primary owner and list affected scopes in metadata.

Use `references/scope-location-rules.md` for placement rules.

## Core Rule

Do not start with implementation tasks.

Start with intent.

Every roadmap must answer:

1. Why is this being built?
2. Who benefits?
3. What user journey, pain point, or business objective does it serve?
4. What is the smallest useful version?
5. What is intentionally deferred?
6. What long-term direction must short-term work preserve?
7. What artifacts or features must be created?
8. What risks, dependencies, or architecture decisions affect sequencing?

## Approval Gate

Only generate final implementation tickets/tasks when one of these is true:

- the user says the roadmap is approved
- the roadmap status is `Approved`
- the user explicitly asks to treat the draft roadmap as approved for planning
- the user asks for provisional tickets and accepts that they are not ready for execution

If the roadmap is not approved, generate only `Proposed Tasks` and clearly mark them as draft.

Do not allow ticket generation to bypass the roadmap.

## Conversation Flow

When roadmap intent is incomplete, ask a compact set of questions before drafting.

Prefer 5 to 8 questions.

Do not ask for information that is already present in the conversation, journey map, ADR, issue, PRD, or code context.

If the user asks for best effort, proceed with assumptions and mark them clearly.

## Discovery Questions

Ask only the questions needed for the current roadmap.

Default questions:

1. What feature, artifact, or capability are we roadmapping?
2. Which app and module own it?
3. Why is it being built now?
4. Which user journey, pain point, or business objective does it serve?
5. What must be true in the first useful version?
6. What is short-term but not MVP?
7. What is the long-term direction?
8. What is explicitly out of scope?
9. What existing artifacts, modules, screens, services, or APIs are affected?
10. Are there architecture, security, tenant, data, or integration constraints?
11. What would make this roadmap successful?

## Roadmap Status Values

Use only:

- Draft
- Proposed
- Approved
- Deprecated
- Superseded

Do not mark a roadmap as Approved unless explicitly instructed or the source document already says it is approved.

## Roadmap Template

Use `assets/roadmap-template.md` for roadmap documents.

Required metadata includes:

- Date
- Status
- App
- Module
- Capability
- Owner
- Related journey
- Related ADRs
- Related modules
- Confidence

## Implementation Ticket Template

Use `assets/task-template.md` for task documents.

Each ticket must include:

- why the work exists
- scope
- acceptance criteria
- validation steps
- dependencies
- source roadmap section
- source journey stage when available
- ADR dependency when applicable
- clear out-of-scope notes

Prefer thin vertical slices over broad technical buckets.

Do not generate tickets that are only vague reminders.

Do not hide architecture decisions inside implementation tickets. Recommend an ADR when needed.

## Output Modes

### Mode: Ask Clarifying Questions

Use when essential roadmap intent is missing.

```md
## Roadmap Clarification

I need a little context before drafting the roadmap:

1.
2.
3.
4.
5.
```

### Mode: Draft Roadmap

Use when enough information exists.

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

### Mode: Roadmap Review

Use when reviewing an existing roadmap.

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

### Mode: Ticket Derivation

Use after roadmap approval or explicit provisional-ticket request.

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

## Roadmap Phases

Prefer:

- MVP / First Useful Version
- Short-Term
- Mid-Term
- Long-Term

Use “Now / Next / Later” only when the user wants a lighter roadmap.

Do not invent exact dates unless the user provides deadlines or release windows.

## Guardrails

Do not produce a roadmap that is only a task list.

Do not skip the “why”.

Do not put long-term ambitions into MVP unless they are required for the first useful version.

Do not create tickets before the roadmap is approved unless they are clearly provisional.

Do not generate tickets that require rewriting stable core logic unless explicitly justified.

Do not invent business priorities when they are unknown.

Do not duplicate documents across app/module folders.

Do not create multiple sources of truth.

If a roadmap implies an architecture change, recommend an ADR.

If a roadmap affects a user workflow, recommend or reference a user journey map.

## Style

Be concise.

Use practical product and engineering language.

Prefer tables for artifact maps, feature maps, and ticket summaries.

Prefer phases over dates unless dates are known.

Keep roadmaps strategic enough to guide direction and concrete enough to derive work.
