---

name: platform-localization-maintenance
description: Review and maintain UI localization coverage by detecting hard-coded user-facing text, verifying IStringLocalizer usage, and preserving presentation-layer localization boundaries.
------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

# Platform Localization Maintenance

## Purpose

Use this skill to review and maintain localization coverage in user-facing UI artifacts.

This skill is a review and maintenance guide. It is not a product-copy owner, does not define translation policy, and must not introduce a new production-code localization abstraction.

Use it to:

* Detect and prevent hard-coded user-visible text in newly created or modified UI artifacts.
* Ensure UI components consistently inject and use `IStringLocalizer`.
* Keep localization concerns at the presentation/interface boundary.
* Preserve resource files or the existing localization provider as the single source of truth for translated text.
* Prevent Domain and Application artifacts from depending on UI localization infrastructure.

## Use When

Use this skill when creating, reviewing, or modifying user-facing UI artifacts, including:

* Razor components
* Razor code-behind files
* host pages
* layouts
* dialogs
* forms
* buttons
* menus
* tabs
* tables and grids
* notifications and snackbars
* empty states
* helper text
* validation messages
* status labels
* user-facing error messages
* confirmation prompts
* loading or progress text

Apply it especially under paths such as:

* `src/Apps/**/Interface/**`
* `src/Modules/**/Interface/**`
* `src/Hosts/**`
* `src/Interfaces/**`

## Review Checklist

When reviewing UI localization, verify that:

* User-visible labels, titles, helper text, button text, snackbar messages, dialog titles, empty states, status text, validation messages, fallback messages, and confirmation prompts use `Localizer[...]`.
* Any component that renders localized text injects `IStringLocalizer`, either through `@inject IStringLocalizer Localizer`, `@inject IStringLocalizer<T> Localizer`, or an equivalent `[Inject]` property.
* Dates, times, numbers, percentages, currency, and culture-sensitive values are formatted using the active culture.
* Pluralized text is handled intentionally and is not built through unsafe string concatenation.
* Enum and status values are not rendered directly unless they are intentionally mapped to localized display text.
* Resource keys follow the convention already used by nearby components or the owning module.
* Repeated common labels reuse existing keys where appropriate, such as `Save`, `Cancel`, `Delete`, `Search`, `Back`, `Next`, and `Close`.
* Grammar-sensitive sentences use full sentence keys instead of unsafe word-by-word composition.
* Composite localization is used only when the resulting phrase remains grammatically safe in supported languages, for example `Localizer["AddItem", Localizer["Source"]]`.
* Backend or application messages shown to users are represented as stable codes or identifiers where practical, then mapped to localized text in the presentation layer.
* Localization concerns do not leak into Domain or Application artifacts.
* Existing resource files or the existing localization provider remain the only source of translated user-facing text.
* Verify localized text passed through `DialogParameters` uses `.Value` when the target dialog parameter expects a `string`, for example `Localizer["Delete"].Value` instead of `Localizer["Delete"]`.

## Do Not Flag

Do not flag string literals or names that are technical rather than user-visible.

Examples that usually should not be localized:

* CSS classes
* CSS variable names
* icon names
* route fragments
* route names
* internal IDs
* HTML element IDs used only for scripting or testing
* component parameter names
* enum names that are not rendered directly
* property names that are not rendered directly
* technical constants
* policy names
* claim names
* permission names
* role names, unless displayed directly to users
* JavaScript interop identifiers
* event names
* telemetry names
* diagnostic logs, unless shown to users
* exception messages that are never displayed to users
* SVG/path data
* test selectors
* localization resource keys themselves

When in doubt, determine whether the string can appear in the rendered UI or in a user-facing notification. If not, do not file it as a localization issue.

## Architecture Boundaries

Localization is a presentation concern.

Allowed:

* UI components may request localized display text through `IStringLocalizer`.
* UI-facing adapters may map stable application result codes to localized text.
* Shared UI components may own reusable display keys when the copy belongs to the shared UI component.
* Resource files or the existing localization provider may contain translated text.

Not allowed:

* Domain artifacts must not depend on `IStringLocalizer`, Razor, MudBlazor, resource files, host UI, or localization infrastructure.
* Application artifacts must not depend on Razor, MudBlazor, host UI, or presentation localization infrastructure.
* Domain entities, value objects, commands, queries, handlers, and pure rules must not return translated UI text as business behavior.
* Do not introduce a new localization source of truth unless the task explicitly changes localization architecture and is covered by an ADR.

Preferred boundary pattern:

* Domain/Application returns stable result codes, validation codes, or structured result data.
* Interface/Presentation maps those codes to localized user-facing text.
* Resource files or the existing localization provider remain the single source of truth for translated copy.

## Key Selection Guidance

Prefer existing nearby conventions over inventing new naming styles.

When no local convention exists, use stable and readable keys:

* Common action labels may use simple reusable keys:

  * `Save`
  * `Cancel`
  * `Delete`
  * `Search`
  * `Close`
* Component-specific labels may use scoped keys:

  * `KnowledgeSource.Title`
  * `KnowledgeSource.EmptyState`
  * `KnowledgeSource.DeleteConfirmation`
