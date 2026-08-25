"use client";

import { I18nProvider as ReactAriaI18nProvider } from "@react-aria/i18n";
import { createContext, type ReactNode, useMemo } from "react";
import { I18nProvider as ReactAriaComponentsI18nProvider } from "react-aria-components/I18nProvider";
import { getReactAriaLocale } from "./dateLocale";
import {
  defaultLocale,
  getLocaleDirection,
  type SupportedLocale,
} from "./locales";
import type { MessageCatalog } from "./messageTypes";

export interface LocalizationContextValue {
  locale: SupportedLocale;
  direction: "ltr" | "rtl";
  messages: MessageCatalog;
}

export const LocalizationContext =
  createContext<LocalizationContextValue | null>(null);

export interface I18nProviderProps {
  children: ReactNode;
  locale?: SupportedLocale;
  messages?: MessageCatalog;
}

export function I18nProvider({
  children,
  locale = defaultLocale,
  messages = {},
}: I18nProviderProps) {
  const value = useMemo(
    () => ({ locale, direction: getLocaleDirection(locale), messages }),
    [locale, messages],
  );

  return (
    <ReactAriaI18nProvider locale={getReactAriaLocale(locale)}>
      <ReactAriaComponentsI18nProvider locale={getReactAriaLocale(locale)}>
        <LocalizationContext.Provider value={value}>
          {children}
        </LocalizationContext.Provider>
      </ReactAriaComponentsI18nProvider>
    </ReactAriaI18nProvider>
  );
}

export function LocalizationProvider(props: I18nProviderProps) {
  return <I18nProvider {...props} />;
}
