import type { Metadata } from "next";
import { defaultLocale, type SupportedLocale } from "./locales";
import { getMessages } from "./messages";
import type { MessageKey } from "./messageTypes";

function read(key: MessageKey, locale: SupportedLocale): string {
  const messages = getMessages(locale);
  const value = key.split(".").reduce<unknown>((current, part) => current && typeof current === "object" ? (current as Record<string, unknown>)[part] : undefined, messages);
  return typeof value === "string" ? value : "";
}

export function localizedMetadata(titleKey: MessageKey, descriptionKey: MessageKey, locale: SupportedLocale = defaultLocale): Metadata {
  return { title: read(titleKey, locale), description: read(descriptionKey, locale) };
}
