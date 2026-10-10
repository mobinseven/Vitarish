/** Modes next-themes may resolve to and write to the root `data-theme` attribute. */
export const themeModeOptions = ["light", "dark"] as const;

/** localStorage key next-themes persists the chosen `light` | `dark` | `system` preference under. */
export const themePreferenceStorageKey = "theme";
