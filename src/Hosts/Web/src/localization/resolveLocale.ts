import { defaultLocale, type SupportedLocale, toSupportedLocale } from "./locales";

export type LocaleResolutionSource = "account" | "cookie" | "accept-language" | "default";

export interface ResolveLocaleInput {
  routeLocale?: string | null;
  accountLocale?: string | null;
  cookieLocale?: string | null;
  acceptLanguageLocales?: readonly string[] | null;
}

export interface ResolvedLocale {
  locale: SupportedLocale;
  source: LocaleResolutionSource;
  reason: string;
}

export function resolveLocale(input: ResolveLocaleInput = {}): ResolvedLocale {
  const accountLocale = toSupportedLocale(input.accountLocale ?? input.routeLocale);
  if (accountLocale) return { locale: accountLocale, source: "account", reason: "Supported authenticated account culture preference." };

  const cookieLocale = toSupportedLocale(input.cookieLocale);
  if (cookieLocale) return { locale: cookieLocale, source: "cookie", reason: "Supported persisted locale cookie." };

  for (const candidate of input.acceptLanguageLocales ?? []) {
    const supported = toSupportedLocale(candidate);
    if (supported) return { locale: supported, source: "accept-language", reason: "Supported Accept-Language candidate." };
  }

  return { locale: defaultLocale, source: "default", reason: "No supported locale input was available." };
}
