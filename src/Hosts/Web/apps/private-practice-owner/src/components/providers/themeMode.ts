/** Modes next-themes may resolve to and write to the root `data-theme` attribute. */
import { resolvedThemeModes } from "@vitarish/ui/theme-mode";
export const themeModeOptions = [resolvedThemeModes.Light, resolvedThemeModes.Dark] as const;

/** localStorage key next-themes persists the chosen `light` | `dark` | `system` preference under. */
export { themePreferenceStorageKey } from "@vitarish/ui/theme-mode";
