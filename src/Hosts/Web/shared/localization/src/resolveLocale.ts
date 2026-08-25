import { defaultLocale, type SupportedLocale, toSupportedLocale } from "./locales";

export type LocaleResolutionSource = "selected" | "account" | "route" | "cookie" | "browser" | "accept-language" | "default";

export interface ResolveLocaleInput {
  selectedLocale?: string | null;
  routeLocale?: string | null;
  accountLocale?: string | null;
  cookieLocale?: string | null;
  browserLocales?: readonly string[] | null;
  acceptLanguageLocales?: readonly string[] | null;
}

export interface ResolvedLocale {
  locale: SupportedLocale;
  source: LocaleResolutionSource;
  reason: string;
}

export function resolveLocale(input: ResolveLocaleInput = {}): ResolvedLocale {
  const selectedLocale = toSupportedLocale(input.selectedLocale);
  if (selectedLocale) return { locale: selectedLocale, source: "selected", reason: "Supported user-selected culture." };

  const accountLocale = toSupportedLocale(input.accountLocale);
  if (accountLocale) return { locale: accountLocale, source: "account", reason: "Supported authenticated account culture preference." };

  const routeLocale = toSupportedLocale(input.routeLocale);
  if (routeLocale) return { locale: routeLocale, source: "route", reason: "Supported route locale segment." };

  const cookieLocale = toSupportedLocale(input.cookieLocale);
  if (cookieLocale) return { locale: cookieLocale, source: "cookie", reason: "Supported persisted locale cookie." };

  for (const candidate of input.browserLocales ?? []) {
    const supported = toSupportedLocale(candidate);
    if (supported) return { locale: supported, source: "browser", reason: "Supported browser culture candidate." };
  }

  for (const candidate of input.acceptLanguageLocales ?? []) {
    const supported = toSupportedLocale(candidate);
    if (supported) return { locale: supported, source: "accept-language", reason: "Supported Accept-Language candidate." };
  }

  return { locale: defaultLocale, source: "default", reason: "No supported locale input was available." };
}
