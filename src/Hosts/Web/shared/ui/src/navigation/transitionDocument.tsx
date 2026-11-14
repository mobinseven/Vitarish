import { navigationTransitionMessages } from "./navigationMessages";
import { navigationTransitionPolicy, navigationTransitionQueryKey, navigationTransitionStorageKey, navigationTransitionVersion, validateNavigationTransitionEnvelope, removeNavigationTransitionQuery, type parseNavigationTransition, type NavigationTransitionContext } from "./transitionContext";
import { supportedLocales, defaultLocale, getLocaleDirection, type SupportedLocale } from "@vitarish/localization";
import { themeModeAttribute, resolvedThemeModes, themePreferenceStorageKey } from "../themeMode";
import { navigationTransitionStyles } from "./transitionStyles";

import { navigationTransitionDocument, navigationTransitionStates, type NavigationTransitionRuntime, type NavigationTransitionState } from "./transitionContract";
export { navigationTransitionDocument, navigationTransitionStates, type NavigationTransitionRuntime } from "./transitionContract";

/** Serialized into the inline bootstrap; dependencies are supplied explicitly. */
function bootstrapNavigationTransition(
  owner: typeof navigationTransitionDocument,
  states: typeof navigationTransitionStates,
  policy: typeof navigationTransitionPolicy,
  queryKey: string,
  storageKey: string,
  version: typeof navigationTransitionVersion,
  modeAttribute: string,
  modes: typeof resolvedThemeModes,
  themeStorageKey: string,
  directions: Record<SupportedLocale, string>,
  fallbackLocale: SupportedLocale,
  parse: typeof parseNavigationTransition,
  stripQuery: typeof removeNavigationTransitionQuery,
) {
  const browser = window as unknown as Record<string, NavigationTransitionRuntime>;
  const surface = document.getElementById(owner.surfaceId)!;
  const recovery = document.getElementById(owner.recoveryId)!;
  const location = new URL(window.location.href);
  let stored: string | null = null;
  try { stored = sessionStorage.getItem(storageKey); } catch { /* Optional UI storage. */ }
  const envelope = location.searchParams.get(queryKey);
  let preferredMode = document.documentElement.getAttribute(modeAttribute);
  try { preferredMode ??= localStorage.getItem(themeStorageKey); } catch { /* A system-mode fallback needs no storage. */ }
  let context: NavigationTransitionContext = parse(envelope ?? stored) ?? {
    version, attempt: crypto.randomUUID(), started: Date.now(),
    mode: preferredMode === modes.Dark || (preferredMode !== modes.Light && window.matchMedia("(prefers-color-scheme: dark)").matches) ? modes.Dark : modes.Light,
    locale: Object.keys(directions).find(locale => locale === document.documentElement.lang) as SupportedLocale ?? fallbackLocale,
  };
  if (envelope !== null) {
    location.search = stripQuery(location.search, queryKey);
    history.replaceState(history.state, "", location.href);
  }
  try { sessionStorage.setItem(storageKey, JSON.stringify(context)); } catch { /* Optional UI storage. */ }
  surface.setAttribute(owner.modeAttribute, context.mode);
  surface.lang = context.locale;
  surface.dir = directions[context.locale];
  let state: NavigationTransitionState = states.Bootstrapping;
  const setState = (next: NavigationTransitionState) => { state = next; surface.setAttribute(owner.stateAttribute, next); };
  const terminal = () => state === states.Ready || state === states.InteractionRequired || state === states.Failed || state === states.Cancelled;
  setState(state);
  surface.setAttribute("aria-busy", "true");
  recovery.hidden = true;
  let controller = new AbortController();
  const pending = new Set<string>([owner.shellParticipant]);
  const content = () => document.querySelector<HTMLElement>(`[${owner.contentAttribute}]`);
  const cover = () => { const element = content(); if (element) element.inert = !surface.hidden && state !== states.Ready; };
  const observer = new MutationObserver(cover);
  observer.observe(document.body, { childList: true });
  cover();
  const clear = () => { try { sessionStorage.removeItem(storageKey); } catch { /* Optional UI storage. */ } };
  let slowTimer: ReturnType<typeof setTimeout>;
  let recoveryTimer: ReturnType<typeof setTimeout>;
  let revealTimer: ReturnType<typeof setTimeout> | undefined;
  const stopTimers = () => { clearTimeout(slowTimer); clearTimeout(recoveryTimer); clearTimeout(revealTimer); };
  const finish: NavigationTransitionRuntime["finish"] = outcome => {
    if (terminal()) return;
    controller.abort();
    stopTimers();
    clear();
    setState(outcome);
    surface.setAttribute("aria-busy", "false");
    const fade = outcome === states.Ready && !window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    surface.hidden = !fade;
    if (fade) {
      const finishedAttempt = context.attempt;
      revealTimer = setTimeout(() => { if (state === states.Ready && context.attempt === finishedAttempt) surface.hidden = true; }, policy.revealMilliseconds);
    }
    cover();
    observer.disconnect();
    if (document.activeElement === document.body || surface.contains(document.activeElement)) {
      const main = content()?.querySelector<HTMLElement>("main, [role=main]");
      if (main) { main.tabIndex = -1; main.focus({ preventScroll: true }); }
    }
  };
  const recover = () => {
    if (terminal()) return;
    controller.abort();
    stopTimers();
    clear();
    setState(states.Failed);
    surface.setAttribute("aria-busy", "false");
    recovery.hidden = false;
    // The failed operation is uncertain: links start new entry, never replay it.
    recovery.querySelector<HTMLAnchorElement>(`[${owner.localeAttribute}="${context.locale}"] a`)?.focus();
    observer.disconnect();
  };
  const armTimers = () => {
    const elapsed = Date.now() - context.started;
    slowTimer = setTimeout(() => { surface.setAttribute(owner.slowAttribute, "true"); }, Math.max(0, policy.slowMilliseconds - elapsed));
    recoveryTimer = setTimeout(recover, Math.max(0, policy.recoveryMilliseconds - elapsed));
  };
  armTimers();
  browser[owner.runtime] = {
    get context() { return context; },
    get state() { return state; },
    get signal() { return controller.signal; },
    hold: participant => { if (!terminal()) { pending.add(participant); if (state !== states.Departing) setState(states.Resolving); } },
    release: participant => {
      if (terminal()) return;
      pending.delete(participant);
      const releasedAttempt = context.attempt;
      // A route commit can unregister one participant before registering its replacement.
      queueMicrotask(() => { if (context.attempt === releasedAttempt && !terminal() && pending.size === 0 && state !== states.Departing) finish(states.Ready); });
    },
    finish,
    departing: () => { if (!terminal()) setState(states.Departing); },
    active: () => !terminal(),
    start: next => {
      if (!terminal()) return;
      stopTimers();
      context = next;
      controller = new AbortController();
      pending.clear();
      pending.add(owner.navigationParticipant);
      surface.hidden = false;
      setState(states.Pending);
      surface.setAttribute(owner.modeAttribute, context.mode);
      surface.setAttribute("aria-busy", "true");
      surface.lang = context.locale;
      surface.dir = directions[context.locale];
      surface.removeAttribute(owner.slowAttribute);
      recovery.hidden = true;
      cover();
      observer.observe(document.body, { childList: true });
      try { sessionStorage.setItem(storageKey, JSON.stringify(context)); } catch { /* Optional UI storage. */ }
      armTimers();
    },
  };
  window.addEventListener("pagehide", () => { stopTimers(); controller.abort(); if (state !== states.Departing) clear(); });
  window.addEventListener("pageshow", event => { if (event.persisted) finish(states.Cancelled); });
  window.addEventListener("popstate", () => finish(states.Cancelled));
  window.addEventListener("error", event => { if (event.target instanceof HTMLScriptElement) recover(); }, true);
}

