import { communicationsMessageCatalogs } from "@vitarish/communications-web/localization";
import { defaultLocale, type SupportedLocale } from "@vitarish/web-localization";
import { enMessages } from "./en";
import { faMessages } from "./fa";

export const catalogs = {
  en: { ...enMessages, ...communicationsMessageCatalogs.en },
  fa: { ...faMessages, ...communicationsMessageCatalogs.fa },
} as const;

export type MessageCatalog = Record<string, unknown>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
