"use client";

import { createContext, type ReactNode, useMemo } from "react";
import { defaultLocale, getLocaleDirection, type SupportedLocale } from "./locales";
import type { MessageCatalog } from "./messageTypes";

export interface LocalizationContextValue {
  locale: SupportedLocale;
  direction: "ltr" | "rtl";
  messages: MessageCatalog;
}

export const LocalizationContext = createContext<LocalizationContextValue | null>(null);

export function LocalizationProvider({ children, locale = defaultLocale, messages = {} }: { children: ReactNode; locale?: SupportedLocale; messages?: MessageCatalog }) {
  const value = useMemo(() => ({ locale, direction: getLocaleDirection(locale), messages }), [locale, messages]);
  return <LocalizationContext.Provider value={value}>{children}</LocalizationContext.Provider>;
}
