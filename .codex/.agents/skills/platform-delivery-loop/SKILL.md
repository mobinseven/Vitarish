---
name: platform-delivery-loop
description: Executes authorized changes through plan revalidation, focused slices, boundary-specific skills, validation, diff review, and precise handoff. Do not use for planning-only work.
---

# Platform Delivery Loop

## Rules

1. Re-read current files; repository state overrides stale plan details.
2. State objective, non-goals, expected contract/data/security/generated-output effects, and success evidence.
3. Load only touched boundary skills.
4. Implement coherent, preferably buildable/testable slices; change authoritative inputs before generated outputs.
5. Prefer deletion, reuse, standard/native behavior, installed dependencies, and cohesive edits over new abstractions.
6. Validate as soon as each slice can prove behavior, not only at the end.
7. Review the final diff for scope creep, duplicate truth, wrong placement, inward leaks, command/query mixing, unsafe failures, manual generated edits, missing cleanup, and stale references.
8. Classify unresolved feedback as Must-fix, Follow-up, or Rejected with reason.
9. Handoff using `AGENTS.md` completion criteria.

## Stop

- A required product, security, data-loss, compatibility, or provider decision is missing.
- Scope no longer forms one focused delivery.
- Progress requires weakening safeguards or editing generated output manually.
