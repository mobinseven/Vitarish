---
name: platform-simplification-audit
description: Finds deletion, reuse, standard-library or native replacements, duplicate state, speculative abstractions, unnecessary configuration, and avoidable dependencies. Use when simplification is requested or a plan/diff introduces notable new layers or parallel paths. Do not use as a mandatory ceremony for every change.
---

# Platform Simplification Audit

## Purpose

Find the smallest correct design without deleting necessary boundaries, validation, security, accessibility, localization, failure handling, or tests.

## Procedure

1. Understand the real flow, callers, and ownership before labeling code unnecessary.
2. Apply the minimum-change ladder from `AGENTS.md` in order: remove, reuse, standard library, native platform, installed dependency, cohesive edit, then minimum custom code.
3. Look for duplicate rules/state, parallel old/new paths without a removal condition, one-use wrappers, vendor abstractions that merely rename SDK calls, speculative options, and hand-rolled platform behavior.
4. Prefer fixing one shared root cause over patching multiple symptoms.
5. State what can be deleted or shrunk, the replacement, and the behavior/risk that must remain protected.
6. Route boundary-impacting recommendations to `platform-artifact-architecture` before implementation.

## Finding tags

`delete`, `reuse`, `stdlib`, `native`, `dedupe`, `yagni`, `shrink`, `keep`.

## Hard stops

- A proposed simplification weakens a trust boundary, data safety, cancellation, compatibility, accessibility, localization, observability, or meaningful test coverage.
- A shorter form is less explicit about lifecycle or failure behavior.

## Handoff

Return approved reductions to `platform-change-planning` or `platform-delivery-loop`.
