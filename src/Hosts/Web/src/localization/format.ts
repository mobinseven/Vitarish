import { defaultLocale, type SupportedLocale } from "./locales";

export function formatDate(value: Date | string | number, locale: SupportedLocale = defaultLocale, options?: Intl.DateTimeFormatOptions): string { return new Intl.DateTimeFormat(locale, options).format(new Date(value)); }
export function formatDateTime(value: Date | string | number, locale: SupportedLocale = defaultLocale, options?: Intl.DateTimeFormatOptions): string { return formatDate(value, locale, { dateStyle: "medium", timeStyle: "short", ...options }); }
export function formatNumber(value: number, locale: SupportedLocale = defaultLocale, options?: Intl.NumberFormatOptions): string { return new Intl.NumberFormat(locale, options).format(value); }
export function formatPercent(value: number, locale: SupportedLocale = defaultLocale, options?: Intl.NumberFormatOptions): string { return formatNumber(value, locale, { style: "percent", ...options }); }
export function formatCurrency(value: number, currency: string, locale: SupportedLocale = defaultLocale, options?: Intl.NumberFormatOptions): string { return formatNumber(value, locale, { style: "currency", currency, ...options }); }
export function formatList(values: readonly string[], locale: SupportedLocale = defaultLocale, options?: Intl.ListFormatOptions): string { return new Intl.ListFormat(locale, options).format(values); }
export function formatRelativeTime(value: number, unit: Intl.RelativeTimeFormatUnit, locale: SupportedLocale = defaultLocale, options?: Intl.RelativeTimeFormatOptions): string { return new Intl.RelativeTimeFormat(locale, options).format(value, unit); }
