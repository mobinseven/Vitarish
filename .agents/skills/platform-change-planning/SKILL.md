---
name: platform-change-planning
description: Produces evidence-based implementation plans with explicit scope, assumptions, artifact ownership, ordered slices, acceptance criteria, risks, and a minimal skill route. Use for non-trivial features, cross-stack work, ambiguous scope, or requests to derive implementation tasks. Do not use for small already-scoped fixes.
---

# Platform Change Planning

## Purpose

Turn a requested outcome into the smallest complete, reviewable plan. This skill owns plan structure and routing; technical boundary decisions belong to the applicable boundary skills.

## Use when

- The request spans multiple artifacts, layers, modules, hosts, or delivery slices.
- Product behavior, non-goals, assumptions, or acceptance criteria need clarification from repository evidence.
- The user asks for a plan, task breakdown, PR slicing, or implementation directions.

## Do not use

- A local fix is already scoped and its owner, behavior, and validation are obvious.
- The task is implementation-only against an approved, still-current plan; use `platform-delivery-loop`.

## Procedure

1. Inspect the current owner, callers, contracts, configuration, tests, validators, and relevant accepted ADRs before proposing files.
2. Separate confirmed repository facts, user requirements, assumptions, and unresolved decisions.
3. State the outcome and exclusions; remove speculative future support from the current slice.
4. Identify the smallest affected artifact set and the authoritative owner of each changed rule, state, and contract.
5. Route only to boundary skills touched by the plan. Let those skills supply boundary-specific decisions.
6. Order slices so each ends in a coherent, testable state and does not require manual edits to generated artifacts.
7. Define acceptance per slice, deterministic checks, runtime hot scenarios, compatibility impact, and revision triggers.
8. End with `Planning-only` or `Implementation authorized`; never blur the boundary.

## Required plan content

- Outcome and non-goals
- Repository evidence and assumptions
- Artifact/responsibility map
- Ownership and dependency direction
- Contract, security, data, generated-code, and compatibility impact
- Ordered delivery slices with acceptance
- Deterministic validation and runtime hot scenarios
- Risks, rejected alternatives, and revision triggers

## Hard stops

- A product, security, data-loss, compatibility, or external-provider choice materially changes the design and cannot be inferred safely.
- The proposed work cannot be divided into reviewable, independently valid slices.

## Handoff

Hand the approved plan to `platform-delivery-loop`. Route technical decisions to the narrow boundary skills listed in `.agents/skills/README.md`.
