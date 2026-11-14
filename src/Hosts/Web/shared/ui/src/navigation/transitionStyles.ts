import { navigationTransitionDocument as owner, navigationTransitionStates as states } from "./transitionContract";
import { resolvedThemeModes } from "../themeMode";
import { navigationTransitionPolicy } from "./transitionContext";

/** Critical pre-paint styling is nonced with the document, independent of app CSS chunks. */
export const navigationTransitionStyles = `
/* Shared presentation palette: deliberately identical across application themes. */
#${owner.surfaceId} {
  color-scheme: light dark;
  --navigation-background: light-dark(oklch(98% 0 0), oklch(18% 0 0));
  --navigation-foreground: light-dark(oklch(24% 0 0), oklch(94% 0 0));
  --navigation-font-sans: system-ui, sans-serif;
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: grid;
  place-items: center;
  background: var(--navigation-background);
  color: var(--navigation-foreground);
  font-family: var(--navigation-font-sans);
  transition: opacity ${navigationTransitionPolicy.revealMilliseconds}ms ease-out;
}
#${owner.surfaceId}[${owner.stateAttribute}="${states.Ready}"] { opacity: 0; pointer-events: none; }
#${owner.surfaceId}[${owner.modeAttribute}="${resolvedThemeModes.Dark}"] {
  color-scheme: dark;
}
#${owner.surfaceId}[${owner.modeAttribute}="${resolvedThemeModes.Light}"] { color-scheme: light; }
#${owner.surfaceId}[hidden],
#${owner.surfaceId} [hidden] { display: none; }
#${owner.surfaceId} a { text-decoration: underline; text-underline-offset: .25em; }
#${owner.surfaceId} a:focus-visible { outline: 2px solid currentColor; outline-offset: .25em; }
#${owner.surfaceId}:lang(en) [${owner.localeAttribute}="fa"],
#${owner.surfaceId}:lang(fa) [${owner.localeAttribute}="en"],
#${owner.surfaceId} .vitarish-navigation-slow,
#${owner.surfaceId} .vitarish-navigation-failure { display: none; }
#${owner.surfaceId}[${owner.slowAttribute}="true"] .vitarish-navigation-waiting { display: none; }
#${owner.surfaceId}[${owner.slowAttribute}="true"] .vitarish-navigation-slow { display: inline; }
#${owner.surfaceId}[${owner.stateAttribute}="${states.Failed}"] .vitarish-navigation-waiting,
#${owner.surfaceId}[${owner.stateAttribute}="${states.Failed}"] .vitarish-navigation-slow { display: none; }
#${owner.surfaceId}[${owner.stateAttribute}="${states.Failed}"] .vitarish-navigation-failure { display: inline; }
#${owner.surfaceId}[${owner.stateAttribute}="${states.Failed}"] .fa-spin { display: none; }
@media (prefers-reduced-motion: reduce) {
  #${owner.surfaceId} .fa-spin { animation: none; }
  #${owner.surfaceId} { transition: none; }
}
`;
