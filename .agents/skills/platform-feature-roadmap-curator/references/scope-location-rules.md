# Scope Location Rules

All product, roadmap, task, and architecture-decision documents must live in the workspace of the app/module they primarily belong to.

## Preferred workspace layout

```txt
docs/
  apps/
    {app-slug}/
      _index.md
      modules/
        {module-slug}/
          _index.md
          journeys/
          roadmaps/
          tasks/
          decisions/
  platform/
    _index.md
    modules/
      {module-slug}/
        _index.md
        roadmaps/
        tasks/
        decisions/
```

Use `docs/apps/...` for app-facing work.

Use `docs/platform/modules/...` for shared platform capabilities that are not owned by a single app, such as identity, search, files, intelligence, communication, or deployment.

## Ownership rule

Each document must have exactly one home folder.

If a document affects multiple apps or modules, place it under the primary owning app/module and link the affected apps/modules in metadata.

If there is no clear primary owner, place it under the nearest shared platform module.

Do not duplicate the same document under multiple folders.

## Metadata rule

Every document should include scope metadata:

```txt
App:
Module:
Capability:
Related apps:
Related modules:
```

For platform-level documents, use:

```txt
App: Platform
Module: <platform-module>
```

## Index rule

Indexes are navigation only. They are not the source of truth.

Maintain indexes at these levels when useful:

```txt
docs/apps/_index.md
docs/apps/{app-slug}/_index.md
docs/apps/{app-slug}/modules/{module-slug}/_index.md
docs/platform/_index.md
docs/platform/modules/{module-slug}/_index.md
```

The source document remains the single source of truth.

## Naming rule

Use kebab-case folder and file names.

Prefer module names that match product/code language closely enough to be searchable.

Examples:

```txt
docs/apps/clinic-owner/modules/patient-procedures/journeys/0001-patient-procedure-journey.md
docs/apps/clinic-owner/modules/patient-procedures/roadmaps/0001-patient-procedure-roadmap.md
docs/apps/clinic-owner/modules/patient-procedures/tasks/0001-patient-procedure-tasks.md
docs/apps/clinic-owner/modules/patient-procedures/decisions/ADR-0007-use-explicit-procedure-status.md
docs/platform/modules/search/decisions/ADR-0004-use-elasticsearch-for-search-retrieval.md
```
