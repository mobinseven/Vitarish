import { defaultLocale, type SupportedLocale } from "@vitarish/localization";
import { authMessageCatalogs } from "@vitarish/auth/localization";
import { contentsStudioMessageCatalogs } from "@vitarish/contents/studio";
import { enMessages } from "./en";
import { faMessages } from "./fa";

export const catalogs = {
  en: { ...enMessages, ...authMessageCatalogs.en, ...contentsStudioMessageCatalogs.en },
  fa: { ...faMessages, ...authMessageCatalogs.fa, ...contentsStudioMessageCatalogs.fa },
} as const;

export type MessageCatalog = Record<string, unknown>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
