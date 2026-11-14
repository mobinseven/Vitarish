/** Resolved modes shared by next-themes, palettes and pre-paint presentation. */
export const resolvedThemeModes = { Light: "light", Dark: "dark" } as const;
export type ResolvedThemeMode = typeof resolvedThemeModes[keyof typeof resolvedThemeModes];
export const themeModeAttribute = "data-theme";
/** Existing next-themes preference, shared with pre-paint presentation readers. */
export const themePreferenceStorageKey = "theme";
