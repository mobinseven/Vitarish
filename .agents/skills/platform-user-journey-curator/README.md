# Platform User Journey Curator Skill

This package defines a reusable agent skill for curating Vitarish user journey maps and deriving implementation work from them.

The central rule is:

> For each design objective, create or update the user journey map first, then derive implementation artifacts, feature slices, acceptance criteria, technical implications, tests, and metrics from that map.

## Folder Structure

```txt
platform-user-journey-curator/
  SKILL.md
  README.md
  agents/openai.yaml
  assets/
    journey-template.md
    implementation-derivation-template.md
    journey-index-template.md
  references/
    journey-curation-rules.md
    platform-architecture-principles.md
  scripts/
    validate_journeys.py
  examples/
    patient-procedure-journey.md
  starter/
    docs/product/journeys/
      _index.md
      _template.md
      0001-patient-procedure-journey.md
```

## Install

Copy the `platform-user-journey-curator` directory into your skills directory.

## Suggested Usage

```txt
Use the Platform User Journey Curator skill to create a journey map for tenant prescription template management.
```

```txt
Use the Platform User Journey Curator skill to derive implementation artifacts from this journey.
```

```txt
Use the Platform User Journey Curator skill to review this PR against the patient procedure journey.
```

## Design Philosophy

The skill treats a journey map as a product-design source of truth.

It avoids jumping directly from an objective to code. It first clarifies:

- persona
- goal
- scope
- journey stages
- emotions
- pain points
- opportunities
- success metrics
- technical implications

Then it derives the smallest coherent implementation slices.

## Validation

Run:

```bash
python scripts/validate_journeys.py starter/docs/product/journeys
```

The validator checks for required journey sections.

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
