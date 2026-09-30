import type { SupportedLocale } from "../locales";
import { authMessageCatalogs } from "@vitarish/auth/localization";
import { intelligenceMessageCatalogs } from "@vitarish/intelligence/localization";
import { enMessages } from "./en";

const catalogs: Partial<Record<SupportedLocale, unknown>> = {
  en: { ...enMessages, auth: { ...enMessages.auth, ...authMessageCatalogs.en.auth }, ...intelligenceMessageCatalogs.en },
};

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type MessageCatalog = Record<string, any>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return (catalogs[locale] ?? catalogs.en) as MessageCatalog;
}