* Validation and error text should identify the condition, not the current wording:

  * `Validation.Required`
  * `Validation.InvalidEmail`
  * `KnowledgeSource.ImportFailed`
* Avoid keys that encode temporary wording:

  * Prefer `DeleteConfirmation`
  * Avoid `AreYouSureYouWantToDeleteThisItem`

Use full-sentence keys for grammar-sensitive text. Avoid assembling user-facing sentences from many small localized words unless the supported languages make that safe.

## Review Method

Use static search to find candidates, then manually classify each result.

A string is a localization finding only if all of the following are true:

1. It is user-facing or can reasonably appear in the rendered UI.
2. It is not already localized.
3. It is not a technical literal.
4. Moving it to localization does not violate an existing architecture boundary.

Do not blindly replace every string literal. Static search results require human review.

## Output Format

For each finding, report:

* File path.
* Line number or search string.
* Current hard-coded text.
* Suggested resource key.
* Suggested replacement expression, for example `Localizer["KeyName"]`.
* Fix location.

Use this concise format:

```md
## Localization Findings

| File | Line/Search | Current text | Suggested key | Replacement | Fix location |
| --- | --- | --- | --- | --- | --- |
| `path/to/File.razor` | `Line 42` | `Save` | `Save` | `Localizer["Save"]` | Component and localization resource file |
```

If a finding requires a boundary fix, state it clearly:

```md
| `src/Modules/Orders/Application/...` | `Search: "Order was not found"` | `Order was not found` | `Orders.NotFound` | Return a stable result code from Application and localize it in Interface | Application result contract and Interface localization resource |
```

If no issues are found, state:

```md
No user-facing hard-coded UI text was identified in the reviewed scope.

Reviewed:
- `path/or/file`
- `path/or/file`

Searches used:
- `<command or search pattern>`
```

## Placement and Routing

Place this skill at:

```text
.agents/skills/platform-localization-maintenance/SKILL.md
```

Route localization review and maintenance tasks to this skill from the root `AGENTS.md` skill routing table when that table is maintained.

Do not add an ADR unless the task changes localization architecture, introduces a new localization source of truth, or changes how localization resources are resolved.

## Validation

Recommended static searches for likely hard-coded UI strings in `.razor` and `.razor.cs` files:

```bash
rg -n --glob '*.razor' '(^|[>[:space:]])([A-Z][A-Za-z0-9 ,.!?:;"()/-]{2,})(<|$)' src/Apps src/Modules src/Hosts src/Interfaces

rg -n --glob '*.razor' '"[A-Z][A-Za-z0-9 ,.!?:;()/-]{2,}"' src/Apps src/Modules src/Hosts src/Interfaces

rg -n --glob '*.razor.cs' '"[A-Z][A-Za-z0-9 ,.!?:;()/-]{2,}"' src/Apps src/Modules src/Hosts src/Interfaces

rg -n --glob '*.razor' '@(Localizer\[|inject\s+IStringLocalizer|using\s+Microsoft\.Extensions\.Localization)' src/Apps src/Modules src/Hosts src/Interfaces

rg -n --glob '*.razor.cs' '(IStringLocalizer|Localizer\[)' src/Apps src/Modules src/Hosts src/Interfaces
```

Additional searches that may help catch common UI text patterns:

```bash
rg -n --glob '*.razor' '(MudText|MudButton|MudAlert|MudChip|MudTooltip|MudDialog|MudSnackbar|MudMenuItem|MudTabPanel|MudInput|MudTextField|MudSelect|MudTable|MudDataGrid)' src/Apps src/Modules src/Hosts src/Interfaces

rg -n --glob '*.razor' '(Placeholder|Label|HelperText|Text|Title|AriaLabel|AdornmentText)="[^@{][^"]+"' src/Apps src/Modules src/Hosts src/Interfaces

rg -n --glob '*.razor.cs' '(Snackbar|Notification|Dialog|Message|Error|Warning|Success|Info).*\("[^"]+"\)' src/Apps src/Modules src/Hosts src/Interfaces
```

These searches intentionally over-match. Reviewers must distinguish user-facing text from technical literals before filing findings.

## Fix Guidance

When fixing a localization finding:

1. Add or reuse a resource key in the existing localization source.
2. Inject `IStringLocalizer` in the component if it is not already available.
3. Replace the hard-coded UI text with `Localizer["Key"]`.
4. For formatted text, use localizer arguments rather than string concatenation.
5. For error codes or status values, map the stable code to localized display text in the presentation layer.
6. Keep Domain and Application artifacts free of localization infrastructure.
7. Do not rename existing resource keys unless the task explicitly includes cleanup or migration.

Examples:

```razor
<MudButton>@Localizer["Save"]</MudButton>
```

```razor
<MudText>@Localizer["KnowledgeSource.EmptyState"]</MudText>
```

```csharp
Snackbar.Add(Localizer["KnowledgeSource.ImportSucceeded"], Severity.Success);
```

```csharp
var message = Localizer["KnowledgeSource.ImportedCount", importedCount];
```

Avoid:

```csharp
var message = "Imported " + importedCount + " items.";
```

Prefer:

```csharp
var message = Localizer["KnowledgeSource.ImportedCount", importedCount];
```
