import type { MessageKey } from "@/localization/messageTypes";

export type AccountThemeValue =
  | "default"
  | "brutalism"
  | "glass"
  | "mouve";

export type ThemeMode = "light" | "dark" | "system";

export interface AccountThemePreference {
  theme: AccountThemeValue;
  isVibrant: boolean;
}

export interface AccountThemeOption {
  value: AccountThemeValue;
  labelKey: MessageKey;
  descriptionKey: MessageKey;
  swatches: readonly string[];
}

export const accountThemeOptions: readonly AccountThemeOption[] = [
  {
    value: "default",
    labelKey: "account.theme.palettes.default",
    descriptionKey: "account.theme.paletteDescriptions.default",
    swatches: ["#ffffff", "#f4f4f5", "#6d5dfc"],
  },
  {
    value: "brutalism",
    labelKey: "account.theme.palettes.brutalism",
    descriptionKey: "account.theme.paletteDescriptions.brutalism",
    swatches: ["#ffffff", "#111111", "#facc15"],
  },
  {
    value: "glass",
    labelKey: "account.theme.palettes.glass",
    descriptionKey: "account.theme.paletteDescriptions.glass",
    swatches: ["#f8fafc", "#dbeafe", "#64748b"],
  },
  {
    value: "mouve",
    labelKey: "account.theme.palettes.mouve",
    descriptionKey: "account.theme.paletteDescriptions.mouve",
    swatches: ["#fff7ed", "#f5d0fe", "#7c3aed"],
  },
] as const;

const accountThemeValues = new Set<string>(accountThemeOptions.map((option) => option.value));
const legacyPaletteValues: Record<string, AccountThemeValue> = {
  system: "default",
  light: "default",
  dark: "default",
  "brutalism-light": "brutalism",
  "brutalism-dark": "brutalism",
  "glass-light": "glass",
  "glass-dark": "glass",
  "mouve-light": "mouve",
  "mouve-dark": "mouve",
};
const paletteClasses = accountThemeOptions
  .filter((option) => option.value !== "default")
  .flatMap((option) => [`${option.value}-light`, `${option.value}-dark`]);
const vibrantPreferenceSuffix = ":vibrant";

export const vibrantPaletteStorageKey = "vitarish:vibrant-palette";
export const paletteStorageKey = "vitarish:theme-palette";

export function isAccountThemeValue(value: string | null | undefined): value is AccountThemeValue {
  return !!value && accountThemeValues.has(value);
}

function resolvePersistedPalette(value: string | null | undefined): AccountThemeValue {
  if (isAccountThemeValue(value)) return value;
  return value ? legacyPaletteValues[value] ?? "default" : "default";
}

export function decodeAccountThemePreference(value: string | null | undefined): AccountThemePreference {
  const isVibrant = !!value?.endsWith(vibrantPreferenceSuffix);
  const theme = isVibrant && value
    ? value.slice(0, -vibrantPreferenceSuffix.length)
    : value;

  return {
    theme: resolvePersistedPalette(theme),
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

export function setPaletteAttribute(
  palette: AccountThemeValue,
  mode: Exclude<ThemeMode, "system">,
  root: HTMLElement
): void {
  root.classList.remove(...paletteClasses);

  if (palette === "default") return;

  root.classList.add(`${palette}-${mode}`);
}
