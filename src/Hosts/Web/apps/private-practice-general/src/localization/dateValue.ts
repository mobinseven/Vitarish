import { parseDate, type DateValue } from "@internationalized/date";

const isoDatePattern = /^\d{4}-\d{2}-\d{2}$/;

export function parseIsoDateValue(value: string | null | undefined): DateValue | null {
  if (!value) return null;
  if (!isoDatePattern.test(value)) return null;
  try {
    return parseDate(value);
  } catch {
    return null;
  }
}

export function dateValueToIsoDate(value: DateValue | null): string {
  return value?.toString() ?? "";
}
