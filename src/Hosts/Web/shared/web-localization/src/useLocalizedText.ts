"use client";

import { useContext, useMemo } from "react";
import { defaultLocale } from "./locales";
import { LocalizationContext } from "./LocalizationProvider";
import type { MessageCatalog, MessageKey, MessageParameters } from "./messageTypes";

function readMessage(messages: unknown, key: MessageKey): string | null {
  const value = key.split(".").reduce<unknown>((current, part) => current && typeof current === "object" ? (current as Record<string, unknown>)[part] : undefined, messages);
  return typeof value === "string" ? value : null;
}

function interpolate(template: string, parameters?: MessageParameters): string {
  if (!parameters) return template;
  return template.replace(/\{(\w+)\}/g, (_, name: string) => String(parameters[name] ?? `{${name}}`));
}

export function useLocalizedText() {
  const context = useContext(LocalizationContext);
  const locale = context?.locale ?? defaultLocale;
  const direction = context?.direction ?? "ltr";
  const messages = context?.messages ?? {};

  return useMemo(() => {
    const t = (key: MessageKey, parameters?: MessageParameters) => interpolate(readMessage(messages, key) ?? key, parameters);
    return { t, locale, direction };
  }, [direction, locale, messages]);
}
