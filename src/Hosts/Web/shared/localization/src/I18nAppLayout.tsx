import type { ReactNode } from "react";
import { parseAcceptLanguage } from "./acceptLanguage";
import { I18nProvider } from "./LocalizationProvider";
import {
  getLocaleDirection,
  toSupportedLocale,
  type SupportedLocale,
} from "./locales";
import { parseLocaleCookie, readLocaleCookieDomain } from "./localeCookie";
import type { MessageCatalog } from "./messageTypes";
import { resolveLocale, type ResolvedLocale } from "./resolveLocale";

export interface ResolveI18nAppLocaleInput {
  selectedLocale?: string | null;
  cookieHeader?: string | null;
  acceptLanguageHeader?: string | null;
}

export interface I18nAppLayoutProps {
  children: ReactNode;
  getMessages: (locale: SupportedLocale) => MessageCatalog;
  selectedLocale?: string | null;
  cookieHeader?: string | null;
  acceptLanguageHeader?: string | null;
  htmlClassName?: string;
  vibrantPalette?: boolean;
  bodyClassName?: string;
}

export function resolveI18nAppLocale({
  selectedLocale,
  cookieHeader,
  acceptLanguageHeader,
}: ResolveI18nAppLocaleInput = {}): ResolvedLocale {
  const persistedLocale =
    toSupportedLocale(selectedLocale) ?? parseLocaleCookie(cookieHeader);
  return resolveLocale({
    selectedLocale: persistedLocale,
    browserLocales: parseAcceptLanguage(acceptLanguageHeader),
  });
}

export function I18nAppLayout({
  children,
  getMessages,
  selectedLocale,
  cookieHeader,
  acceptLanguageHeader,
  htmlClassName,
  vibrantPalette = false,
  bodyClassName,
}: I18nAppLayoutProps) {
  const { locale } = resolveI18nAppLocale({
    selectedLocale,
    cookieHeader,
    acceptLanguageHeader,
  });

  return (
    <html
      lang={locale}
      dir={getLocaleDirection(locale)}
      suppressHydrationWarning
      className={htmlClassName}
      data-vibrant-palette={vibrantPalette ? "true" : undefined}
    >
      <body className={bodyClassName}>
        <I18nProvider locale={locale} localeCookieDomain={readLocaleCookieDomain(process.env)} messages={getMessages(locale)}>
          {children}
        </I18nProvider>
      </body>
    </html>
  );
}
