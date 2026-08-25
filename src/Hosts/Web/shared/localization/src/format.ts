import { defaultLocale, type SupportedLocale } from "./locales";

export function formatDate(value: Date | string | number, locale: SupportedLocale = defaultLocale, options?: Intl.DateTimeFormatOptions): string {
  const date = new Date(value);
  if (isNaN(date.getTime())) return "";
  return new Intl.DateTimeFormat(locale, options).format(date);
}
export function formatDateTime(value: Date | string | number, locale: SupportedLocale = defaultLocale, options?: Intl.DateTimeFormatOptions): string { return formatDate(value, locale, { dateStyle: "medium", timeStyle: "short", ...options }); }
export function formatNumber(value: number, locale: SupportedLocale = defaultLocale, options?: Intl.NumberFormatOptions): string { return new Intl.NumberFormat(locale, options).format(value); }
export function formatPercent(value: number, locale: SupportedLocale = defaultLocale, options?: Intl.NumberFormatOptions): string { return formatNumber(value, locale, { style: "percent", ...options }); }
export function formatCurrency(value: number, currency: string, locale: SupportedLocale = defaultLocale, options?: Intl.NumberFormatOptions): string { return formatNumber(value, locale, { style: "currency", currency, ...options }); }
export function formatList(values: readonly string[], locale: SupportedLocale = defaultLocale, options?: Intl.ListFormatOptions): string { return new Intl.ListFormat(locale, options).format(values); }
export function formatRelativeTime(value: number, unit: Intl.RelativeTimeFormatUnit, locale: SupportedLocale = defaultLocale, options?: Intl.RelativeTimeFormatOptions): string { return new Intl.RelativeTimeFormat(locale, options).format(value, unit); }
export function formatFileSize(bytes: number, locale: SupportedLocale = defaultLocale): string {
  if (!Number.isFinite(bytes) || bytes < 0) return "";
  const units = ["byte", "kilobyte", "megabyte", "gigabyte", "terabyte"] as const;
  const exponent = bytes === 0 ? 0 : Math.min(Math.floor(Math.log(bytes) / Math.log(1024)), units.length - 1);
  return new Intl.NumberFormat(locale, {
    style: "unit",
    unit: units[exponent],
    unitDisplay: "short",
    maximumFractionDigits: exponent === 0 ? 0 : 1,
  }).format(bytes / 1024 ** exponent);
}
