export const supportedLocales = ["fa", "en"] as const;

export type SupportedLocale = (typeof supportedLocales)[number];

export const defaultLocale: SupportedLocale = "fa";

const supportedLocaleSet = new Set<string>(supportedLocales);

export function isSupportedLocale(locale: string | null | undefined): locale is SupportedLocale {
  return typeof locale === "string" && supportedLocaleSet.has(normalizeLocale(locale));
}

export function normalizeLocale(locale: string): string {
  return locale.trim().toLowerCase();
}

export function toSupportedLocale(locale: string | null | undefined): SupportedLocale | null {
  if (!locale) return null;
  const normalized = normalizeLocale(locale);
  if (isSupportedLocale(normalized)) return normalized;
  const baseLocale = normalized.split("-")[0];
  return isSupportedLocale(baseLocale) ? baseLocale : null;
}

export function getLocaleDirection(locale: SupportedLocale): "ltr" | "rtl" {
  if (locale === "fa") return "rtl";
  return "ltr";
}
