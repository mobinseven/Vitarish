"use client";

import { type ReactNode } from "react";
import { LocalizationProvider as WebLocalizationProvider, type SupportedLocale } from "@vitarish/web-localization";
import { defaultLocale } from "./locales";
import { getMessages, type MessageCatalog } from "./messages";

export function LocalizationProvider({ children, locale = defaultLocale, messages }: { children: ReactNode; locale?: SupportedLocale; messages?: MessageCatalog }) {
  return (
    <WebLocalizationProvider locale={locale} messages={messages ?? getMessages(locale)}>
      {children}
    </WebLocalizationProvider>
  );
}
