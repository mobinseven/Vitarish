import { authMessageCatalogs } from "@vitarish/auth/localization";
import { clinicMessageCatalogs } from "@vitarish/clinic/localization";
import { intelligenceMessageCatalogs } from "@vitarish/intelligence/localization";
import { defaultLocale, type SupportedLocale } from "@vitarish/localization";
import { enMessages } from "./en";
import { faMessages } from "./fa";

export const catalogs = {
  en: {
    ...enMessages,
    ...authMessageCatalogs.en,
    // Only the specialty labels: the consultation names clinic kinds as sign-up does.
    clinic: { specialties: clinicMessageCatalogs.en.clinic.specialties },
    ...intelligenceMessageCatalogs.en,
  },
  fa: {
    ...faMessages,
    ...authMessageCatalogs.fa,
    // Only the specialty labels: the consultation names clinic kinds as sign-up does.
    clinic: { specialties: clinicMessageCatalogs.fa.clinic.specialties },
    ...intelligenceMessageCatalogs.fa,
  },
} as const;

export type MessageCatalog = Record<string, unknown>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
