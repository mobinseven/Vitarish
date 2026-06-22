---
name: platform-user-journey-curator
description: Curate scoped user journey maps for Vitarish design objectives, then derive implementation artifacts, feature slices, acceptance criteria, risks, metrics, and follow-up work from the journey. Journeys are grouped by owning app/module.
---

# Platform User Journey Curator

## Purpose

Maintain user journey maps as the first product-design artifact for every design objective.

For each design objective, create or refine a journey map before proposing implementation artifacts, features, UI changes, data model changes, APIs, tests, or migration work.

The journey map is the source of truth for the user's goal, emotional arc, touchpoints, pain points, opportunities, and implementation implications.

## Use When

Use this skill when the user asks to:

- create a user journey map
- analyze a product or feature objective
- turn a design objective into implementation tasks
- derive UI, domain, API, data, or workflow requirements from a journey
- review whether a proposed implementation satisfies a journey
- identify gaps, pain points, or opportunities in an existing flow
- convert code review observations into user-facing design implications
- maintain journey documents under the product documentation tree
- decide where a journey belongs by app/module scope

## Scope-First Repository Contract

Before creating or updating a journey, classify its owning scope:

```txt
App:
Module:
Capability:
Related apps:
Related modules:
```

Put the journey in the workspace of the app/module it primarily belongs to:

```txt
docs/apps/{app-slug}/modules/{module-slug}/journeys/
```

For shared platform workflows with no single app owner, use:

```txt
docs/platform/modules/{module-slug}/journeys/
```

Examples:

```txt
docs/apps/clinic-owner/modules/patient-procedures/journeys/0001-patient-procedure-journey.md
docs/apps/trainer-console/modules/client-onboarding/journeys/0001-client-invite-journey.md
```

Each journey has exactly one home folder. If it affects multiple apps/modules, place it under the primary owner and list affected scopes in metadata.

Use `references/scope-location-rules.md` for placement rules.

## Authority Rules

The skill may:

- draft journey maps
- refine existing journey maps
- derive implementation artifacts from a journey
- identify gaps between current implementation and desired user experience
- propose feature slices, UI changes, data model changes, API changes, tests, and metrics
- update local journey indexes
- mark assumptions and confidence level
- recommend the owning app/module folder

The skill must not:

- skip the journey and jump straight to implementation
- invent evidence without marking it as an assumption
- treat technical implementation as the user journey
- duplicate one journey across multiple folders
- create multiple conflicting sources of truth
- convert every observation into a feature without prioritization
- hide uncertainty
- overcomplicate the journey with unnecessary ceremony

## Core Rule

For every design objective:

1. Classify the owning app/module scope.
2. Understand the objective.
3. Create or update the journey map.
4. Extract user problems and opportunities.
5. Derive implementation artifacts from the journey.
6. Prioritize the smallest coherent feature slices.
7. Preserve traceability from feature work back to journey stages.

No implementation plan is complete unless each proposed artifact can be traced to a journey stage, pain point, opportunity, or success metric.

## Journey Template

Use `assets/journey-template.md` for journey maps.

Required context metadata includes:

- App
- Module
- Capability
- Product / service
- Journey type
- Scenario
- User goal
- Business goal
- Scope start
- Scope end
- Primary persona
- Evidence sources
- Confidence level

## Journey Types

Use `Current state` when documenting what exists.

Use `Future state` when designing the target journey.

Use `Current state with gaps` when reviewing an existing implementation and deriving what should change next.

Default to `Current state with gaps` when the input includes code review observations or missing features.

## Evidence and Confidence

Always identify evidence sources.

Examples:

- user interview
- product requirement
- support ticket
- analytics
- static code review
- implementation inspection
- stakeholder assumption
- design objective only

Set confidence level:

- `High`: validated by user research, analytics, or production evidence
- `Medium`: supported by code, stakeholder knowledge, or repeated observations
- `Low`: inferred from limited context

If evidence is weak, continue with clear assumptions instead of blocking progress.

## From Journey to Implementation

After the journey exists, derive implementation work in this order:

1. Journey gap
2. User need
3. Product opportunity
4. Required artifact
5. Feature slice
6. Acceptance criteria
7. Technical implications
8. Test coverage
9. Metrics

Prefer small feature slices that produce visible user value.

Do not create technical tasks that cannot be traced back to the journey.

## Artifact Types

When deriving work, classify artifacts as:

- UI component
- page or route
- dialog or form
- domain model
- metadata extension
- state machine
- application service
- API endpoint
- permission rule
- validation rule
- migration
- background job
- notification
- audit/history record
- test
- documentation
- ADR candidate

If a derived artifact changes architecture, recommend using the Platform ADR Steward skill.

## Output Modes

### Mode: Create Journey

Use when creating a journey map from a design objective.

Output:

```md
## Proposed File

<scoped path>/journeys/NNNN-title-journey.md

## Journey Map

<journey document>

## Immediate Implementation Candidates

<small prioritized list>

## Index Updates

<local and global index rows>

## Assumptions

<explicit assumptions>
```

### Mode: Derive Implementation

Use when the user already has a journey and wants artifacts or features.

Output:

```md
## Journey-Derived Implementation Plan

### Source journey

### Owning scope

### Objective

### Highest-value gaps

### Required artifacts

### Feature slices

### Acceptance criteria

### Technical implications

### Tests

### Metrics

### ADR candidates
```

### Mode: Review Implementation Against Journey

Use when checking whether code, a plan, or a PR satisfies the journey.

Output:

```md
## Journey Fit Review

Classification: Fits / Partially Fits / Does Not Fit / Needs Human Judgment

Owning scope:

### Covered journey needs

### Missing journey needs

### Implementation gaps

### Recommended next slice

### Risks
```

### Mode: Normalize Journeys

Use when maintaining journey documents.

Output:

```md
## Journey Maintenance Summary

Files reviewed:

Changes proposed:

Problems found:

Index updates:

Follow-up recommendations:
```

## Quality Rules

A good journey map must:

- have one clear user goal
- have one clear business goal
- define owning app/module scope
- define scope start and scope end
- name the primary persona
- show stages in sequence
- include actions, touchpoints, questions, emotions, pain points, needs, opportunities, metrics, and evidence
- identify moments that matter
- include cross-journey insights
- derive implementation artifacts traceably
- mark assumptions and confidence level

A poor journey map:

- describes only screens
- lists technical tasks without user motivation
- lacks evidence
- hides uncertainty
- mixes multiple unrelated journeys
- jumps to implementation before understanding the experience
- lacks metrics or acceptance signals

## Style

Be direct and practical.

Use product language first, technical language second.

Keep journey maps readable.

Use tables only when they improve traceability.

Prefer the smallest useful artifact over a large speculative design.

Preserve separation between user experience, product opportunity, and technical implementation.
