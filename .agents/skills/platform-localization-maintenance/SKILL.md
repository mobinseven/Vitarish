---
name: platform-localization-maintenance
description: Maintain UI localization coverage for Blazor and React web surfaces by detecting hard-coded user-facing text, enforcing the approved localization source of truth, and preserving presentation-layer boundaries.
---

# Platform Localization Maintenance

## Purpose

Use this skill when creating, reviewing, or fixing user-facing UI text.

It exists to keep localization global, central, and enforceable. It covers both active UI stacks:

- Blazor/Razor surfaces that use `IStringLocalizer` and resource files.
- React/Next surfaces that use the React web localization infrastructure under `src/Hosts/Web/src/i18n/`.

This skill is not a product-copy owner and must not invent a second localization source of truth. If a task changes localization architecture, add or update an ADR.

## Use When

Use this skill for UI artifacts that render or pass user-visible text, including pages, layouts, components, dialogs, forms, buttons, menus, tabs, tables, empty states, helper text, validation messages, status labels, toasts, snackbars, metadata, and confirmation prompts.

Apply it especially under:

```txt
src/Hosts/**
src/Apps/**/Interface/**
src/Modules/**/Interface/**
src/Interfaces/**
```

## Architecture Contract

Localization is a presentation concern.

Allowed:

- UI components may request localized text through the approved UI-stack localizer.
- UI adapters may map stable application result codes to localized display text.
- Shared UI components may own reusable display keys when the copy belongs to the shared UI component.
- Resource files or `src/Hosts/Web/src/i18n/messages.ts` may contain translated UI text.

Not allowed:

- Domain artifacts must not depend on UI localization, React, Razor, resource files, or presentation infrastructure.
- Application artifacts must not return translated UI text as business behavior.
- Generated API clients and OpenAPI contracts must not be edited to satisfy display-copy needs.
- Do not introduce page-local dictionaries, feature-local translation stores, or ad hoc `{ en, fa }` maps.

Preferred boundary:

```txt
Domain/Application stable code or data → Presentation maps to localized display text → UI renders localized string
```

## React Web Rules

The React web host source of truth is:

```txt
src/Hosts/Web/src/i18n/
```

Use:

- `messages.ts` for typed message keys and translations.
- `cultures.ts` for supported cultures, normalization, cookie name, and direction helpers.
- `localizer.ts` for `t(...)`, fallback lookup, date formatting, and number formatting.
- `server.ts` for request culture resolution.
- `client.tsx` for `LocaleProvider`, `useLocalizer()`, and client-side culture persistence.

Checklist:

- User-visible React strings use `useLocalizer().t("Message.Key")`.
- Repeated labels use shared keys such as `Common.Save` instead of component-local text.
- Arrays that feed menus, cards, tabs, or settings links store `labelKey: MessageKey`, not `label: string`.
- Formatted text uses placeholders, for example `t("Account.SignedInAs", { name })`.
- `<html lang>` and `dir` come from the central culture resolver, not page-local code.
- Culture changes update the server preference where applicable and persist the culture cookie.
- Dates, times, numbers, percentages, and currency use the active localizer or `Intl` with the active culture.
- Full sentences are localized as full sentences when grammar can change by language.

Avoid:

```tsx
<Button>Save</Button>
<StatusMessage message="Failed to update language" />
const labels = { en: "Name", fa: "نام" };
```

Prefer:

```tsx
const { t } = useLocalizer();

<Button>{t("Common.Save")}</Button>
<StatusMessage message={t("Culture.UpdateFailed")} />
```

## Blazor/Razor Rules

Checklist:

- User-visible Razor strings use `Localizer[...]`.
- Components that render localized text inject `IStringLocalizer` or `IStringLocalizer<T>`.
- Dialog parameters use `.Value` when the target parameter expects `string`.
- Existing resource files or the existing localization provider remain the source of truth.
- Dates, times, numbers, percentages, and currency use the active culture.

Examples:

```razor
<MudButton>@Localizer["Save"]</MudButton>
<MudText>@Localizer["KnowledgeSource.EmptyState"]</MudText>
```

```csharp
Snackbar.Add(Localizer["KnowledgeSource.ImportSucceeded"], Severity.Success);
var message = Localizer["KnowledgeSource.ImportedCount", importedCount];
```

## Do Not Flag

Do not flag technical literals that are not displayed to users:

- CSS classes and CSS variable names.
- Routes, route fragments, and route names.
- HTML IDs used only for scripting or tests.
- Icon names.
- Import paths and module names.
- Query keys and generated API operation names.
- Claim, policy, permission, and role names unless displayed directly.
- Telemetry names and diagnostic logs unless shown to users.
- Localization keys themselves.
- SVG/path data.
- Test selectors.

When in doubt, ask whether the string can appear in rendered UI or a user-facing notification. If not, do not file it as a localization issue.

## Key Selection

Prefer existing nearby conventions.

When no convention exists:

- Common labels: `Common.Save`, `Common.Cancel`, `Common.Delete`, `Common.Search`, `Common.Close`.
- Feature/component labels: `Account.SettingsTitle`, `Culture.UpdateFailed`.
- Validation/error labels: `Validation.Required`, `Validation.InvalidEmail`.
- Use condition-based keys, not temporary wording.

Avoid composing grammar-sensitive sentences from many small localized words.

## Review Method

A string is a finding only when all are true:

1. It is user-facing or can reasonably appear in rendered UI.
2. It is not already localized.
3. It is not a technical literal.
4. Moving it to localization does not violate boundaries.

Do not blindly replace every string literal. Static search results require human review.

## Validation

React web host:

```bash
cd src/Hosts/Web
npm run validate:localization
npm run validate:localization:strict
npm run typecheck
npm run test
```

Use report mode while migrating legacy hard-coded React strings. Use strict mode for new or fully migrated scopes.

Blazor/Razor candidate searches:

```bash
rg -n --glob '*.razor' '(^|[>[:space:]])([A-Z][A-Za-z0-9 ,.!?:;"()/-]{2,})(<|$)' src/Apps src/Modules src/Hosts src/Interfaces
rg -n --glob '*.razor' '"[A-Z][A-Za-z0-9 ,.!?:;()/-]{2,}"' src/Apps src/Modules src/Hosts src/Interfaces
rg -n --glob '*.razor.cs' '"[A-Z][A-Za-z0-9 ,.!?:;()/-]{2,}"' src/Apps src/Modules src/Hosts src/Interfaces
rg -n --glob '*.razor' '@(Localizer\[|inject\s+IStringLocalizer|using\s+Microsoft\.Extensions\.Localization)' src/Apps src/Modules src/Hosts src/Interfaces
```

## Output Format

For each finding, report:

```md
## Localization Findings

| File | Line/Search | Current text | Suggested key | Replacement | Fix location |
| --- | --- | --- | --- | --- | --- |
| `path/to/File.tsx` | `Line 42` | `Save` | `Common.Save` | `t("Common.Save")` | `messages.ts` and component |
```

If no issues are found:

```md
No user-facing hard-coded UI text was identified in the reviewed scope.

Reviewed:
- `path/or/file`

Searches used:
- `<command or search pattern>`
```

## Fix Guidance

1. Add or reuse a key in the approved localization source.
2. Inject/use the approved localizer for that UI stack.
3. Replace hard-coded UI text with a localized expression.
4. Use localizer arguments/placeholders instead of string concatenation.
5. Map stable application codes to localized display text in Presentation.
6. Keep Domain and Application free of localization infrastructure.
7. Do not rename existing keys unless cleanup or migration is explicitly in scope.

## References

- `.agents/skills/platform-localization-maintenance/references/react-localization.md`
