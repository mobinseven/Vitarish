import { defaultLocale, type SupportedLocale } from "../locales";
import { clinicMessageCatalogs } from "@vitarish/clinic-web/localization";
import { enMessages } from "./en";
import { faMessages } from "./fa";

export const catalogs = {
  fa: { ...faMessages, ...clinicMessageCatalogs.fa },
  en: { ...enMessages, ...clinicMessageCatalogs.en },
} as const;

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type MessageCatalog = Record<string, any>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
