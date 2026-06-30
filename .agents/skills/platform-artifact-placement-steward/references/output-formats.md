# Output Formats

## Single Artifact Placement

Use this format for a single artifact:

```md
## Artifact Placement

Artifact:

Classification:

Owning scope:

Preferred location:

Reason:

Allowed dependencies:

Disallowed dependencies:

Related tests:

Documentation impact:

ADR impact:

ADR-0003 impact:

Modular UI impact:

React/Web host impact:

Validation impact:

Notes:
```

## Multiple Artifact Placement Plan

Use this format for multiple artifacts:

```md
## Artifact Placement Plan

| Artifact | Classification | Owning scope | Preferred location | Related tests | ADR impact | ADR-0003 impact | Modular UI impact | React/Web host impact |
|---|---|---|---|---|---|---|---|---|

## Placement Notes

## Risks / Conflicts

## Recommended Next Step
```

## Placement Review Output

When reviewing existing files or a PR, use:

```md
## Artifact Placement Review

Classification summary:

Correct placements:

Possible misplacements:

Ownership conflicts:

Dependency risks:

ADR-0003 boundary risks:

Modular UI risks:

React/Web host risks:

Suggested moves:

Tests/docs/ADR impact:

Validation impact:
```
