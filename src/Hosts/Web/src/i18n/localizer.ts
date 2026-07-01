import {
  getCultureDirection,
  normalizeCulture,
  type SupportedCulture,
} from "./cultures";
import { fallbackMessages, messages, type MessageKey } from "./messages";

export type MessageValue = string | number | boolean | Date | null | undefined;

export type MessageValues = Record<string, MessageValue>;

export interface Localizer {
  readonly culture: SupportedCulture;
  readonly direction: "ltr" | "rtl";
  readonly t: (key: MessageKey, values?: MessageValues) => string;
  readonly formatDate: (
    value: Date | number | string,
    options?: Intl.DateTimeFormatOptions,
  ) => string;
  readonly formatNumber: (
    value: number,
    options?: Intl.NumberFormatOptions,
  ) => string;
}

export function createLocalizer(
  cultureInput: string | null | undefined,
): Localizer {
  const culture = normalizeCulture(cultureInput);

  return {
    culture,
    direction: getCultureDirection(culture),
    t: (key, values) => translate(culture, key, values),
    formatDate: (value, options) =>
      new Intl.DateTimeFormat(culture, options).format(new Date(value)),
    formatNumber: (value, options) =>
      new Intl.NumberFormat(culture, options).format(value),
  };
}

export function translate(
  cultureInput: string | null | undefined,
  key: MessageKey,
  values?: MessageValues,
): string {
  const culture = normalizeCulture(cultureInput);
  const template = messages[culture][key] ?? fallbackMessages[key] ?? key;

  return formatMessage(template, values);
}

function formatMessage(template: string, values?: MessageValues): string {
  if (!values) {
    return template;
  }

  return template.replace(/\{([A-Za-z0-9_.-]+)\}/g, (match, key) => {
    const value = values[key];

    if (value === null || value === undefined) {
      return "";
    }

    if (value instanceof Date) {
      return value.toISOString();
    }

    return String(value ?? match);
  });
}
