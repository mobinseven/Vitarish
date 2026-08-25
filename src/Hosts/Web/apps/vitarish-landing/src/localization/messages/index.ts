import { communicationsMessageCatalogs } from "@vitarish/communications/localization";
import { intelligenceMessageCatalogs } from "@vitarish/intelligence/localization";
import { defaultLocale, type SupportedLocale } from "@vitarish/localization";
import { enMessages } from "./en";
import { faMessages } from "./fa";

export const catalogs = {
  en: {
    ...enMessages,
    ...communicationsMessageCatalogs.en,
    ...intelligenceMessageCatalogs.en,
  },
  fa: {
    ...faMessages,
    ...communicationsMessageCatalogs.fa,
    ...intelligenceMessageCatalogs.fa,
  },
} as const;

export type MessageCatalog = Record<string, unknown>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
