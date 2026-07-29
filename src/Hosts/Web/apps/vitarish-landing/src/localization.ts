import { communicationsMessageCatalogs } from "@vitarish/communications-web/localization";
import type { MessageCatalog, SupportedLocale } from "@vitarish/web-localization";

export function getMessages(locale: SupportedLocale): MessageCatalog {
  return communicationsMessageCatalogs[locale];
}
