---
name: platform-localization-maintenance
description: Review and maintain UI localization coverage by detecting hard-coded user-facing text, verifying IStringLocalizer usage, and preserving presentation-layer localization boundaries.
---

# Platform Localization Maintenance

## Purpose

Use this skill as a review and maintenance guide for localization coverage in UI artifacts. It is not a product-copy owner and does not introduce a production-code abstraction.

- Detect and prevent hard-coded user-visible strings in newly created or modified UI artifacts.
- Ensure components consistently inject and use `IStringLocalizer`.
- Keep localization concerns in the presentation/interface layer.
- Preserve resource files or the existing localization provider as the single source of truth for translated text.

## Use When

Use this skill when reviewing or creating user-facing UI artifacts, including:

- Razor components
- host pages
- dialogs
- forms
- buttons
- notifications and snackbars
- empty states
- helper text
- validation messages
- status labels
- user-facing error messages

Apply it especially under paths such as:

- `src/Apps/**/Interface/**`
- `src/Modules/**/Interface/**`
- `src/Hosts/**`
- `src/Interfaces/**`

## Review Checklist

- Verify each user-visible label, helper text, button label, snackbar message, dialog title, empty state, status text, validation message, and fallback message uses `Localizer[...]`.
- Verify the component has either `@inject IStringLocalizer Localizer` or an equivalent `[Inject] IStringLocalizer Localizer` property when it renders localized text.
- Verify dates, times, numbers, currency, and pluralized text are culture-aware.
- Verify enum/status values are not rendered directly unless intentionally localized through a resource key.
- Verify proposed resource keys are named consistently with nearby components.
- Verify localization does not leak into Domain or Application artifacts.
- Verify composite localizer usage like `Localizer["AddItem", Localizer["Source"]]` or `Localizer["AddItemHelp", Localizer["Source"]]` is used to avoid excessive localization key generation.
- Verify backend messages, if passed to frontend, are passed through the localization service.

## Do Not Flag

Do not flag string literals or names that are technical rather than user-visible, including:

- CSS classes
- icon names
- route fragments
- internal IDs
- component parameter names
- technical constants
- SVG/path data
- diagnostic logs unless they are shown to users
- enum/property names that are not rendered directly

## Architecture Boundaries

- UI components may request localized display text through `IStringLocalizer`.
- Domain and Application artifacts must not depend on `IStringLocalizer`, Razor, MudBlazor, host UI, or localization infrastructure.
- Application results should prefer stable error/code identifiers where practical; presentation components should map those identifiers to localized text.
- Resource files or the existing localization provider remain the single source of truth for translated text.
- This skill may suggest resource keys, but final product copy and translations belong to the owning app/module team.

## Output Format

For each finding, report:

- File path.
- Line number or search string.
- Current hard-coded text.
- Suggested resource key.
- Suggested replacement expression, for example `Localizer["KeyName"]`.
- Whether the fix belongs in the component, code-behind, shared UI component, or localization resource file.

Use this concise format:

```md
## Localization Findings

| File | Line/Search | Current text | Suggested key | Replacement | Fix location |
| --- | --- | --- | --- | --- | --- |
| `path/to/File.razor` | `Line 42` | `Save` | `Save` | `Localizer["Save"]` | Component and localization resource file |
```

If no issues are found, state that no user-facing hard-coded UI text was identified in the reviewed scope and list the searches or files reviewed.

## Placement and Routing

- Add this skill file at `.agents/skills/platform-localization-maintenance/SKILL.md`.
- Route localization review and maintenance tasks to this skill from the root `AGENTS.md` skill routing table when that table is maintained.
- Do not add an ADR unless the task changes the localization architecture or introduces a new localization source of truth.

## Validation

Recommended static searches for likely hard-coded UI strings in `.razor` and `.razor.cs` files:

```bash
rg -n --glob '*.razor' '(^|[>[:space:]])([A-Z][A-Za-z0-9 ,.!?:;"()/-]{2,})(<|$)' src/Apps src/Modules src/Hosts src/Interfaces
rg -n --glob '*.razor' '"[A-Z][A-Za-z0-9 ,.!?:;()/-]{2,}"' src/Apps src/Modules src/Hosts src/Interfaces
rg -n --glob '*.razor.cs' '"[A-Z][A-Za-z0-9 ,.!?:;()/-]{2,}"' src/Apps src/Modules src/Hosts src/Interfaces
rg -n --glob '*.razor' '@(Localizer\[|inject\s+IStringLocalizer|using\s+Microsoft\.Extensions\.Localization)' src/Apps src/Modules src/Hosts src/Interfaces
rg -n --glob '*.razor.cs' '(IStringLocalizer|Localizer\[)' src/Apps src/Modules src/Hosts src/Interfaces
```

Static search findings require human review because many string literals are technical and should not be localized. Reviewers must distinguish user-visible text from CSS classes, icon names, route fragments, IDs, parameter names, constants, SVG data, and non-user-facing diagnostics before filing findings.
