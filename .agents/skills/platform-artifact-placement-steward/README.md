# Platform Artifact Placement Steward

This skill classifies source, test, and documentation artifacts before they are added, moved, or reviewed.

It protects the repository structure by answering:

- what kind of artifact is this?
- which app/module/submodule/host/Web host/context owns it?
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

```txt
Use the Platform Artifact Placement Steward. I need to add a React page and feature components to the Web host. Where should they live?
```

```txt
Use the Platform Artifact Placement Steward. I need to add Medical/Prescription pages, components, hooks, and UI models. Keep them organized by module and submodule.
```

## Core rule

Classify first. Place second. Implement third.

## Scope model

Generated or reviewed artifacts should be scoped to one of:

- `src/Apps/<App>`
- `src/Modules/<Module>` including `Interface/Components/<Capability>`, `Interface/Models/<Capability>`, `Interface/Services/<Capability>`, and established module UI composition entries
- `src/Hosts/<Host>` including Razor pages grouped by module/capability and React Web host `src`, pages, features, components, generated API clients, and assets
- `src/Identity/<BoundedContext>`
- `src/Interfaces/Interfaces.<Name>`
- `src/Libraries` or `src/Modules/Base`
- `tests/...`
- `docs/...`

## Modular UI example

When a module has nested capabilities, keep pages and components aligned with ownership:

```txt
src/Modules/Medical/Interface/Components/Prescription/PrescriptionEditor.razor
src/Modules/Medical/Interface/Models/Prescription/PrescriptionEditorModel.cs
src/Hosts/Web/Pages/Medical/Prescription/PrescriptionPage.razor
src/Hosts/Web/src/pages/medical/prescription/PrescriptionPage.tsx
src/Hosts/Web/src/features/medical/prescription/components/PrescriptionEditor.tsx
src/Hosts/Web/src/features/medical/prescription/hooks/usePrescriptionWorkflow.ts
```

Pages compose route state and layout. Reusable UI belongs in module Interface components, React feature components, host components, or shared interface packages according to ownership.

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
