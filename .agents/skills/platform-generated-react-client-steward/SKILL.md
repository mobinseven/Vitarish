---
name: platform-generated-react-client-steward
description: Maintain the generated OpenAPI-to-React client boundary; govern Orval output, custom apiFetch/mutator, generated DTOs/hooks/query keys, MSW mocks, and no-manual-edit rules.
---

# Platform Generated React Client Steward

## Purpose

Use this skill to keep generated frontend API clients replaceable, isolated, and boring.

Generated code is transport infrastructure. It must not become the feature API, domain model, or UI state model.

## Use When

- Adding or changing Orval generation.
- Regenerating TypeScript DTOs, request functions, React Query hooks, query keys, or MSW handlers.
- Creating or changing `apiFetch`, custom mutators, auth headers, cookies/tokens, base URL handling, cancellation, or ProblemDetails mapping.
- Reviewing imports from `src/api/generated`.

## Do Not Use For

- Designing user workflows or screen composition.
- Adding business behavior to generated DTOs.
- Editing generated files by hand.

## Boundary Rules

1. Generated files live under `src/api/generated/**`.
2. Generated files are never manually edited.
3. Runtime behavior lives in one custom `apiFetch`/mutator boundary.
4. `apiFetch` owns base URL, auth, cookies/tokens, headers, cancellation, content negotiation, and ProblemDetails normalization.
5. Generated DTOs are transport shapes; feature/UI models may wrap or map them when needed.
6. Generated hooks are not imported directly by pages, forms, tables, dialogs, or design components.
7. MSW mocks are generated from the same OpenAPI source used for the client.
8. Regeneration must be deterministic and safe to review.
9. Do not fork generated clients to patch contract issues; fix the OpenAPI contract or generator configuration.
10. Query key ownership must be clear enough for feature hooks to invalidate/update data safely.

## Review Procedure

1. Identify the OpenAPI input and generated output folder.
2. Verify generated output is isolated and reproducible.
3. Inspect the mutator/client boundary instead of generated internals.
4. Check that ProblemDetails, auth, base URL, and cancellation are centralized.
5. Search for forbidden direct imports from generated hooks in UI/page code.
6. Confirm MSW output matches the current contract when mocks are needed.
7. Ensure any generation diff reflects contract changes, not manual edits.

## Required Output

```txt
Generated boundary: <path>
OpenAPI source: <path/url>
Runtime policy owner: <apiFetch/mutator path>
Decision: <accept / fix / reject>

Passes:
- <what is correct>

Fixes:
- <minimal required changes>

Forbidden imports or edits:
- <path or none>
```

## Hard Stops

Stop and correct if:

- Generated files were manually edited.
- Auth, base URL, or ProblemDetails logic appears in generated files or UI components.
- UI components import generated hooks directly.
- Mock handlers come from a different contract than the client.
- Regeneration is not reproducible.
- A generated DTO is treated as the single source of business truth.

## Handoff

Use `platform-feature-ui-integration-steward` when generated clients are ready to be wrapped into feature-level hooks and UI-ready contracts.

## Localization Boundary Addendum

Generated DTOs, request functions, hooks, query keys, and MSW output must not contain localized display text. Stable error/status codes may flow through generated transport shapes, but React UI maps those codes to localized messages at `src/Hosts/Web/src/localization/**`. Do not manually edit generated output and do not change backend contracts solely to fit a visual component string shape.
