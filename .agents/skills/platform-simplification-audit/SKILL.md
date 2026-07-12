---
name: platform-simplification-audit
description: Reviews code, plans, or diffs for over-engineering, unnecessary abstractions, duplicate behavior, avoidable dependencies, hand-rolled standard-library logic, and deletion opportunities while preserving validation, security, accessibility, and architecture rules. Use for simplification reviews, bloat audits, refactor criticism, or "what can we remove" requests.
---

# Platform Simplification Audit

## Purpose

Find the smallest correct design. This skill owns over-engineering criticism and deletion opportunities; it does not replace correctness, security, accessibility, or architecture review.

## Use

Use this skill when the user asks to simplify, criticize, audit for bloat, remove unnecessary code, review over-engineering, reduce dependencies, or compare a plan against the minimum-change ladder.

Skip it when the request is a normal correctness review unless over-engineering is also in scope.

## Read

- `AGENTS.md`
- The changed files, callers of touched functions, package manifests, and existing local conventions.

## Procedure

1. Understand the real flow before calling something unnecessary.
2. Check `.agents/skills/README.md` → Default Chains for any chain containing this skill. If the task also touches another skill from that chain, confirm the chain's stated order before proceeding — do not run out of sequence without a stated reason.
3. Apply the ladder: does it need to exist, does it already exist, does the standard library do it, does the platform do it, does an installed dependency do it, can one line or one existing pattern cover it, and only then keep custom code.
4. Prefer fixing root causes in shared functions over patching only the reported symptom path.
5. Tag findings as:
   - `delete`: remove dead code, speculative features, unused flexibility, or duplicate state.
   - `reuse`: use an existing local helper, type, component, route, hook, validator, or convention.
   - `stdlib`: replace hand-rolled behavior with standard library behavior.
   - `native`: replace code or dependency with a platform/browser/database/framework feature already in use.
   - `yagni`: remove abstractions, configuration, or layers with no current second use.
   - `shrink`: keep behavior but reduce code or files.
6. Never simplify away trust-boundary validation, explicit failure handling, cancellation, security, accessibility, localization, architecture boundaries, or required tests.

## Output

Use one finding per line:

```md
<file>:<line>: <tag>: <what to remove or simplify>. <replacement>.
```

End with:

```md
net: -<estimated lines/files/dependencies> possible.
```

If there is nothing meaningful to cut, say `Lean already. Ship.`

## Hard Stops

- Do not recommend deleting validation, tests, accessibility affordances, error handling, or boundary checks merely to reduce line count.
- Do not recommend a clever shorter form when it weakens edge-case behavior.
- Do not add a new abstraction as a simplification unless it removes real duplication now.
- A doc, ADR, or validator script this skill names under Read or Validation cannot be found or read. Stop and report the gap. Do not proceed as if the rule does not apply.

## Handoff

Route correctness findings to ordinary review, architecture issues to `platform-architecture-governance`, and test gaps to `platform-test-strategy`.
