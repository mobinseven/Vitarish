# Platform Artifact Placement Steward

This skill classifies source, test, and documentation artifacts before they are added, moved, or reviewed.

It protects the repository structure by answering:

- what kind of artifact is this?
- which app/module/host/context owns it?
- where should it live?
- what dependencies are allowed?
- where should related tests and docs go?
- does this imply an ADR?

## Install

Copy the folder to your skills directory:

```txt
skills/platform-artifact-placement-steward/
```

## Typical use

```txt
Use the Platform Artifact Placement Steward. I need to add ProcedureStatus and ProcedureCancellationReason for patient procedures. Where should they go?
```

```txt
Use the Platform Artifact Placement Steward. Review this PR diff and identify misplaced artifacts.
```

```txt
Use the Platform Artifact Placement Steward. Classify the artifacts needed by this roadmap task before implementation.
```

## Core rule

Classify first. Place second. Implement third.

## Scope model

Generated or reviewed artifacts should be scoped to one of:

- `src/Apps/<App>`
- `src/Modules/<Module>`
- `src/Hosts/<Host>`
- `src/Identity/<BoundedContext>`
- `src/Libraries` or `src/Modules/Base`
- `tests/...`
- `docs/...`

## Validation script

The script validates a simple JSON manifest of planned artifacts:

```bash
python scripts/validate_artifact_placement.py artifact-manifest.json
```

Example manifest:

```json
[
  {
    "artifact": "ProcedureStatus.cs",
    "artifact_type": "domain enum",
    "path": "src/Modules/Medical/Domain/Enums/ProcedureStatus.cs"
  }
]
```

The script is intentionally conservative. It does not replace human review.
