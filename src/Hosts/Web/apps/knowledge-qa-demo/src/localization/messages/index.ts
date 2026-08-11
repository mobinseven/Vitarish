import type { SupportedLocale } from "../locales";
import { intelligenceMessageCatalogs } from "@vitarish/intelligence-web/localization";
import { enMessages } from "./en";

const catalogs: Partial<Record<SupportedLocale, unknown>> = {
  en: { ...enMessages, ...intelligenceMessageCatalogs.en },
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type MessageCatalog = Record<string, any>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return (catalogs[locale] ?? catalogs.en) as MessageCatalog;
}
