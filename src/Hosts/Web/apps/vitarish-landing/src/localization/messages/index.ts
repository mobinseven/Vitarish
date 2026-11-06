import { defaultLocale, type SupportedLocale } from "@vitarish/localization";
import { authMessageCatalogs } from "@vitarish/auth/localization";
import { contentsStudioMessageCatalogs } from "@vitarish/contents/studio";
import { enMessages } from "./en";
import { faMessages } from "./fa";
import { homeMessageCatalogs } from "../../features/home/homeMessages";

export const catalogs = {
  en: { ...enMessages, ...homeMessageCatalogs.en, ...authMessageCatalogs.en, ...contentsStudioMessageCatalogs.en },
  fa: { ...faMessages, ...homeMessageCatalogs.fa, ...authMessageCatalogs.fa, ...contentsStudioMessageCatalogs.fa },
} as const;

export type MessageCatalog = Record<string, unknown>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