export function NavigationTransitionDocument({ nonce, signInHref, homeHref }: { nonce?: string; signInHref: string; homeHref: string }) {
  // The parser has two source-owned constants; bind them in its serialized scope.
  const script = `(()=>{const policy=${JSON.stringify(navigationTransitionPolicy)};const version=${navigationTransitionVersion};(${bootstrapNavigationTransition.toString()})(${JSON.stringify(navigationTransitionDocument)},${JSON.stringify(navigationTransitionStates)},policy,${JSON.stringify(navigationTransitionQueryKey)},${JSON.stringify(navigationTransitionStorageKey)},version,${JSON.stringify(themeModeAttribute)},${JSON.stringify(resolvedThemeModes)},${JSON.stringify(themePreferenceStorageKey)},${JSON.stringify(Object.fromEntries(supportedLocales.map(locale => [locale, getLocaleDirection(locale)])))},${JSON.stringify(defaultLocale)},value=>(${validateNavigationTransitionEnvelope.toString()})(value,Date.now(),policy,version,${JSON.stringify(supportedLocales)},${JSON.stringify(Object.values(resolvedThemeModes))}),${removeNavigationTransitionQuery.toString()});})()`;
  return <>
    <style nonce={nonce}>{navigationTransitionStyles}</style>
    <div id={navigationTransitionDocument.surfaceId} className="vitarish-navigation-transition" {...{ [navigationTransitionDocument.stateAttribute]: navigationTransitionStates.Failed }} role="status" aria-live="polite" aria-busy="false" suppressHydrationWarning>
      <div className="flex max-w-sm flex-col items-center gap-5 px-6 text-center">
        <span className="text-sm font-semibold tracking-wide" dir="ltr">{navigationTransitionMessages.fa.identity}</span>
        <i className="fa-light fa-circle-notch fa-spin" aria-hidden="true" />
        <p id={navigationTransitionDocument.statusId}>
          {supportedLocales.map(locale => <span key={locale} {...{ [navigationTransitionDocument.localeAttribute]: locale }}>
            <span className="vitarish-navigation-waiting">{navigationTransitionMessages[locale].waiting}</span>
            <span className="vitarish-navigation-slow">{navigationTransitionMessages[locale].slow}</span>
            <span className="vitarish-navigation-failure">{navigationTransitionMessages[locale].recovery}</span>
          </span>)}
        </p>
        <div id={navigationTransitionDocument.recoveryId} suppressHydrationWarning>
          {supportedLocales.map(locale => <div key={locale} className="flex flex-wrap justify-center gap-5" {...{ [navigationTransitionDocument.localeAttribute]: locale }}>
            <a href={signInHref} title={navigationTransitionMessages[locale].signIn} aria-describedby={navigationTransitionDocument.statusId}><i className="fa-light fa-arrow-right-to-bracket" aria-hidden="true" /> {navigationTransitionMessages[locale].signIn}</a>
            <a href={homeHref} title={navigationTransitionMessages[locale].home} aria-describedby={navigationTransitionDocument.statusId}><i className="fa-light fa-house" aria-hidden="true" /> {navigationTransitionMessages[locale].home}</a>
          </div>)}
        </div>
        <noscript>{supportedLocales.map(locale => <div key={locale} {...{ [navigationTransitionDocument.localeAttribute]: locale }}><p>{navigationTransitionMessages[locale].noScript}</p><a href={signInHref}>{navigationTransitionMessages[locale].signIn}</a></div>)}</noscript>
      </div>
    </div>
    <script nonce={nonce} dangerouslySetInnerHTML={{ __html: script }} />
  </>;
}
