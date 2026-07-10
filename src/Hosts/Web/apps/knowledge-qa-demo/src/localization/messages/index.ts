import { defaultLocale, type SupportedLocale } from "../locales";
import { enMessages } from "./en";

export const catalogs = { en: enMessages } as const;

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type MessageCatalog = Record<string, any>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
