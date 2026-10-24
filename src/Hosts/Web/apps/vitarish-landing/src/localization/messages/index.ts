import { defaultLocale, type SupportedLocale } from "@vitarish/localization";
import { enMessages } from "./en";
import { faMessages } from "./fa";

export const catalogs = {
  en: enMessages,
  fa: faMessages,
} as const;

export type MessageCatalog = Record<string, unknown>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
