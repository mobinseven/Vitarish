---
name: platform-simplification-audit
description: Finds safe deletion, reuse, native/standard replacements, duplicate state, speculative abstractions, unnecessary configuration, and avoidable dependencies. Use when simplification is material.
---

# Platform Simplification Audit

## Rules

1. Trace real flow, callers, and ownership before judging code unnecessary.
2. Apply the `AGENTS.md` minimum-change ladder in order.
3. Find duplicate rules/state, unbounded old/new coexistence, one-use wrappers, SDK-renaming abstractions, speculative options, and hand-rolled platform behavior.
4. Prefer one shared root-cause fix over symptom patches.
5. Tag findings `delete`, `reuse`, `stdlib`, `native`, `dedupe`, `yagni`, `shrink`, or `keep`; state replacement and protected behavior/risk.
6. Route recommendations that alter boundaries through architecture review before implementation.

## Stop

Do not reduce trust, data safety, cancellation, compatibility, accessibility, localization, observability, failure clarity, or meaningful tests merely to shorten code.
