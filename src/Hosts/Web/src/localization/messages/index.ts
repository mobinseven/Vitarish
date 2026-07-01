import { defaultLocale, type SupportedLocale } from "../locales";
import { enMessages } from "./en";

export const catalogs = { en: enMessages } as const;
export type MessageCatalog = typeof enMessages;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
