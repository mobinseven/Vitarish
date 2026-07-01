# React Web Localization Reference

## Source of truth

The React web host localizes user-facing text through the infrastructure under:

```txt
src/Hosts/Web/src/i18n/
```

Ownership is split by responsibility:

- `cultures.ts`: supported cultures, default culture, culture cookie name, direction helpers, and culture normalization.
- `messages.ts`: the typed message catalog. English keys define `MessageKey`; every supported culture must provide every key.
- `localizer.ts`: message lookup, fallback behavior, placeholder formatting, and culture-aware number/date formatters.
- `server.ts`: request culture resolution from the culture cookie and `Accept-Language`.
- `client.tsx`: `LocaleProvider`, `useLocalizer()`, and browser-side culture persistence.

Do not create page-local dictionaries, feature-local translation stores, or ad hoc `const labels = { en, fa }` maps. Add or reuse a key in `messages.ts`.

## Required React pattern

Use the localizer inside client components:

```tsx
const { t } = useLocalizer();

return <Button>{t("Common.Save")}</Button>;
```

For arrays rendered by UI components, store keys instead of rendered labels:

```tsx
const links = [
  { labelKey: "Account.Email", href: Routes.Account.Email },
] as const satisfies ReadonlyArray<{ labelKey: MessageKey; href: string }>;
```

For formatted text, use placeholders instead of string concatenation:

```tsx
t("Account.SignedInAs", { name: displayName });
```

## Request culture flow

1. `RootLayout` resolves the request culture with `resolveRequestCulture()`.
2. `<html>` receives the resolved `lang` and `dir`.
3. The root provider passes the culture into `LocaleProvider`.
4. Client components call `useLocalizer()`.
5. The account culture page saves the server preference and persists the culture cookie through `persistClientCulture()`.

## Rules

- Localize every user-visible label, heading, helper text, status message, empty state, confirmation prompt, validation message, button text, and page metadata string.
- Keep route paths, CSS classes, icon names, query keys, generated API names, telemetry names, and test selectors unlocalized.
- Add new cultures by extending `supportedCultures`, `supportedCultureOptions`, and `messages` together.
- Use full-sentence keys for grammar-sensitive copy.
- Never assemble translated phrases from many translated words unless the target languages make that grammar safe.
- Keep Domain, Application, generated API clients, and OpenAPI contracts free of UI localization infrastructure.

## Validation

From `src/Hosts/Web`:

```bash
npm run validate:localization
npm run validate:localization:strict
npm run typecheck
npm run test
```

Use report mode while migrating legacy hard-coded React strings. Use strict mode for new or fully migrated scopes.
