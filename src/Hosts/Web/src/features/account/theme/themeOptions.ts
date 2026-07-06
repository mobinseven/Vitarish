import type { MessageKey } from "@/localization/messageTypes";

export type AccountThemeValue =
  | "system"
  | "light"
  | "dark"
  | "brutalism-light"
  | "brutalism-dark"
  | "glass-light"
  | "glass-dark"
  | "mouve-light"
  | "mouve-dark";

export type ThemeMode = "light" | "dark" | "system";

export interface AccountThemePreference {
  theme: AccountThemeValue;
  isVibrant: boolean;
}

export interface AccountThemeOption {
  value: AccountThemeValue;
  labelKey: MessageKey;
  descriptionKey: MessageKey;
  mode: ThemeMode;
  swatches: readonly string[];
}

export const accountThemeOptions: readonly AccountThemeOption[] = [
  {
    value: "system",
    labelKey: "account.theme.palettes.system",
    descriptionKey: "account.theme.paletteDescriptions.system",
    mode: "system",
    swatches: ["#ffffff", "#111827", "#6d5dfc"],
  },
  {
    value: "light",
    labelKey: "account.theme.palettes.defaultLight",
    descriptionKey: "account.theme.paletteDescriptions.defaultLight",
    mode: "light",
    swatches: ["#ffffff", "#f4f4f5", "#6d5dfc"],
  },
  {
    value: "dark",
    labelKey: "account.theme.palettes.defaultDark",
    descriptionKey: "account.theme.paletteDescriptions.defaultDark",
    mode: "dark",
    swatches: ["#111827", "#27272a", "#a5b4fc"],
  },
  {
    value: "brutalism-light",
    labelKey: "account.theme.palettes.brutalismLight",
    descriptionKey: "account.theme.paletteDescriptions.brutalismLight",
    mode: "light",
    swatches: ["#ffffff", "#111111", "#facc15"],
  },
  {
    value: "brutalism-dark",
    labelKey: "account.theme.palettes.brutalismDark",
    descriptionKey: "account.theme.paletteDescriptions.brutalismDark",
    mode: "dark",
    swatches: ["#0a0a0a", "#ffffff", "#f97316"],
  },
  {
    value: "glass-light",
    labelKey: "account.theme.palettes.glassLight",
    descriptionKey: "account.theme.paletteDescriptions.glassLight",
    mode: "light",
    swatches: ["#f8fafc", "#dbeafe", "#64748b"],
  },
  {
    value: "glass-dark",
    labelKey: "account.theme.palettes.glassDark",
    descriptionKey: "account.theme.paletteDescriptions.glassDark",
    mode: "dark",
    swatches: ["#020617", "#1e293b", "#e2e8f0"],
  },
  {
    value: "mouve-light",
    labelKey: "account.theme.palettes.mouveLight",
    descriptionKey: "account.theme.paletteDescriptions.mouveLight",
    mode: "light",
    swatches: ["#fff7ed", "#f5d0fe", "#7c3aed"],
  },
  {
    value: "mouve-dark",
    labelKey: "account.theme.palettes.mouveDark",
    descriptionKey: "account.theme.paletteDescriptions.mouveDark",
    mode: "dark",
    swatches: ["#181014", "#3b2738", "#f0abfc"],
  },
] as const;

const accountThemeValues = new Set<string>(accountThemeOptions.map((option) => option.value));
const vibrantPreferenceSuffix = ":vibrant";

export const vibrantPaletteStorageKey = "vitarish:vibrant-palette";

export function isAccountThemeValue(value: string | null | undefined): value is AccountThemeValue {
  return !!value && accountThemeValues.has(value);
}

export function decodeAccountThemePreference(value: string | null | undefined): AccountThemePreference {
  const isVibrant = !!value?.endsWith(vibrantPreferenceSuffix);
  const theme = isVibrant && value
    ? value.slice(0, -vibrantPreferenceSuffix.length)
    : value;

  return {
    theme: isAccountThemeValue(theme) ? theme : "system",
    isVibrant,
  };
}

export function encodeAccountThemePreference(preference: AccountThemePreference): string {
  return preference.isVibrant
    ? `${preference.theme}${vibrantPreferenceSuffix}`
    : preference.theme;
}

export function setVibrantPaletteAttribute(isVibrant: boolean, root: HTMLElement): void {
  if (isVibrant) {
    root.dataset.vibrantPalette = "true";
    return;
  }

  delete root.dataset.vibrantPalette;
}

export function resolveThemeMode(value: string | undefined): ThemeMode {
  if (value === "system") return "system";
  return value?.endsWith("-dark") || value === "dark" ? "dark" : "light";
}
