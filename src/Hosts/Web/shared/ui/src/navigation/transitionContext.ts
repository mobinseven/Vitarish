import { supportedLocales, type SupportedLocale } from "@vitarish/localization/locales";
import { resolvedThemeModes, type ResolvedThemeMode } from "@vitarish/ui/theme-mode";

/** Optional presentation metadata. Never pass this envelope to authentication APIs. */
export const navigationTransitionQueryKey = "ui-transition";
export const navigationTransitionStorageKey = "vitarish.navigation-transition";
export const navigationTransitionVersion = 1;
export const navigationTransitionPolicy = {
  maximumAgeMilliseconds: 60_000,
  maximumEnvelopeLength: 256,
  slowMilliseconds: 8_000,
  recoveryMilliseconds: 30_000,
  revealMilliseconds: 150,
} as const;

export interface NavigationTransitionContext {
  version: typeof navigationTransitionVersion;
  attempt: string;
  started: number;
  mode: ResolvedThemeMode;
  locale: SupportedLocale;
}

/** Exact fields only: no destination, session, tenant or credential data. */
export function parseNavigationTransition(value: string | null, now = Date.now()): NavigationTransitionContext | null {
  return validateNavigationTransitionEnvelope(value, now, navigationTransitionPolicy, navigationTransitionVersion, supportedLocales, Object.values(resolvedThemeModes));
}

/** Self-contained so the pre-paint script uses this exact validator after bundling. */
export function validateNavigationTransitionEnvelope(value: string | null, now: number, policy: Pick<typeof navigationTransitionPolicy, "maximumEnvelopeLength" | "maximumAgeMilliseconds">, version: number, locales: readonly string[], modes: readonly string[]): NavigationTransitionContext | null {
  if (!value || value.length > policy.maximumEnvelopeLength) return null;
  try {
    const candidate: unknown = JSON.parse(value);
    if (!candidate || typeof candidate !== "object" || Array.isArray(candidate)) return null;
    const context = candidate as Record<string, unknown>;
    if (Object.keys(context).sort().join(",") !== "attempt,locale,mode,started,version"
      || context.version !== version
      || typeof context.attempt !== "string"
      || !/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(context.attempt)
      || typeof context.started !== "number" || !Number.isSafeInteger(context.started)
      || context.started > now || now - context.started >= policy.maximumAgeMilliseconds
      || typeof context.mode !== "string" || !modes.includes(context.mode)
      || typeof context.locale !== "string" || !locales.includes(context.locale)) return null;
    return context as unknown as NavigationTransitionContext;
  } catch {
    return null;
  }
}

/** The caller owns approval of the target. This function cannot select an origin or path. */
export function withNavigationTransition(target: string, context: NavigationTransitionContext, base: string): string {
  const url = new URL(target, base);
  const query = removeNavigationTransitionQuery(url.search, navigationTransitionQueryKey);
  url.search = `${query}${query ? "&" : "?"}${navigationTransitionQueryKey}=${encodeURIComponent(JSON.stringify(context))}`;
  return url.toString();
}

/** Removes only UI metadata, retaining authentication queries and fragment byte semantics. */
export function withoutNavigationTransition(target: string): string {
  const hashIndex = target.indexOf("#");
  const queryIndex = target.indexOf("?");
  if (queryIndex < 0 || (hashIndex >= 0 && hashIndex < queryIndex)) return target;
  const end = hashIndex < 0 ? target.length : hashIndex;
  const search = target.slice(queryIndex, end);
  if (!new URLSearchParams(search).has(navigationTransitionQueryKey)) return target;
  const stripped = removeNavigationTransitionQuery(search, navigationTransitionQueryKey);
  return target.slice(0, queryIndex) + stripped + target.slice(end);
}

/** Keep every other query pair byte-for-byte, including ordering and percent encoding. */
export function removeNavigationTransitionQuery(search: string, key: string): string {
  const remaining = search.replace(/^\?/, "").split("&").filter(pair => {
    try { return decodeURIComponent(pair.split("=", 1)[0].replace(/\+/g, " ")) !== key; }
    catch { return true; }
  }).join("&");
  return remaining ? `?${remaining}` : "";
}
