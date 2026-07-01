export const defaultCulture = "en-US";

export const supportedCultures = ["en-US", "fa-IR"] as const;

export type SupportedCulture = (typeof supportedCultures)[number];

export const cultureCookieName = "vitarish.culture";

export const supportedCultureOptions = [
  { value: "en-US", nativeLabel: "English" },
  { value: "fa-IR", nativeLabel: "فارسی" },
] as const satisfies ReadonlyArray<{
  value: SupportedCulture;
  nativeLabel: string;
}>;

const cultureAliases: Record<string, SupportedCulture> = {
  en: "en-US",
  "en-us": "en-US",
  fa: "fa-IR",
  "fa-ir": "fa-IR",
  pr: "fa-IR",
  persian: "fa-IR",
};

export function tryNormalizeCulture(
  culture: string | null | undefined,
): SupportedCulture | undefined {
  if (!culture) {
    return undefined;
  }

  const normalized = culture.trim().toLowerCase();

  if (!normalized) {
    return undefined;
  }

  return cultureAliases[normalized] ?? cultureAliases[normalized.split("-")[0]];
}

export function normalizeCulture(
  culture: string | null | undefined,
): SupportedCulture {
  return tryNormalizeCulture(culture) ?? defaultCulture;
}

export function isRtlCulture(culture: SupportedCulture): boolean {
  return culture === "fa-IR";
}

export function getCultureDirection(culture: SupportedCulture): "ltr" | "rtl" {
  return isRtlCulture(culture) ? "rtl" : "ltr";
}
