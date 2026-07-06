import type { SupportedLocale } from "./locales";

export type SupportedDateCalendar = "gregory" | "persian";

const reactAriaLocaleByAppLocale = {
  fa: "fa-IR-u-ca-persian",
  en: "en-US-u-ca-gregory",
} satisfies Record<SupportedLocale, string>;

const dateCalendarByAppLocale = {
  fa: "persian",
  en: "gregory",
} satisfies Record<SupportedLocale, SupportedDateCalendar>;

export function getReactAriaLocale(locale: SupportedLocale): string {
  return reactAriaLocaleByAppLocale[locale];
}

export function getDateCalendar(locale: SupportedLocale): SupportedDateCalendar {
  return dateCalendarByAppLocale[locale];
}
