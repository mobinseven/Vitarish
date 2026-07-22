"use client";

import { type ReactNode } from "react";
import { I18nProvider, type SupportedLocale } from "@vitarish/web-localization";
import { defaultLocale } from "./locales";
import { getMessages, type MessageCatalog } from "./messages";

export function LocalizationProvider({
  children,
  locale = defaultLocale,
  messages,
}: {
  children: ReactNode;
  locale?: SupportedLocale;
  messages?: MessageCatalog;
}) {
  return (
    <I18nProvider locale={locale} messages={messages ?? getMessages(locale)}>
      {children}
    </I18nProvider>
  );
}
