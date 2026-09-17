import { defaultLocale, type SupportedLocale } from "../locales";
import { clinicMessageCatalogs } from "@vitarish/clinic/localization";
import { intelligenceMessageCatalogs } from "@vitarish/intelligence/localization";
import { securityMessageCatalogs } from "@vitarish/security/localization";
import { usersMessageCatalogs } from "@vitarish/users/localization";
import { enMessages } from "./en";
import { faMessages } from "./fa";

export const catalogs = {
  fa: { ...faMessages, ...clinicMessageCatalogs.fa, ...intelligenceMessageCatalogs.fa, ...securityMessageCatalogs.fa, ...usersMessageCatalogs.fa },
  en: { ...enMessages, ...clinicMessageCatalogs.en, ...intelligenceMessageCatalogs.en, ...securityMessageCatalogs.en, ...usersMessageCatalogs.en },
} as const;

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export type MessageCatalog = Record<string, any>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
