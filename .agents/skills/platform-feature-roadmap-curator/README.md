# Platform Feature Roadmap Curator

A reusable agent skill for documenting feature/artifact roadmaps and deriving implementation tickets after approval.

## What it does

The skill runs a short discovery conversation, creates a roadmap, and then turns an approved roadmap into implementation tickets or markdown task files.

It is designed to sit after user journey curation and before engineering implementation.

## Install

Copy this folder into your agent skills directory.

## Recommended repo layout

```txt
docs/product/roadmaps/
docs/product/tasks/
docs/product/journeys/
docs/architecture/decisions/
```

## Typical flow

1. User provides a feature/artifact objective.
2. Skill asks why it is being built, what the first useful version is, what is short-term, what is long-term, and what is out of scope.
3. Skill drafts a roadmap.
4. User approves or revises the roadmap.
5. Skill derives tickets/tasks with acceptance criteria and validation steps.

## Principle

Roadmap first. Tickets second. Implementation last.

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
