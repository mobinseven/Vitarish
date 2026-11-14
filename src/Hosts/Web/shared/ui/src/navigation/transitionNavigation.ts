import { navigationTransitionQueryKey, navigationTransitionStorageKey, navigationTransitionVersion, parseNavigationTransition, withNavigationTransition, withoutNavigationTransition, type NavigationTransitionContext } from "./transitionContext";
import { themeModeAttribute, resolvedThemeModes } from "../themeMode";
import { defaultLocale, supportedLocales } from "@vitarish/localization/locales";
import { navigationTransitionDocument, navigationTransitionStates, type NavigationTransitionRuntime } from "./transitionContract";

let current: NavigationTransitionContext | null = null;

/** Capture and scrub the optional UI envelope without touching a ticket fragment. */
export function takeNavigationTransition(): NavigationTransitionContext {
  const runtime = (window as unknown as Record<string, NavigationTransitionRuntime>)[navigationTransitionDocument.runtime];
  if (runtime?.active()) return runtime.context;
  if (runtime) current = null;
  if (current && parseNavigationTransition(JSON.stringify(current))) return current;
  const url = new URL(window.location.href);
  const envelope = url.searchParams.get(navigationTransitionQueryKey);
  let stored: string | null = null;
  try { stored = window.sessionStorage.getItem(navigationTransitionStorageKey); } catch { /* Optional presentation storage may be denied. */ }
  current = parseNavigationTransition(envelope ?? stored) ?? {
    version: navigationTransitionVersion,
    attempt: window.crypto.randomUUID(),
    started: Date.now(),
    mode: document.documentElement.getAttribute(themeModeAttribute) === resolvedThemeModes.Dark ? resolvedThemeModes.Dark : resolvedThemeModes.Light,
    locale: supportedLocales.find(locale => locale === document.documentElement.lang) ?? defaultLocale,
  };
  if (envelope !== null) {
    window.history.replaceState(window.history.state, "", withoutNavigationTransition(window.location.href));
  }
  try { window.sessionStorage.setItem(navigationTransitionStorageKey, JSON.stringify(current)); } catch { /* Navigation is independent of storage availability. */ }
  runtime?.start(current);
  return current;
}

/** Use only for an existing first-party navigation target selected by its owning flow. */
export function carryNavigationTransition(target: string): string {
  const context = takeNavigationTransition();
  const runtime = (window as unknown as Record<string, NavigationTransitionRuntime>)[navigationTransitionDocument.runtime];
  runtime?.departing();
  if (new URL(target, window.location.href).origin === window.location.origin) return withoutNavigationTransition(target);
  // The envelope carries the cross-origin handover. Leaving a stored copy here
  // would resurrect this attempt when the tab later returns without an envelope.
  clearStoredNavigationTransition();
  return withNavigationTransition(target, context, window.location.origin);
}

export function clearNavigationTransition() {
  (window as unknown as Record<string, NavigationTransitionRuntime>)[navigationTransitionDocument.runtime]?.finish(navigationTransitionStates.Cancelled);
  current = null;
  clearStoredNavigationTransition();
}

function clearStoredNavigationTransition() {
  try { window.sessionStorage.removeItem(navigationTransitionStorageKey); } catch { /* Optional presentation storage may be denied. */ }
}

export function isCurrentNavigationTransition(attempt: NavigationTransitionContext): boolean {
  const runtime = (window as unknown as Record<string, NavigationTransitionRuntime>)[navigationTransitionDocument.runtime];
  return runtime ? runtime.active() && runtime.context.attempt === attempt.attempt : current?.attempt === attempt.attempt;
}

export function navigationTransitionSignal(): AbortSignal | undefined {
  return (window as unknown as Record<string, NavigationTransitionRuntime>)[navigationTransitionDocument.runtime]?.signal;
}
