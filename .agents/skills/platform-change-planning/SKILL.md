---
name: platform-change-planning
description: Produces evidence-based plans with scope, assumptions, ownership, ordered slices, acceptance, risks, and minimal skill routing. Use for non-trivial planning, not scoped implementation.
---

# Platform Change Planning

## Rules

1. Inspect current owners, callers, contracts, configuration, tests, validators, and relevant ADRs before naming files.
2. Separate confirmed facts, requirements, assumptions, and unresolved decisions.
3. State outcome and non-goals; exclude speculative future support.
4. Identify the smallest artifact set and owner of each changed rule, state, and contract.
5. Load only touched boundary skills for technical decisions.
6. Order slices so each ends coherent and testable; change authoritative inputs before generated output.
7. For each slice define acceptance, deterministic checks, required hot scenarios, compatibility effects, risks, rejected alternatives, and revision triggers.
8. End with `Planning-only` or `Implementation authorized`.

A complete plan includes an artifact/responsibility map, dependency direction, trust/data/contract/generated-output impact, and delivery ordering—not merely files or generic steps.

## Stop

- A material product, security, data-loss, compatibility, or provider decision cannot be inferred safely.
- Work cannot be divided into reviewable, independently valid slices.
