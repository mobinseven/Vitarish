import type { Metadata } from "next";
import { defaultLocale, type SupportedLocale } from "./locales";
import { getMessages } from "./messages";
import type { MessageKey } from "@vitarish/localization";

function read(key: MessageKey, locale: SupportedLocale): string {
  const messages = getMessages(locale);
  const value = key.split(".").reduce<unknown>((current: unknown, part: string) => current && typeof current === "object" ? (current as Record<string, unknown>)[part] : undefined, messages);
  if (typeof value === "string") return value;
  if (locale !== defaultLocale) {
    return read(key, defaultLocale);
  }
  return key;
}

export function localizedMetadata(titleKey: MessageKey, descriptionKey: MessageKey, locale: SupportedLocale = defaultLocale): Metadata {
  return { title: read(titleKey, locale), description: read(descriptionKey, locale) };
}
