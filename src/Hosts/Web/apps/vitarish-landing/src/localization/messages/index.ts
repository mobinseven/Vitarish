import { defaultLocale, type SupportedLocale } from "@vitarish/localization";
import { authMessageCatalogs } from "@vitarish/auth/localization";
import { contentsStudioMessageCatalogs } from "@vitarish/contents/studio";
import { enMessages } from "./en";
import { faMessages } from "./fa";
import { accountMessageCatalogs, authOtpMessages } from "../../features/account/accountMessages";
import { homeMessageCatalogs } from "../../features/home/homeMessages";

// The shared auth catalog owns most of `auth`; the account area adds the one-time-code wording under `auth.otp`.
export const catalogs = {
  en: {
    ...enMessages,
    ...homeMessageCatalogs.en,
    ...accountMessageCatalogs.en,
    ...authMessageCatalogs.en,
    auth: { ...authMessageCatalogs.en.auth, otp: authOtpMessages.en },
    ...contentsStudioMessageCatalogs.en,
  },
  fa: {
    ...faMessages,
    ...homeMessageCatalogs.fa,
    ...accountMessageCatalogs.fa,
    ...authMessageCatalogs.fa,
    auth: { ...authMessageCatalogs.fa.auth, otp: authOtpMessages.fa },
    ...contentsStudioMessageCatalogs.fa,
  },
} as const;

export type MessageCatalog = Record<string, unknown>;

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return catalogs[locale] ?? catalogs[defaultLocale];
}
